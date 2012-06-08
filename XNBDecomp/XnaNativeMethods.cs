using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Security;
using System.IO;
using System.ComponentModel;

namespace XNBDecomp
{
    internal sealed class XnaNativeMethods
    {
        private static readonly XnaNativeMethods instance = new XnaNativeMethods();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private unsafe delegate void* dCreateDecompressionContext();
        private dCreateDecompressionContext _CreateDecompressionContext;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private unsafe delegate void dDestroyDecompressionContext(void* context);
        private dDestroyDecompressionContext _DestroyDecompressionContext;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private unsafe delegate int dDecompress(void* context, void* outputData, int* outputSize, void* sourceData, int* sourceSize);
        private dDecompress _Decompress;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private unsafe delegate void* dCreateCompressionContext();
        private dCreateCompressionContext _CreateCompressionContext;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private unsafe delegate void dDestroyCompressionContext(void* context);
        private dDestroyCompressionContext _DestroyCompressionContext;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private unsafe delegate int dCompress(void* context, void* outputData, int* outputSize, void* sourceData, int* sourceSize);
        private dCompress _Compress;

        private const string regPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\XNA\Framework\v4.0";
        private const string nativeDll = "XnaNative.dll";
        private const string missingNativeDependency = "Failed to load {0}. Please verify that you have the XNA Framework installed.";
        private UnmanagedLibrary xnaLib;

        public static XnaNativeMethods Instance
        {
            get
            {
                return instance;
            }
        }

        private XnaNativeMethods()
        {
            string nativePath = Registry.GetValue(regPath, "NativeLibraryPath", string.Empty) as String;
            if (nativePath == null)
            {
                throw new FileNotFoundException(string.Format(missingNativeDependency, nativeDll));
            }

            string fullNativeDll = Path.Combine(nativePath, nativeDll);

            xnaLib = new UnmanagedLibrary(fullNativeDll);

            _CreateDecompressionContext = xnaLib.GetUnmanagedFunction<dCreateDecompressionContext>("CreateDecompressionContext");
            _DestroyDecompressionContext = xnaLib.GetUnmanagedFunction<dDestroyDecompressionContext>("DestroyDecompressionContext");
            _Decompress = xnaLib.GetUnmanagedFunction<dDecompress>("Decompress");

            _CreateCompressionContext = xnaLib.GetUnmanagedFunction<dCreateCompressionContext>("CreateCompressionContext");
            _DestroyCompressionContext = xnaLib.GetUnmanagedFunction<dDestroyCompressionContext>("DestroyCompressionContext");
            _Compress = xnaLib.GetUnmanagedFunction<dCompress>("Compress");
        }

        public unsafe IntPtr CreateDecompressionContext()
        {
            return new IntPtr(_CreateDecompressionContext());
        }

        public unsafe void DestroyDecompressionContext(IntPtr context)
        {
            _DestroyDecompressionContext(context.ToPointer());
        }

        public unsafe int Decompress(IntPtr context, void* outputData, ref int outputSize, void* sourceData, ref int sourceSize)
        {
            if (context == IntPtr.Zero)
            {
                return unchecked((int)0x80070057);
            }

            int sOutputSize = outputSize;
            int sSourceSize = sourceSize;

            int err = _Decompress(context.ToPointer(), outputData, &sOutputSize, sourceData, &sSourceSize);
            if (err < 0)
            {
                return err;
            }

            outputSize = sOutputSize;
            sourceSize = sSourceSize;

            return 0;
        }

        public unsafe IntPtr CreateCompressionContext()
        {
            return new IntPtr(_CreateCompressionContext());
        }

        public unsafe void DestroyCompressionContext(IntPtr context)
        {
            _DestroyCompressionContext(context.ToPointer());
        }

        public unsafe int Compress(IntPtr context, void* outputData, ref int outputSize, void* sourceData, ref int sourceSize)
        {
            if (context == IntPtr.Zero)
            {
                return unchecked((int)0x80070057);
            }

            int sOutputSize = outputSize;
            int sSourceSize = sourceSize;

            int err = _Compress(context.ToPointer(), outputData, &sOutputSize, sourceData, &sSourceSize);
            if (err < 0)
            {
                return err;
            }

            outputSize = sOutputSize;
            sourceSize = sSourceSize;

            return 0;
        }

    }
}
