using System;
using System.IO;

namespace XNBDecomp
{
    public class ContentReader : BinaryReader
    {
        internal int filePlatform;
        internal int fileVersion;
        internal bool compressed;
        internal int fileSize;
        internal int graphicsProfile;

        private const char PlatformWindows = 'w';
        private const char PlatformXbox = 'x';
        private const char PlatformMobile = 'm';
        private const byte XnbVersion_30 = 3;
        private const byte XnbVersion_31 = 4;
        private const byte XnbVersion_40 = 5;
        private const int XnbCompressedPrologueSize = 14;
        private const int XnbPrologueSize = 10;
        private const byte XnbProfileMask = 0x7f;
        private const byte XnbCompressedMask = 0x80;

        private ContentReader(Stream input, int filePlatform, int fileVersion, int graphicsProfile, bool compressed, int fileSize)
            : base(input)
        {
            this.filePlatform = filePlatform;
            this.fileVersion = fileVersion;
            this.graphicsProfile = graphicsProfile;
            this.compressed = compressed;
            this.fileSize = fileSize;
        }

        internal static ContentReader Create(Stream input)
        {
            BinaryReader reader = new BinaryReader(input);

            if (((reader.ReadByte() != 'X') || (reader.ReadByte() != 'N')) || (reader.ReadByte() != 'B'))
            {
                throw new InvalidOperationException("Bad magic.");
            }

            int filePlatform = reader.ReadByte();
            if (filePlatform != PlatformWindows && filePlatform != PlatformXbox)
            {
                throw new InvalidOperationException("Bad platform.");
            }

            int fileVersion = reader.ReadByte();
            if (fileVersion > XnbVersion_40)
            {
                throw new InvalidOperationException("Bad version.");
            }

            int num = reader.ReadByte();
            bool compressed = false;
            int graphicsProfile = 0;
            if (fileVersion >= XnbVersion_40)
            {
                graphicsProfile = num & XnbProfileMask;
            }
            if (fileVersion >= XnbVersion_30)
            {
                compressed = (num & XnbCompressedMask) == XnbCompressedMask;
            }

            int fileSize = reader.ReadInt32();
            if (input.CanSeek && ((fileSize - XnbPrologueSize) > (input.Length - input.Position)))
            {
                throw new InvalidOperationException("Bad size.");
            }

            if (compressed)
            {
                int compressedTodo = fileSize - XnbCompressedPrologueSize;
                fileSize = reader.ReadInt32();
                //input = new DecompressStreamNative(input, compressedTodo, fileSize);
                //input = DecompressStreamOld.getStream(input, compressedTodo, fileSize);
                input = DecompressStream.getStream(input, compressedTodo, fileSize);
            }
            else
            {
                fileSize = fileSize - XnbPrologueSize;
            }

            return new ContentReader(input, filePlatform, fileVersion, graphicsProfile, compressed, fileSize);
        }

        internal static ContentReader Create(String filename)
        {
            return Create(File.Open(filename, FileMode.Open));
        }
    }
}
