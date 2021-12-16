using System.IO;
using Tunetoon.BZip2;

namespace Tunetoon.Utilities
{
    /*
	The original bsdiff.c source code (http://www.daemonology.net/bsdiff/) is
	distributed under the following license:

	Copyright 2003-2005 Colin Percival
	All rights reserved

	Redistribution and use in source and binary forms, with or without
	modification, are permitted providing that the following conditions 
	are met:
	1. Redistributions of source code must retain the above copyright
		notice, this list of conditions and the following disclaimer.
	2. Redistributions in binary form must reproduce the above copyright
		notice, this list of conditions and the following disclaimer in the
		documentation and/or other materials provided with the distribution.

	THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR
	IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
	WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
	ARE DISCLAIMED.  IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY
	DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
	DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
	OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
	HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
	STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING
	IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
	POSSIBILITY OF SUCH DAMAGE.
    */
    internal static class BsPatch
    {
        private const long Bsdiff40 = 3473478480300364610L;
        private const int HeaderSize = 32;

        private static int ReadFileOffset(this BinaryReader binaryReader)
        {
            byte[] buffer = binaryReader.ReadBytes(8);

            int value = buffer[3] & 0x7F;
            for (int i = 2; i >= 0; i--)
            {
                value <<= 8;
                value |= buffer[i];
            }

            if ((buffer[7] & 0x80) != 0)
            {
                value = -value;
            }
            return value;
        }

        public static void Apply(string inputFile, string patchFile)
        {
            /*
			File format:
				0	8	"BSDIFF40"
				8	8	X
				16	8	Y
				24	8	sizeof(newfile)
				32	X	bzip2(control block)
				32+X	Y	bzip2(diff block)
				32+X+Y	???	bzip2(extra block)
			with control block a set of triples (x,y,z) meaning "add x bytes
			from oldfile to x bytes from the diff block; copy y bytes from the
			extra block; seek forwards in oldfile by z bytes".
			*/

            long controlLength, diffLength, newSize;
            Stream OpenPatchStream() => new FileStream(patchFile, FileMode.Open, FileAccess.Read);

            // read header
            using (Stream patchStream = OpenPatchStream())
            using (BinaryReader binaryReader = new BinaryReader(patchStream))
            {
                // check for appropriate magic
                long signature = binaryReader.ReadInt64();
                if (signature != Bsdiff40)
                {
                    return;
                }

                // read lengths from header
                controlLength = binaryReader.ReadInt64();
                diffLength = binaryReader.ReadInt64();
                newSize = binaryReader.ReadInt64();
                if (controlLength < 0 || diffLength < 0 || newSize < 0)
                {
                    return;
                }
            }

            byte[] newData;
            byte[] oldData;

            using (var controlStream = OpenPatchStream())
            using (var diffStream = OpenPatchStream())
            using (var extraStream = OpenPatchStream())
            {
                // seek streams
                controlStream.Seek(HeaderSize, SeekOrigin.Current);
                diffStream.Seek(HeaderSize + controlLength, SeekOrigin.Current);
                extraStream.Seek(HeaderSize + controlLength + diffLength, SeekOrigin.Current);

                using (var bz2ControlStream = new BZip2InputStream(controlStream))
                using (var bz2DiffStream = new BZip2InputStream(diffStream))
                using (var bz2ExtraStream = new BZip2InputStream(extraStream))
                using (var controlReader = new BinaryReader(bz2ControlStream))
                using (var diffReader = new BinaryReader(bz2DiffStream))
                using (var extraReader = new BinaryReader(bz2ExtraStream))
                using (var inputStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
                using (var outputStream = new FileStream(inputFile + ".tmp", FileMode.Create, FileAccess.Write))
                using (var inputReader = new BinaryReader(inputStream))
                using (var outputWriter = new BinaryWriter(outputStream))
                {
                    int oldPosition = 0;
                    int newPosition = 0;
                    while (newPosition < newSize)
                    {
                        // Read control data
                        int x = controlReader.ReadFileOffset();
                        int y = controlReader.ReadFileOffset();
                        int z = controlReader.ReadFileOffset();

                        // sanity-check
                        if (x < 0 || y < 0)
                        {
                            return;
                        }

                        // sanity-check
                        if (newPosition + x > newSize)
                        {
                            return;
                        }

                        // read diff string
                        newData = diffReader.ReadBytes(x);

                        // add old data to diff string
                        oldData = inputReader.ReadBytes(x);

                        for (int i = 0; i < x; i++)
                        {
                            newData[i] += oldData[i];
                        }

                        outputWriter.Write(newData);

                        // adjust position
                        newPosition += x;
                        oldPosition += x;

                        // sanity-check
                        if (newPosition + y > newSize)
                        {
                            return;
                        }

                        // read extra string
                        newData = extraReader.ReadBytes(y);
                        outputWriter.Write(newData);

                        // adjust position
                        newPosition += y;
                        oldPosition += z;
                        inputStream.Position = oldPosition;
                    }
                }
            }
        }
    }
}