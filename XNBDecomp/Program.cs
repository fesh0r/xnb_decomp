using System;
using System.IO;
using System.Reflection;

namespace XNBDecomp
{
    class Program
    {
        private const string contentExtension = ".xnb";

        static int Main(string[] args)
        {
            Console.WriteLine("XNB decompressor - Fesh0r 2011");

            if (args.Length < 2)
            {
                string exeName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
                Console.WriteLine("Usage: {0} [ContentPath] [OutputPath]", exeName);
                return 1;
            }

            String srcDir = Path.GetFullPath(args[0]);
            String destDir = Path.GetFullPath(args[1]);

            if (!Directory.Exists(srcDir))
            {
                Console.WriteLine("Could not find source directory {0}", srcDir);
                return 1;
            }

            if (!Directory.Exists(destDir))
            {
                try
                {
                    Directory.CreateDirectory(destDir);
                }
                catch
                {
                    Console.WriteLine("Could not create output directory {0}", destDir);
                    return 1;
                }
            }

            Console.WriteLine("Decompressing XNBs in {0} to {1}", srcDir, destDir);

            string[] files = Directory.GetFiles(srcDir, "*" + contentExtension, SearchOption.AllDirectories);

            string assetName;
            string inDir;
            string outDir;
            string assetDir;

            foreach (string file in files)
            {
                assetName = Path.GetFileNameWithoutExtension(file);
                inDir = Path.GetDirectoryName(file);
                outDir = inDir.Replace(srcDir, destDir);
                assetDir = inDir.Replace(srcDir + @"\", "");

                try
                {
                    Directory.CreateDirectory(outDir);
                }
                catch
                {
                    Console.WriteLine("Could not create output directory {0}", outDir);
                    return 1;
                }

                Console.WriteLine(Path.Combine(assetDir, assetName));

                using (ContentReader cr = ContentReader.Create(Path.Combine(inDir, assetName + contentExtension)))
                {
                    using (ContentWriter cw = new ContentWriter(Path.Combine(outDir, assetName + contentExtension), false, cr.filePlatform, cr.fileVersion, cr.graphicsProfile))
                    {
                        try
                        {
                            for (int i = 0; i < cr.fileSize; i++)
                            {
                                cw.Write(cr.ReadByte());
                            }
                        }
                        catch
                        {
                            Console.WriteLine("FAIL!");
                        }

                        cw.FlushOutput();
                    }
                }
            }

            Console.WriteLine("Done!");
            return 0;
        }
    }
}
