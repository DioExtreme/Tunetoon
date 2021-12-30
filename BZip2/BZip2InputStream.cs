using System;
using System.IO;

namespace Tunetoon.BZip2
{
	/*
	Copyright © 2000-2018 SharpZipLib Contributors

	Permission is hereby granted, free of charge, to any person obtaining a copy of this
	software and associated documentation files (the "Software"), to deal in the Software
	without restriction, including without limitation the rights to use, copy, modify, merge,
	publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
	to whom the Software is furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in all copies or
	substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
	INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
	PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
	FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
	OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
	DEALINGS IN THE SOFTWARE.
	*/

	/// <summary>
	/// An input stream that decompresses files in the BZip2 format
	/// </summary>
	public class BZip2InputStream : Stream
	{
		#region Constants

		private const int START_BLOCK_STATE = 1;
		private const int NO_RAND_PART_A_STATE = 5;
		private const int NO_RAND_PART_B_STATE = 6;
		private const int NO_RAND_PART_C_STATE = 7;

		#endregion Constants

		#region Instance Fields

		/*--
		index of the last char in the block, so
		the block size == last + 1.
		--*/
		private int last;

		/*--
		index in zptr[] of original string after sorting.
		--*/
		private int origPtr;

		/*--
		always: in the range 0 .. 9.
		The current block size is 100000 * this number.
		--*/
		private int blockSize100k;

		private int bitBuffer;
		private int bitCount;

		private bool[] inUse = new bool[256];
		private int nInUse;

		private byte[] seqToUnseq = new byte[256];
		private byte[] unseqToSeq = new byte[256];

		private byte[] selector = new byte[BZip2Constants.MaximumSelectors];
		private byte[] selectorMtf = new byte[BZip2Constants.MaximumSelectors];

		private int[] tt;
		private byte[] ll8;

		/*--
		freq table collected to save a pass over the data
		during decompression.
		--*/
		private int[] unzftab = new int[256];

		private int[][] limit = new int[BZip2Constants.GroupCount][];
		private int[][] baseArray = new int[BZip2Constants.GroupCount][];
		private int[][] perm = new int[BZip2Constants.GroupCount][];
		private int[] minLens = new int[BZip2Constants.GroupCount];

		private readonly Stream baseStream;
		private bool streamEnd;

		private int currentChar = -1;

		private int currentState = START_BLOCK_STATE;

		private int count, chPrev, ch2;
		private int tPos;
		private int i2, j2;
		private byte z;

		#endregion Instance Fields

		/// <summary>
		/// Construct instance for reading from stream
		/// </summary>
		/// <param name="stream">Data source</param>
		public BZip2InputStream(Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));
			// init arrays
			for (int i = 0; i < BZip2Constants.GroupCount; ++i)
			{
				limit[i] = new int[BZip2Constants.MaximumAlphaSize];
				baseArray[i] = new int[BZip2Constants.MaximumAlphaSize];
				perm[i] = new int[BZip2Constants.MaximumAlphaSize];
			}

