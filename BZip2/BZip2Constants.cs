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
	/// Defines internal values for both compression and decompression
	/// </summary>
	internal static class BZip2Constants
	{
		/// <summary>
		/// First three bytes of block header marker
		/// </summary>
		public const int BlockHeaderMarker1 = 0x314159;

		/// <summary>
		/// Last three bytes of block header marker
		/// </summary>
		public const int BlockHeaderMarker2 = 0x265359;

		/// <summary>
		/// First three bytes of stream start marker
		/// </summary>
		public const int StreamStartMarker1 = 0x425a;

		/// <summary>
		/// Last three bytes of stream start marker
		/// </summary>
		public const int StreamStartMarker2 = 0x68;

		/// <summary>
		/// First three bytes of stream end marker
		/// </summary>
		public const int StreamEndMarker1 = 0x177245;

		/// <summary>
		/// Last three bytes of stream end marker
		/// </summary>
		public const int StreamEndMarker2 = 0x385090;

		/// <summary>
		/// When multiplied by compression parameter (1-9) gives the block size for compression
		/// 9 gives the best compression but uses the most memory.
		/// </summary>
		public const int BaseBlockSize = 100000;

		/// <summary>
		/// Backend constant
		/// </summary>
		public const int MaximumAlphaSize = 258;

		/// <summary>
		/// Backend constant
		/// </summary>
		public const int MaximumCodeLength = 23;

		/// <summary>
		/// Backend constant
		/// </summary>
		public const int RunA = 0;

		/// <summary>
		/// Backend constant
		/// </summary>
		public const int RunB = 1;

		/// <summary>
		/// Backend constant
		/// </summary>
		public const int GroupCount = 6;

		/// <summary>
		/// Backend constant
		/// </summary>
		public const int GroupSize = 50;

		/// <summary>
		/// Backend constant
		/// </summary>
		public const int NumberOfIterations = 4;

		/// <summary>
		/// Backend constant
		/// </summary>
		public const int MaximumSelectors = (2 + (900000 / GroupSize));

		/// <summary>
		/// Backend constant
		/// </summary>
		public const int OvershootBytes = 20;
	}
}