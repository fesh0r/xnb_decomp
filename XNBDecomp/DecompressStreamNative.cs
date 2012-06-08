using System;
using System.IO;

namespace XNBDecomp
{
    internal class DecompressStreamNative : Stream
    {
        private Stream baseStream;
        private XnaNativeMethods nativeMethods;

        private byte[] compressedBuffer;
        private const int CompressedBufferSize = 0x10000;
        private int compressedPosition;
        private int compressedSize;
        private int compressedTodo;

        private byte[] decompressedBuffer;
        private const int DecompressedBufferSize = 0x10000;
        private int decompressedPosition;
        private int decompressedSize;
        private int decompressedTodo;

        private IntPtr decompressionContext;

        public DecompressStreamNative(Stream baseStream, int compressedTodo, int decompressedTodo)
        {
            nativeMethods = XnaNativeMethods.Instance;

            this.baseStream = baseStream;

            this.compressedTodo = compressedTodo;
            this.decompressedTodo = decompressedTodo;

            this.compressedBuffer = new byte[CompressedBufferSize];
            this.decompressedBuffer = new byte[DecompressedBufferSize];

            this.decompressionContext = nativeMethods.CreateDecompressionContext();
            if (this.decompressionContext == IntPtr.Zero)
            {
                throw new InvalidOperationException("Error decompressing content data.");
            }
        }

        private unsafe bool DecompressNextBuffer()
        {
            if (this.decompressedTodo <= 0)
            {
                return false;
            }

            do
            {
                if (this.compressedPosition >= this.compressedSize)
                {
                    this.ReadNextBufferFromDisk();
                }

                int sourceSize = this.compressedSize - this.compressedPosition;
                int outputSize = DecompressedBufferSize;

                fixed (byte* inBuff = this.compressedBuffer)
                {
                    fixed (byte* outBuff = this.decompressedBuffer)
                    {
                        if (nativeMethods.Decompress(this.decompressionContext, (void*)outBuff, ref outputSize, (void*)(inBuff + this.compressedPosition), ref sourceSize) != 0)
                        {
                            throw new InvalidOperationException("Error decompressing content data.");
                        }
                    }
                }

                if ((outputSize == 0) && (sourceSize == 0))
                {
                    throw new InvalidOperationException("Error decompressing content data.");
                }

                this.compressedPosition += sourceSize;
                this.decompressedTodo -= outputSize;
                this.decompressedSize = outputSize;
                this.decompressedPosition = 0;
            }
            while (this.decompressedSize == 0);

            return true;
        }

        protected override void Dispose(bool disposing)
        {
            if (this.decompressionContext != IntPtr.Zero)
            {
                nativeMethods.DestroyDecompressionContext(this.decompressionContext);
                this.decompressionContext = IntPtr.Zero;
            }

            base.Dispose(disposing);
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if ((this.decompressedPosition >= this.decompressedSize) && !this.DecompressNextBuffer())
            {
                return 0;
            }

            int remaining = this.decompressedSize - this.decompressedPosition;
            if (count > remaining)
            {
                count = remaining;
            }

            Array.Copy(this.decompressedBuffer, this.decompressedPosition, buffer, offset, count);

            this.decompressedPosition += count;

            return count;
        }

        private void ReadBufferFromDisk(byte[] buffer, ref int bufferTodo, out int bufferSize)
        {
            int bytesRead;

            int bytesToRead = CompressedBufferSize;
            if (bytesToRead > bufferTodo)
            {
                bytesToRead = bufferTodo;
            }

            for (int i = 0; i < bytesToRead; i += bytesRead)
            {
                bytesRead = this.baseStream.Read(buffer, i, bytesToRead - i);
                if (bytesRead == 0)
                {
                    throw new InvalidOperationException("Error decompressing content data.");
                }
            }

            bufferTodo -= bytesToRead;
            bufferSize = bytesToRead;
        }

        public override int ReadByte()
        {
            if ((this.decompressedPosition >= this.decompressedSize) && !this.DecompressNextBuffer())
            {
                return -1;
            }

            return this.decompressedBuffer[this.decompressedPosition++];
        }

        private void ReadNextBufferFromDisk()
        {
            if (this.compressedTodo > 0)
            {
                this.ReadBufferFromDisk(this.compressedBuffer, ref this.compressedTodo, out this.compressedSize);
                this.compressedPosition = 0;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public override long Length
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }
    }
}