			baseStream = stream;
			bitCount = 0;
			bitBuffer = 0;
			Initialize();
			InitBlock();
			SetupBlock();
		}

		/// <summary>
		/// Get/set flag indicating ownership of underlying stream.
		/// When the flag is true <see cref="Stream.Dispose()" /> will close the underlying stream also.
		/// </summary>
		public bool IsStreamOwner { get; set; } = true;

		#region Stream Overrides

		/// <summary>
		/// Gets a value indicating if the stream supports reading
		/// </summary>
		public override bool CanRead
		{
			get
			{
				return baseStream.CanRead;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the current stream supports seeking.
		/// </summary>
		public override bool CanSeek
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the current stream supports writing.
		/// This property always returns false
		/// </summary>
		public override bool CanWrite
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		/// Gets the length in bytes of the stream.
		/// </summary>
		public override long Length
		{
			get
			{
				return baseStream.Length;
			}
		}

		/// <summary>
		/// Gets the current position of the stream.
		/// Setting the position is not supported and will throw a NotSupportException.
		/// </summary>
		/// <exception cref="NotSupportedException">Any attempt to set the position.</exception>
		public override long Position
		{
			get
			{
				return baseStream.Position;
			}
			set
			{
				throw new NotSupportedException("BZip2InputStream position cannot be set");
			}
		}

		/// <summary>
		/// Flushes the stream.
		/// </summary>
		public override void Flush()
		{
			baseStream.Flush();
		}

		/// <summary>
		/// Set the streams position.  This operation is not supported and will throw a NotSupportedException
		/// </summary>
		/// <param name="offset">A byte offset relative to the <paramref name="origin"/> parameter.</param>
		/// <param name="origin">A value of type <see cref="SeekOrigin"/> indicating the reference point used to obtain the new position.</param>
		/// <returns>The new position of the stream.</returns>
		/// <exception cref="NotSupportedException">Any access</exception>
		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException("BZip2InputStream Seek not supported");
		}

		/// <summary>
		/// Sets the length of this stream to the given value.
		/// This operation is not supported and will throw a NotSupportedExceptionortedException
		/// </summary>
		/// <param name="value">The new length for the stream.</param>
		/// <exception cref="NotSupportedException">Any access</exception>
		public override void SetLength(long value)
		{
			throw new NotSupportedException("BZip2InputStream SetLength not supported");
		}

		/// <summary>
		/// Writes a block of bytes to this stream using data from a buffer.
		/// This operation is not supported and will throw a NotSupportedException
		/// </summary>
		/// <param name="buffer">The buffer to source data from.</param>
		/// <param name="offset">The offset to start obtaining data from.</param>
		/// <param name="count">The number of bytes of data to write.</param>
		/// <exception cref="NotSupportedException">Any access</exception>
		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException("BZip2InputStream Write not supported");
		}

		/// <summary>
		/// Writes a byte to the current position in the file stream.
		/// This operation is not supported and will throw a NotSupportedException
		/// </summary>
		/// <param name="value">The value to write.</param>
		/// <exception cref="NotSupportedException">Any access</exception>
		public override void WriteByte(byte value)
		{
			throw new NotSupportedException("BZip2InputStream WriteByte not supported");
		}

		/// <summary>
		/// Read a sequence of bytes and advances the read position by one byte.
		/// </summary>
		/// <param name="buffer">Array of bytes to store values in</param>
		/// <param name="offset">Offset in array to begin storing data</param>
		/// <param name="count">The maximum number of bytes to read</param>
		/// <returns>The total number of bytes read into the buffer. This might be less
		/// than the number of bytes requested if that number of bytes are not
		/// currently available or zero if the end of the stream is reached.
		/// </returns>
		public override int Read(byte[] buffer, int offset, int count)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException(nameof(buffer));
			}

			for (int i = 0; i < count; ++i)
			{
				int rb = ReadByte();
				if (rb == -1)
				{
					return i;
				}
				buffer[offset + i] = (byte)rb;
			}
			return count;
		}

		/// <summary>
		/// Closes the stream, releasing any associated resources.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if (disposing && IsStreamOwner)
			{
				baseStream.Dispose();
			}
		}

		/// <summary>
		/// Read a byte from stream advancing position
		/// </summary>
		/// <returns>byte read or -1 on end of stream</returns>
		public override int ReadByte()
		{
			if (streamEnd)
			{
				return -1; // ok
			}

			int retChar = currentChar;
			switch (currentState)
			{
				case NO_RAND_PART_B_STATE:
					SetupNoRandPartB();
					break;

				case NO_RAND_PART_C_STATE:
					SetupNoRandPartC();
					break;

				case START_BLOCK_STATE:
				case NO_RAND_PART_A_STATE:
					break;
			}
			return retChar;
		}

		#endregion Stream Overrides

		private void MakeMaps()
		{
			nInUse = 0;
			for (int i = 0; i < 256; ++i)
			{
				if (inUse[i])
				{
					seqToUnseq[nInUse] = (byte)i;
					unseqToSeq[i] = (byte)nInUse;
					nInUse++;
				}
			}
		}

		private void Initialize()
		{
			int marker1 = ReadBits(16);
			int marker2 = ReadBits(8);
			char blockSize = ReadUChar();

			if (marker1 != BZip2Constants.StreamStartMarker1 || marker2 != BZip2Constants.StreamStartMarker2
				|| blockSize < '1' || blockSize > '9')
			{
				streamEnd = true;
				return;
			}

			SetDecompressStructureSizes(blockSize - '0');
		}

		private void InitBlock()
		{
			int marker1 = ReadBits(24);
			int marker2 = ReadBits(24);

			if (marker1 == BZip2Constants.StreamEndMarker1 && marker2 == BZip2Constants.StreamEndMarker2)
			{
				streamEnd = true;
				return;
			}

			if (marker1 != BZip2Constants.BlockHeaderMarker1 || marker2 != BZip2Constants.BlockHeaderMarker2)
            {
				BadBlockHeader();
				return;
            }

			// storedBlockCRC
			ReadInt32();

			ReadBits(1);

			GetAndMoveToFrontDecode();

			currentState = START_BLOCK_STATE;
		}

		private void FillBuffer()
		{
			int byteRead = 0;

			try
			{
				byteRead = baseStream.ReadByte();
			}
			catch (Exception)
			{
				CompressedStreamEOF();
			}

			if (byteRead == -1)
			{
				CompressedStreamEOF();
			}

			bitBuffer = (bitBuffer << 8) | (byteRead & 0xFF);
			bitCount += 8;
		}

		private int ReadBits(int n)
		{
			while (bitCount < n)
			{
				FillBuffer();
			}

			int v = (bitBuffer >> (bitCount - n)) & ((1 << n) - 1);
			bitCount -= n;
			return v;
		}

		private char ReadUChar()
		{
			return (char)ReadBits(8);
		}

		private int ReadInt32()
		{
			int result = ReadBits(8);
			result = (result << 8) | ReadBits(8);
			result = (result << 8) | ReadBits(8);
			result = (result << 8) | ReadBits(8);
			return result;
		}

		private void RecvDecodingTables()
		{
			char[][] len = new char[BZip2Constants.GroupCount][];
			for (int i = 0; i < BZip2Constants.GroupCount; ++i)
			{
				len[i] = new char[BZip2Constants.MaximumAlphaSize];
			}

			bool[] inUse16 = new bool[16];

			//--- Receive the mapping table ---
			for (int i = 0; i < 16; i++)
			{
				inUse16[i] = (ReadBits(1) == 1);
			}

			for (int i = 0; i < 16; i++)
			{
				if (inUse16[i])
				{
					for (int j = 0; j < 16; j++)
					{
						inUse[i * 16 + j] = (ReadBits(1) == 1);
					}
				}
				else
				{
					for (int j = 0; j < 16; j++)
					{
						inUse[i * 16 + j] = false;
					}
				}
			}

			MakeMaps();
			int alphaSize = nInUse + 2;

			//--- Now the selectors ---
			int nGroups = ReadBits(3);
			int nSelectors = ReadBits(15);

			for (int i = 0; i < nSelectors; i++)
			{
				int j = 0;
				while (ReadBits(1) == 1)
				{
					j++;
				}
				selectorMtf[i] = (byte)j;
			}

			//--- Undo the MTF values for the selectors. ---
			byte[] pos = new byte[BZip2Constants.GroupCount];
			for (int v = 0; v < nGroups; v++)
			{
				pos[v] = (byte)v;
			}

			for (int i = 0; i < nSelectors; i++)
			{
				int v = selectorMtf[i];
				byte tmp = pos[v];
				while (v > 0)
				{
					pos[v] = pos[v - 1];
					v--;
				}
				pos[0] = tmp;
				selector[i] = tmp;
			}

			//--- Now the coding tables ---
			for (int t = 0; t < nGroups; t++)
			{
				int curr = ReadBits(5);
				for (int i = 0; i < alphaSize; i++)
				{
					while (ReadBits(1) == 1)
					{
						if (ReadBits(1) == 0)
						{
							curr++;
						}
						else
						{
							curr--;
						}
					}
					len[t][i] = (char)curr;
				}
			}

			//--- Create the Huffman decoding tables ---
			for (int t = 0; t < nGroups; t++)
			{
				int minLen = 32;
				int maxLen = 0;
				for (int i = 0; i < alphaSize; i++)
				{
					maxLen = Math.Max(maxLen, len[t][i]);
					minLen = Math.Min(minLen, len[t][i]);
				}
				HbCreateDecodeTables(limit[t], baseArray[t], perm[t], len[t], minLen, maxLen, alphaSize);
				minLens[t] = minLen;
			}
		}

		private void GetAndMoveToFrontDecode()
		{
			byte[] yy = new byte[256];
			int nextSym;

			int limitLast = BZip2Constants.BaseBlockSize * blockSize100k;
			origPtr = ReadBits(24);

			RecvDecodingTables();
			int EOB = nInUse + 1;
			int groupNo = -1;
			int groupPos = 0;

			/*--
			Setting up the unzftab entries here is not strictly
			necessary, but it does save having to do it later
			in a separate pass, and so saves a block's worth of
			cache misses.
			--*/
			for (int i = 0; i <= 255; i++)
			{
				unzftab[i] = 0;
			}

			for (int i = 0; i <= 255; i++)
			{
				yy[i] = (byte)i;
			}

			last = -1;

			if (groupPos == 0)
			{
				groupNo++;
				groupPos = BZip2Constants.GroupSize;
			}

			groupPos--;
			int zt = selector[groupNo];
			int zn = minLens[zt];
			int zvec = ReadBits(zn);
			int zj;

			while (zvec > limit[zt][zn])
			{
				if (zn > 20)
				{ // the longest code
					throw new BZip2Exception("Bzip data error");
				}
				zn++;
				while (bitCount < 1)
				{
					FillBuffer();
				}
				zj = (bitBuffer >> (bitCount - 1)) & 1;
				bitCount--;
				zvec = (zvec << 1) | zj;
			}
			if (zvec - baseArray[zt][zn] < 0 || zvec - baseArray[zt][zn] >= BZip2Constants.MaximumAlphaSize)
			{
				throw new BZip2Exception("Bzip data error");
			}
			nextSym = perm[zt][zvec - baseArray[zt][zn]];

			while (true)
			{
				if (nextSym == EOB)
				{
					break;
				}

				if (nextSym == BZip2Constants.RunA || nextSym == BZip2Constants.RunB)
				{
					int s = -1;
					int n = 1;
					do
					{
						if (nextSym == BZip2Constants.RunA)
						{
							s += (0 + 1) * n;
						}
						else if (nextSym == BZip2Constants.RunB)
						{
							s += (1 + 1) * n;
						}

						n <<= 1;

						if (groupPos == 0)
						{
							groupNo++;
							groupPos = BZip2Constants.GroupSize;
						}

						groupPos--;

						zt = selector[groupNo];
						zn = minLens[zt];
						zvec = ReadBits(zn);

						while (zvec > limit[zt][zn])
						{
							zn++;
							while (bitCount < 1)
							{
								FillBuffer();
							}
							zj = (bitBuffer >> (bitCount - 1)) & 1;
							bitCount--;
							zvec = (zvec << 1) | zj;
						}
						nextSym = perm[zt][zvec - baseArray[zt][zn]];
					} while (nextSym == BZip2Constants.RunA || nextSym == BZip2Constants.RunB);

					s++;
					byte ch = seqToUnseq[yy[0]];
					unzftab[ch] += s;

					while (s > 0)
					{
						last++;
						ll8[last] = ch;
						s--;
					}

					if (last >= limitLast)
					{
						BlockOverrun();
					}
					continue;
				}
				else
				{
					last++;
					if (last >= limitLast)
					{
						BlockOverrun();
					}

					byte tmp = yy[nextSym - 1];
					unzftab[seqToUnseq[tmp]]++;
					ll8[last] = seqToUnseq[tmp];

					Buffer.BlockCopy(yy, 0, yy, 1, nextSym - 1);

					yy[0] = tmp;

					if (groupPos == 0)
					{
						groupNo++;
						groupPos = BZip2Constants.GroupSize;
					}

					groupPos--;
					zt = selector[groupNo];
					zn = minLens[zt];
					zvec = ReadBits(zn);
					while (zvec > limit[zt][zn])
					{
						zn++;
						while (bitCount < 1)
						{
							FillBuffer();
						}
						zj = (bitBuffer >> (bitCount - 1)) & 1;
						bitCount--;
						zvec = (zvec << 1) | zj;
					}
					nextSym = perm[zt][zvec - baseArray[zt][zn]];
					continue;
				}
			}
		}

		private void SetupBlock()
		{
			int[] cftab = new int[257];

			cftab[0] = 0;
			Array.Copy(unzftab, 0, cftab, 1, 256);

			for (int i = 1; i <= 256; i++)
			{
				cftab[i] += cftab[i - 1];
			}

			for (int i = 0; i <= last; i++)
			{
				byte ch = ll8[i];
				tt[cftab[ch]] = i;
				cftab[ch]++;
			}

			cftab = null;

			tPos = tt[origPtr];

			count = 0;
			i2 = 0;
			ch2 = 256;   /*-- not a char and not EOF --*/

			SetupNoRandPartA();
		}

		private void SetupNoRandPartA()
		{
			if (i2 <= last)
			{
				chPrev = ch2;
				ch2 = ll8[tPos];
				tPos = tt[tPos];
				i2++;

				currentChar = ch2;
				currentState = NO_RAND_PART_B_STATE;
			}
			else
			{
				InitBlock();
				SetupBlock();
			}
		}

		private void SetupNoRandPartB()
		{
			if (ch2 != chPrev)
			{
				currentState = NO_RAND_PART_A_STATE;
				count = 1;
				SetupNoRandPartA();
			}
			else
			{
				count++;
				if (count >= 4)
				{
					z = ll8[tPos];
					tPos = tt[tPos];
					currentState = NO_RAND_PART_C_STATE;
					j2 = 0;
					SetupNoRandPartC();
				}
				else
				{
					currentState = NO_RAND_PART_A_STATE;
					SetupNoRandPartA();
				}
			}
		}

		private void SetupNoRandPartC()
		{
			if (j2 < (int)z)
			{
				currentChar = ch2;
				j2++;
			}
			else
			{
				currentState = NO_RAND_PART_A_STATE;
				i2++;
				count = 0;
				SetupNoRandPartA();
			}
		}

		private void SetDecompressStructureSizes(int newSize100k)
		{
			if (!(0 <= newSize100k && newSize100k <= 9 && 0 <= blockSize100k && blockSize100k <= 9))
			{
				throw new BZip2Exception("Invalid block size");
			}

			blockSize100k = newSize100k;

			if (newSize100k == 0)
			{
				return;
			}

			int n = BZip2Constants.BaseBlockSize * newSize100k;
			ll8 = new byte[n];
			tt = new int[n];
		}

		private static void CompressedStreamEOF()
		{
			throw new EndOfStreamException("BZip2 input stream end of compressed stream");
		}

		private static void BlockOverrun()
		{
			throw new BZip2Exception("BZip2 input stream block overrun");
		}

		private static void BadBlockHeader()
		{
			throw new BZip2Exception("BZip2 input stream bad block header");
		}

		private static void HbCreateDecodeTables(int[] limit, int[] baseArray, int[] perm, char[] length, int minLen, int maxLen, int alphaSize)
		{
			int pp = 0;

			for (int i = minLen; i <= maxLen; ++i)
			{
				for (int j = 0; j < alphaSize; ++j)
				{
					if (length[j] == i)
					{
						perm[pp] = j;
						++pp;
					}
				}
			}

			for (int i = 0; i < BZip2Constants.MaximumCodeLength; i++)
			{
				baseArray[i] = 0;
			}

			for (int i = 0; i < alphaSize; i++)
			{
				++baseArray[length[i] + 1];
			}

			for (int i = 1; i < BZip2Constants.MaximumCodeLength; i++)
			{
				baseArray[i] += baseArray[i - 1];
			}

			for (int i = 0; i < BZip2Constants.MaximumCodeLength; i++)
			{
				limit[i] = 0;
			}

			int vec = 0;

			for (int i = minLen; i <= maxLen; i++)
			{
				vec += (baseArray[i + 1] - baseArray[i]);
				limit[i] = vec - 1;
				vec <<= 1;
			}

			for (int i = minLen + 1; i <= maxLen; i++)
			{
				baseArray[i] = ((limit[i - 1] + 1) << 1) - baseArray[i];
			}
		}
	}
}