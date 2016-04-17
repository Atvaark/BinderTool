using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BinderTool.Core;
using BinderTool.Core.Bdf4;
using BinderTool.Core.Bdt5;
using BinderTool.Core.Bhd5;
using BinderTool.Core.Bhf4;
using BinderTool.Core.Bnd4;
using BinderTool.Core.Common;
using BinderTool.Core.Dcx;
using BinderTool.Core.Sl2;

namespace BinderTool
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Run(args);
        }

        private static void Run(string[] args)
        {
            if (args.Length == 0)
            {
                ShowUsageInfo();
                return;
            }

            Options options;
            try
            {
                options = Options.Parse(args);
            }
            catch (FormatException e)
            {
                Console.WriteLine(e.Message);
                ShowUsageInfo();
                return;
            }

            switch (options.InputType)
            {
                // These files have a single output file. 
                case FileType.EncryptedBhd:
                case FileType.Dcx:
                    break;
                default:
                    Directory.CreateDirectory(options.OutputPath);
                    break;
            }

            switch (options.InputType)
            {
                case FileType.Regulation:
                    throw new NotImplementedException();
                case FileType.Dcx:
                    UnpackDcxFile(options);
                    break;
                case FileType.EncryptedBdt:
                    UnpackBdtFile(options);
                    break;
                case FileType.EncryptedBhd:
                    UnpackBhdFile(options);
                    break;
                case FileType.Bdt:
                    UnpackBdf4File(options);
                    break;
                case FileType.Bhd:
                    throw new NotImplementedException();
                case FileType.Bnd:
                    UnpackBndFile(options);
                    break;
                case FileType.Savegame:
                    UnpackSl2File(options);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(string.Format("Unable to handle type '{0}'", options.InputType));
            }
        }

        private static void ShowUsageInfo()
        {
            Console.WriteLine(
                "BinderTool by Atvaark\n" +
                "  A tool for unpacking Dark Souls III Bdt, Bhd, Dcx and Sl2 files\n" +
                "Usage:\n" +
                "  BinderTool file_path [output_path]\n" +
                "Examples:\n" +
                "  BinderTool data1.bhd data1");
        }

        private static List<string> GetArchiveNamesFromFileName(string archiveFileName)
        {
            List<string> archiveNames;
            switch (archiveFileName)
            {
                default:
                    archiveNames = new List<string>()
                    {   
                        "action",
                        "adhoc",
                        "aiscript",
                        "capture",
                        "cap_dbgsaveload",
                        "cap_debugmenu",
                        "chr",
                        "chranibnd",
                        "chresd",
                        "chresdpatch",
                        "config",
                        "dbgai",
                        "debug",
                        "event",
                        "facegen",
                        "font",
                        "map",
                        "menu",
                        "msg",
                        "mtd",
                        "obj",
                        "other",
                        "param",
                        "paramdef",
                        "parampatch",
                        "parts",
                        "patch_sfxbnd",
                        "regulation",
                        "script",
                        "sfx",
                        "sfxbnd",
                        "shader",
                        "stayparamdef",
                        "system",
                        "temp",
                        "testdata",
                        "title"
                    };
                    break;
            }
            return archiveNames;
        }

        private static void UnpackBdtFile(Options options)
        {
            string dictionaryPath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Dictionary.csv");
            FileNameDictionary dictionary = FileNameDictionary.OpenFromFile(dictionaryPath);

            string fileNameWithoutExtension = Path.GetFileName(options.InputPath).Replace(".bdt", "");

            List<string> archiveNames = GetArchiveNamesFromFileName(fileNameWithoutExtension);
            Bdt5FileStream bdtStream = Bdt5FileStream.OpenFile(options.InputPath, FileMode.Open, FileAccess.Read);

            MemoryStream bhdStream = DecryptBhdFile(Path.ChangeExtension(options.InputPath, "bhd"));
            Bhd5File bhdFile = Bhd5File.Read(bhdStream);

            long eof = bdtStream.Length;
            bhdFile.GetBuckets()
                .SelectMany(b => b.GetEntries())
                .OrderByDescending(e => e.FileOffset)
                .Aggregate(eof, (pos, entry) =>
                {
                    long entrySize = pos - entry.FileOffset;
                    if (entry.FileSize == 0)
                    {
                        //Debug.WriteLine("{0}\t0 size", entry.FileOffset);
                    }

                    if (entrySize != entry.FileSize)
                    {
                        long diff = Math.Abs(entrySize - entry.FileSize);
                        //long offsnew = entry.FileOffset + entrySize;
                        //Debug.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}", entry.FileOffset, offsnew, entrySize, entry.FileSize, diff);

                        entry.PaddedFileSize = entrySize;
                        if (entry.AesKey != null && diff >= 16)
                        {
                            // Fixes entries with length "0", which probably mean "unknown length"
                            // If the diff is < 16, then it's probably AES encrypted and has padding.
                            // TODO: Do this late and only if the file size can't get read. 
                            //entry.FileSize = entrySize;
                        }
                    }

                    return entry.FileOffset;
                });

            foreach (var bucket in bhdFile.GetBuckets())
            {
                foreach (var entry in bucket.GetEntries())
                {
                    MemoryStream data;
                    bool encrypted = entry.AesKey != null;

                    if (entry.FileSize == 0)
                    {
                        const int sampleLength = 48;
                        data = bdtStream.Read(entry.FileOffset, sampleLength);

                        if (encrypted)
                        {
                            data = CryptographyUtility.DecryptAesEcb(data, entry.AesKey.Key);
                        }

                        string sampleSignature;
                        if (!TryGetSignature(data, out sampleSignature)
                            || sampleSignature != DcxFile.DcxSignature)
                        {
                            Console.WriteLine("Unable to determine the length of file '{0:D10}'", entry.FileNameHash);
                            continue;
                        }

                        entry.FileSize = DcxFile.DcxSize + DcxFile.ReadCompressedSize(data);
                    }

                    if (encrypted)
                    {
                        data = bdtStream.Read(entry.FileOffset, entry.PaddedFileSize ?? entry.FileSize);
                        // BUG: DCX files are encrypted one more time (offset 78)
                        // BUG: BHF4 files are encrypted one more time (offset 1024)
                        data = CryptographyUtility.DecryptAesEcb(data, entry.AesKey.Key);
                        data.SetLength(entry.FileSize);
                    }
                    else
                    {
                        data = bdtStream.Read(entry.FileOffset, entry.FileSize);
                    }

                    string fileName;
                    string extension;
                    if (!dictionary.TryGetFileName(entry.FileNameHash, archiveNames, out fileName))
                    {
                        extension = GetDataExtension(data);
                        fileName = string.Format(
                            "{0:D10}_{1}{2}",
                            entry.FileNameHash,
                            fileNameWithoutExtension,
                            extension);
                    }
                    else
                    {
                        extension = Path.GetExtension(fileName);
                    }

                    Debug.WriteLine(
                        "{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}",
                        fileNameWithoutExtension,
                        fileName,
                        extension,
                        entry.FileNameHash,
                        entry.FileNameHashUnknown,
                        entry.FileOffset,
                        entry.FileSize,
                        entry.PaddedFileSize,
                        encrypted);

                    string newFileNamePath = Path.Combine(options.OutputPath, fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(newFileNamePath));
                    File.WriteAllBytes(newFileNamePath, data.ToArray());
                }
            }
        }

        private static string GetDataExtension(MemoryStream data)
        {
            string signature;
            string extension;
            if (!TryGetSignature(data, out signature)
                || !TryGetFileExtension(signature, out extension))
            {
                
                Debug.WriteLine(
                    string.Format("Unknown signature: '{0}'",
                    BitConverter.ToString(Encoding.ASCII.GetBytes(signature)).Replace("-", " ")));
                return ".bin";
            }

            return extension;
        }

        private static bool TryGetSignature(MemoryStream stream, out string signature)
        {
            signature = null;
            if (stream.Length < 4)
            {
                return false;
            }

            BinaryReader reader = new BinaryReader(stream, Encoding.ASCII, true);
            signature = new string(reader.ReadChars(4));
            stream.Position = 0;

            return true;
        }

        private static bool TryGetFileExtension(string signature, out string extension)
        {
            extension = null;
            switch (signature)
            {
                case "BND4":
                    extension = ".bnd";
                    return true;
                case "BHF4":
                    extension = ".bhd";
                    return true;
                case "BDF4":
                    extension = ".bdt";
                    return true;
                case "DCX\0":
                    extension = ".dcx";
                    return true;
                case "TAE ":
                    extension = ".tae";
                    return true;
                case "fSSL":
                    extension = ".fssl";
                    return true;
                case "TPF\0":
                    extension = ".tpf";
                    return true;
                case "PFBB":
                    extension = ".pfbb";
                    return true;
                case "OBJB":
                    extension = ".breakobj";
                    return true;
                case "filt":
                    extension = ".fltparam";
                    return true;
                case "VSDF":
                    extension = ".vsd";
                    return true;
                case "NVG2":
                    extension = ".ngp";
                    return true;
                case "#BOM":
                    extension = ".txt";
                    return true;
                case "\x1BLua":
                    extension = ".lua";
                    return true;
                case "DDS ":
                    extension = ".dds";
                    return true;
                case "RIFF":
                    extension = ".fev";
                    return true;
                case "FSB5":
                    extension = ".fsb";
                    return true;
                case "GFX\v":
                    extension = ".gfx";
                    return true;
                case "ENFL":
                    extension = ".enf"; // ?
                    return true;
                default:
                    extension = ".bin";
                    break;
            }
            return false;
        }

        private static void UnpackBhdFile(Options options)
        {
            using (var inputStream = DecryptBhdFile(options.InputPath))
            using (var outputStream = File.OpenWrite(options.OutputPath))
            {
                inputStream.WriteTo(outputStream);
            }
        }

        private static void UnpackBndFile(Options options)
        {
            using (FileStream inputStream = new FileStream(options.InputPath, FileMode.Open))
            {
                UnpackBndFile(inputStream, options.OutputPath);
            }
        }

        private static void UnpackBndFile(Stream inputStream, string outputPath)
        {
            Bnd4File file = Bnd4File.ReadBnd4File(inputStream);

            foreach (var entry in file.Entries)
            {
                string fileName = FileNameDictionary.NormalizeFileName(entry.FileName);
                string outputFilePath = Path.Combine(outputPath, fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
                File.WriteAllBytes(outputFilePath, entry.EntryData);
            }
        }

        private static void UnpackSl2File(Options options)
        {
            using (FileStream inputStream = new FileStream(options.InputPath, FileMode.Open))
            {
                Sl2File sl2File = Sl2File.ReadSl2File(inputStream);
                foreach (var userData in sl2File.UserData)
                {
                    string outputFilePath = Path.Combine(options.OutputPath, userData.UserDataName);
                    File.WriteAllBytes(outputFilePath, userData.DecryptedUserData);
                }
            }
        }

        private static void UnpackRegulationFile(Options options)
        {
            //using (FileStream inputStream = new FileStream(options.InputPath, FileMode.Open))
            //{
            //    RegulationFile encryptedRegulationFile = RegulationFile.ReadRegulationFile(inputStream);
            //    DcxFile compressedRegulationFile = DcxFile.Read(new MemoryStream(encryptedRegulationFile.DecryptedData));
            //    UnpackBndFile(new MemoryStream(compressedRegulationFile.d), options.OutputPath);
            //}
        }

        private static void UnpackDcxFile(Options options)
        {
            string unpackedFileName = Path.GetFileNameWithoutExtension(options.InputPath);
            string outputFilePath = options.OutputPath;
            bool hasExtension = Path.GetExtension(unpackedFileName) != "";

            using (FileStream inputStream = new FileStream(options.InputPath, FileMode.Open))
            {
                DcxFile dcxFile = DcxFile.Read(inputStream);
                byte[] decompressedData = dcxFile.Decompress();

                if (!hasExtension)
                {
                    string extension = GetDataExtension(new MemoryStream(decompressedData));
                    if (extension != ".dcx")
                    {
                        outputFilePath += extension;
                    }
                }

                File.WriteAllBytes(outputFilePath, decompressedData);
            }
        }

        private static void UnpackBdf4File(Options options)
        {
            var bdfDirectory = Path.GetDirectoryName(options.InputPath);
            var bhf4FilePath = options.InputPath.Substring(0, options.InputPath.Length - 3) + "bhd";

            if (File.Exists(bhf4FilePath) == false)
            {
                // HACK: Adding 132 to a hash of a text that ends with XXX.bdt will give you the hash of XXX.bhd.
                string[] split = Path.GetFileNameWithoutExtension(options.InputPath).Split('_');
                uint hash;
                if (uint.TryParse(split[0], out hash))
                {
                    hash += 132;
                    split[0] = hash.ToString("D10");
                    bhf4FilePath = Path.Combine(bdfDirectory, String.Join("_", split) + ".bhd");
                }
            }

            using (FileStream bhf4InputStream = new FileStream(bhf4FilePath, FileMode.Open))
            using (FileStream bdf4InputStream = new FileStream(options.InputPath, FileMode.Open))
            {
                Bhf4File bhf4File = Bhf4File.ReadBhf4File(bhf4InputStream);
                Bdf4File bdf4File = Bdf4File.ReadBdf4File(bdf4InputStream);
                foreach (var file in bdf4File.ReadData(bdf4InputStream, bhf4File))
                {
                    ExportFile(file, options.OutputPath);
                }
            }
        }

        private static void ExportFile(DataContainer file, string outputPath)
        {
            string outputFilePath = Path.Combine(outputPath, file.Name);

            Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
            using (FileStream outputStream = new FileStream(outputFilePath, FileMode.Create))
            {
                file.DataStream.CopyTo(outputStream);
            }
        }

        private static MemoryStream DecryptBhdFile(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            string key = DecryptionKeys.GetFileKey(fileName);
            return CryptographyUtility.DecryptRsa(filePath, key);
        }
    }
}
