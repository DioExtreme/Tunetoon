using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Tunetoon.BZip2
{
	/// <summary>
	/// Provides simple <see cref="Stream"/>" utilities.
	/// </summary>
	public static class StreamUtils
	{
		/// <summary>
		/// Copy the contents of one <see cref="Stream"/> to another.
		/// </summary>
		/// <param name="source">The stream to source data from.</param>
		/// <param name="destination">The stream to write data to.</param>
		/// <param name="buffer">The buffer to use during copying.</param>
		public static void Copy(Stream source, Stream destination, byte[] buffer)
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			if (destination == null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			if (buffer == null)
			{
				throw new ArgumentNullException(nameof(buffer));
			}

			// Ensure a reasonable size of buffer is used without being prohibitive.
			if (buffer.Length < 128)
			{
				throw new ArgumentException("Buffer is too small", nameof(buffer));
			}

			bool copying = true;

			while (copying)
			{
				int bytesRead = source.Read(buffer, 0, buffer.Length);
				if (bytesRead > 0)
				{
					destination.Write(buffer, 0, bytesRead);
				}
				else
				{
					destination.Flush();
					copying = false;
				}
			}
		}
	}
}