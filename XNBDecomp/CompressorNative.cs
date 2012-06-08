using System;
using System.IO;

namespace XNBDecomp
{
    internal class CompressorNative : IDisposable
    {
        private Stream outputStream;
        private XnaNativeMethods nativeMethods;
        private byte[] tempBuffer;

        private IntPtr compressionContext;

        public unsafe CompressorNative(Stream outputStream)
        {
            nativeMethods = XnaNativeMethods.Instance;

            this.outputStream = outputStream;
            this.tempBuffer = new byte[0x10000];

            this.compressionContext = nativeMethods.CreateCompressionContext();
            if (this.compressionContext == IntPtr.Zero)
            {
                throw new InvalidOperationException("Error compressing content data.");
            }
        }

        public void Compress(byte[] sourceData, int sourceLength)
        {
            this.CompressWorker(sourceData, sourceLength, false);
        }

        private unsafe void CompressWorker(byte[] sourceData, int sourceLength, bool flushMode)
        {
            if (sourceLength == 0)
            {
                sourceData = new byte[1];
            }

            int compressedPosition = 0;
            while (flushMode || compressedPosition < sourceLength)
            {
                int sourceSize = sourceLength - compressedPosition;
                int outputSize = this.tempBuffer.Length;
                fixed (byte* inBuff = sourceData)
                {
                    fixed (byte* outBuff = this.tempBuffer)
                    {
                        if (nativeMethods.Compress(this.compressionContext, (void*)outBuff, ref outputSize, (void*)(inBuff + compressedPosition), ref sourceSize) != 0)
                        {
                            throw new InvalidOperationException("Error compressing content data.");
                        }
                    }
                }

                if (outputSize > 0)
                {
                    this.outputStream.Write(this.tempBuffer, 0, outputSize);
                    if (flushMode)
                    {
                        continue;
                    }
                }
                else
                {
                    if (flushMode)
                    {
                        break;
                    }
                }
                compressedPosition += sourceSize;
            }
        }

        protected unsafe virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.compressionContext != IntPtr.Zero)
                {
                    nativeMethods.DestroyCompressionContext(this.compressionContext);
                    this.compressionContext = IntPtr.Zero;
                }
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        public void FlushOutput()
        {
            this.CompressWorker(null, 0, true);
        }
    }
}
