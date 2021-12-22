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

	internal static class CrcUtilities
	{
		/// <summary>
		/// The number of slicing lookup tables to generate.
		/// </summary>
		internal const int SlicingDegree = 16;

		/// <summary>
		/// Generates multiple CRC lookup tables for a given polynomial, stored
		/// in a linear array of uints. The first block (i.e. the first 256
		/// elements) is the same as the byte-by-byte CRC lookup table. 
		/// </summary>
		/// <param name="polynomial">The generating CRC polynomial</param>
		/// <param name="isReversed">Whether the polynomial is in reversed bit order</param>
		/// <returns>A linear array of 256 * <see cref="SlicingDegree"/> elements</returns>
		/// <remarks>
		/// This table could also be generated as a rectangular array, but the
		/// JIT compiler generates slower code than if we use a linear array.
		/// Known issue, see: https://github.com/dotnet/runtime/issues/30275
		/// </remarks>
		internal static uint[] GenerateSlicingLookupTable(uint polynomial, bool isReversed)
		{
			var table = new uint[256 * SlicingDegree];
			uint one = isReversed ? 1 : (1U << 31);

			for (int i = 0; i < 256; i++)
			{
				uint res = (uint)(isReversed ? i : i << 24);
				for (int j = 0; j < SlicingDegree; j++)
				{
					for (int k = 0; k < 8; k++)
					{
						if (isReversed)
						{
							res = (res & one) == 1 ? polynomial ^ (res >> 1) : res >> 1;
						}
						else
						{
							res = (res & one) != 0 ? polynomial ^ (res << 1) : res << 1;
						}
					}

					table[(256 * j) + i] = res;
				}
			}

			return table;
		}
	}
}