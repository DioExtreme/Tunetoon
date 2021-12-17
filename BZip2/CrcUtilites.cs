namespace Tunetoon.BZip2
{
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