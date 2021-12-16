using System;
using System.IO;

namespace Tunetoon.BZip2
{
	public static class BZip2Decompressor
	{
		/// <summary>
		/// Decompress the <paramref name="inStream">input</paramref> writing
		/// uncompressed data to the <paramref name="outStream">output stream</paramref>
		/// </summary>
		/// <param name="inStream">The readable stream containing data to decompress.</param>
		/// <param name="outStream">The output stream to receive the decompressed data.</param>
		/// <param name="isStreamOwner">Both streams are closed on completion if true.</param>
		public static void Decompress(Stream inStream, Stream outStream, bool isStreamOwner)
		{
			if (inStream == null)
				throw new ArgumentNullException(nameof(inStream));

			if (outStream == null)
				throw new ArgumentNullException(nameof(outStream));

			try
			{
				using (BZip2InputStream bzipInput = new BZip2InputStream(inStream))
				{
					bzipInput.IsStreamOwner = isStreamOwner;
					StreamUtils.Copy(bzipInput, outStream, new byte[4096]);
				}
			}
			finally
			{
				if (isStreamOwner)
				{
					// inStream is closed by the BZip2InputStream if stream owner
					outStream.Dispose();
				}
			}
		}
	}
}