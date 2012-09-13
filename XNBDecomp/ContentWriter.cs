using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace XNBDecomp
{
    public class ContentWriter : BinaryWriter
    {
        private char filePlatform;
        private byte fileVersion;
        private byte graphicsProfile;
        private bool compressContent;
        private Stream finalOutput;
        private MemoryStream contentData;

        private const char PlatformWindows = 'w';
        private const char PlatformXbox = 'x';
        private const char PlatformMobile = 'm';
        private const byte XnbVersion_30 = 3;
        private const byte XnbVersion_31 = 4;
        private const byte XnbVersion_40 = 5;
        private const int XnbFileSizeOffset = 6;
        private const int XnbPrologueSize = 10;
        private const int XnbVersionOffset = 4;
        private const byte XnbProfileMask = 0x7f;
        private const byte XnbCompressedMask = 0x80;

        internal ContentWriter(Stream output, bool compressContent = false, int filePlatform = PlatformWindows, int fileVersion = XnbVersion_40, int graphicsProfile = 0)
        {
            this.finalOutput = output;
            this.contentData = new MemoryStream();

            this.filePlatform = (char)filePlatform;

            this.fileVersion = (byte)fileVersion;

            if (fileVersion >= XnbVersion_40)
            {
                this.graphicsProfile = (byte)graphicsProfile;
            }
            else
            {
                this.graphicsProfile = 0;
            }

            this.compressContent = false;

            base.OutStream = this.contentData;
        }

        internal ContentWriter(String filename, bool compressContent = false, int filePlatform = PlatformWindows, int fileVersion = XnbVersion_40, int graphicsProfile = 0)
            : this(File.Open(filename, FileMode.Create), compressContent, filePlatform, fileVersion, graphicsProfile)
        {
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    this.contentData.Dispose();
                }
            }
            finally
            {
                base.OutStream = this.contentData;
                base.Dispose(disposing);
            }
        }

        internal void FlushOutput()
        {
            this.WriteFinalOutput();
        }

        private void WriteFinalOutput()
        {
            base.OutStream = this.finalOutput;
            this.Write('X');
            this.Write('N');
            this.Write('B');

            this.Write(this.filePlatform);

            this.WriteUncompressedOutput();
        }

        private void WriteUncompressedOutput()
        {
            this.WriteVersionNumber();

            int contentLength = (int)this.contentData.Length;
            this.Write((int)(XnbPrologueSize + contentLength));

            base.OutStream.Write(this.contentData.GetBuffer(), 0, contentLength);
        }

        private void WriteVersionNumber()
        {
            this.Write(this.fileVersion);

            byte profile = (byte)((this.graphicsProfile & XnbProfileMask) | (this.compressContent ? XnbCompressedMask : 0));
            this.Write(profile);
        }
    }
}
