using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using BinderTool.Core;
using BinderTool.Core.Bdf4;
using BinderTool.Core.Bdt5;
using BinderTool.Core.Bhd5;
using BinderTool.Core.Bhf4;
using BinderTool.Core.Bnd4;
using BinderTool.Core.Dcx;
using BinderTool.Core.Regulation;
using BinderTool.Core.Sl2;

namespace BinderTool
{
    public static class Program
    {
        private static void Main(string[] args)
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
                    UnpackRegulationFile(options);
                    break;
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
                    UnpackBhf4File(options);
                    break;
                case FileType.Bnd:
                    UnpackBndFile(options);
                    break;
                case FileType.Savegame:
                    UnpackSl2File(options);
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Unable to handle type '{options.InputType}'");
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

            using (Bdt5FileStream bdtStream = Bdt5FileStream.OpenFile(options.InputPath, FileMode.Open, FileAccess.Read))
            {
                Bhd5File bhdFile = Bhd5File.Read(DecryptBhdFile(Path.ChangeExtension(options.InputPath, "bhd")));
                foreach (var bucket in bhdFile.GetBuckets())
                {
                    foreach (var entry in bucket.GetEntries())
                    {
                        MemoryStream data;
                        if (entry.FileSize == 0)
                        {
                            long fileSize;
                            if (!TryReadFileSize(entry, bdtStream, out fileSize))
                            {
                                Console.WriteLine("Unable to determine the length of file '{0:D10}'", entry.FileNameHash);
                                continue;
                            }

                            entry.FileSize = fileSize;
                        }

                        if (entry.IsEncrypted)
                        {
                            data = bdtStream.Read(entry.FileOffset, entry.PaddedFileSize);
                            CryptographyUtility.DecryptAesEcb(data, entry.AesKey.Key, entry.AesKey.Ranges);
                            data.Position = 0;
                            data.SetLength(entry.FileSize);
                        }
                        else
                        {
                            data = bdtStream.Read(entry.FileOffset, entry.FileSize);
                        }

                        string fileName;
                        string extension;
                        bool fileNameFound = dictionary.TryGetFileName(entry.FileNameHash, archiveNames, out fileName);
                        if (fileNameFound)
                        {
                            extension = Path.GetExtension(fileName);
                        }
                        else
                        {
                            extension = GetDataExtension(data);
                            fileName = $"{entry.FileNameHash:D10}_{fileNameWithoutExtension}{extension}";
                        }

                        // TODO: Handle .enc files (regulation:/regulation.regbnd.dcx.enc)
                        // Example: 2084217123_Data1.bin

                        if (extension == ".dcx")
                        {
                            DcxFile dcxFile = DcxFile.Read(data);
                            data = new MemoryStream(dcxFile.Decompress());

                            fileName = Path.GetFileNameWithoutExtension(fileName);

                            if (fileNameFound)
                            {
                                extension = Path.GetExtension(fileName);
                            }
                            else
                            {
                                extension = GetDataExtension(data);
                                fileName += extension;
                            }
                        }

                        Debug.WriteLine(
                            "{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}",
                            fileNameWithoutExtension,
                            fileName,
                            extension,
                            entry.FileNameHash,
                            entry.FileOffset,
                            entry.FileSize,
                            entry.PaddedFileSize,
                            entry.IsEncrypted,
                            fileNameFound);

                        string newFileNamePath = Path.Combine(options.OutputPath, fileName);
                        Directory.CreateDirectory(Path.GetDirectoryName(newFileNamePath));
                        File.WriteAllBytes(newFileNamePath, data.ToArray());
                    }
                }
            }
        }

        private static bool TryReadFileSize(Bhd5BucketEntry entry, Bdt5FileStream bdtStream, out long fileSize)
        {
            fileSize = 0;

            const int sampleLength = 48;
            MemoryStream data = bdtStream.Read(entry.FileOffset, sampleLength);

            if (entry.IsEncrypted)
            {
                data = CryptographyUtility.DecryptAesEcb(data, entry.AesKey.Key);
            }

            string sampleSignature;
            if (!TryGetAsciiSignature(data, 4, out sampleSignature)
                || sampleSignature != DcxFile.DcxSignature)
            {
                return false;
            }

            fileSize = DcxFile.DcxSize + DcxFile.ReadCompressedSize(data);
            return true;
        }

        private static string GetDataExtension(MemoryStream data)
        {
            string signature;
            string extension;

            if (TryGetAsciiSignature(data, 4, out signature)
                && TryGetFileExtension(signature, out extension))
            {
                return extension;
            }

            if (TryGetUnicodeSignature(data, 4, out signature)
                && TryGetFileExtension(signature, out extension))
            {
                return extension;
            }

            if (TryGetAsciiSignature(data, 26, out signature)
                && TryGetFileExtension(signature.Substring(12, 14), out extension))
            {
                return extension;
            }

            Debug.WriteLine($"Unknown signature: '{BitConverter.ToString(Encoding.ASCII.GetBytes(signature)).Replace("-", " ")}'");
            return ".bin";
        }

        private static bool TryGetAsciiSignature(MemoryStream stream, int signatureLength, out string signature)
        {
            const int asciiBytesPerChar = 1;
            return TryGetSignature(stream, Encoding.ASCII, asciiBytesPerChar, signatureLength, out signature);
        }

        private static bool TryGetUnicodeSignature(MemoryStream stream, int signatureLength, out string signature)
        {
            const int unicodeBytesPerChar = 2;
            return TryGetSignature(stream, Encoding.Unicode, unicodeBytesPerChar, signatureLength, out signature);
        }

        private static bool TryGetSignature(MemoryStream stream, Encoding encoding, int bytesPerChar, int signatureLength, out string signature)
        {
            signature = null;

            long startPosition = stream.Position;
            if (stream.Length - startPosition < bytesPerChar * signatureLength)
            {
                return false;
            }

            BinaryReader reader = new BinaryReader(stream, encoding, true);
            signature = new string(reader.ReadChars(signatureLength));
            stream.Position = startPosition;

            return true;
        }

        private static bool TryGetFileExtension(string signature, out string extension)
        {
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
                case "fsSL":
                    extension = ".esd";
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
                    extension = ".lua"; // or .hks
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
                case "SMD\0":
                    extension = ".metaparam";
                    return true;
                case "SMDD":
                    extension = ".metadebug";
                    return true;
                case "CLM2":
                    extension = ".clm2";
                    return true;
                case "FLVE":
                    extension = ".flver";
                    return true;
                case "F2TR":
                    extension = ".flver2tri";
                    return true;
                case "FRTR":
                    extension = ".tri";
                    return true;
                case "FXR\0":
                    extension = ".fxr";
                    return true;
                case "ENFL":
                    extension = ".edf"; // ?
                    return true;
                case "EVD\0":
                    extension = ".evd"; // ?
                    return true;
                case "NVMA":
                    extension = ".nvma"; // ?
                    return true;
                case "MSB ":
                    extension = ".msb"; // ?
                    return true;
                case "BJBO":
                    extension = ".bjbo"; // ?
                    return true;
                case "ONAV":
                    extension = ".onav"; // ?
                    return true;
                case "ITLIMITER_INFO":
                    extension = ".itlimiterinfo"; // ?
                    return true;
                default:
                    extension = ".bin";
                    return false;
            }
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
                Sl2File sl2File = Sl2File.ReadSl2File(inputStream, DecryptionKeys.UserDataKey);
                foreach (var userData in sl2File.UserData)
                {
                    string outputFilePath = Path.Combine(options.OutputPath, userData.Name);
                    File.WriteAllBytes(outputFilePath, userData.DecryptedUserData);
                }
            }
        }

        private static void UnpackRegulationFile(Options options)
        {
            using (FileStream inputStream = new FileStream(options.InputPath, FileMode.Open))
            {
                RegulationFile encryptedRegulationFile = RegulationFile.ReadRegulationFile(inputStream, DecryptionKeys.RegulationFileKey);
                DcxFile compressedRegulationFile = DcxFile.Read(new MemoryStream(encryptedRegulationFile.DecryptData()));
                UnpackBndFile(new MemoryStream(compressedRegulationFile.Decompress()), options.OutputPath);
            }
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
            string bhf4FilePath = Path.ChangeExtension(options.InputPath, "bhd");
            if (!File.Exists(bhf4FilePath))
            {
                // HACK: Adding 132 to a hash of a text that ends with XXX.bdt will give you the hash of XXX.bhd.
                string[] split = Path.GetFileNameWithoutExtension(options.InputPath).Split('_');
                uint hash;
                if (uint.TryParse(split[0], out hash))
                {
                    hash += 132;
                    split[0] = hash.ToString("D10");
                    string bdfDirectoryPath = Path.GetDirectoryName(options.InputPath);
                    bhf4FilePath = Path.Combine(bdfDirectoryPath, String.Join("_", split) + ".bhd");
                }
            }

            using (Bdf4FileStream bdf4InputStream = Bdf4FileStream.OpenFile(options.InputPath, FileMode.Open, FileAccess.Read))
            {
                Bhf4File bhf4File = Bhf4File.OpenBhf4File(bhf4FilePath);
                foreach (var entry in bhf4File.Entries)
                {
                    MemoryStream data = bdf4InputStream.Read(entry.FileOffset, entry.FileSize);

                    string fileName = entry.FileName;
                    string fileExtension = Path.GetExtension(fileName);
                    if (fileExtension == ".dcx")
                    {
                        DcxFile dcxFile = DcxFile.Read(data);
                        data = new MemoryStream(dcxFile.Decompress());

                        fileName = Path.GetFileNameWithoutExtension(fileName);
                    }

                    string outputFilePath = Path.Combine(options.OutputPath, fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
                    using (FileStream outputStream = new FileStream(outputFilePath, FileMode.Create))
                    {
                        data.CopyTo(outputStream);
                    }
                }
            }
        }

        private static void UnpackBhf4File(Options options)
        {
            Console.WriteLine($"The file : \'{options.InputPath}\' is already decrypted.");
        }

        private static MemoryStream DecryptBhdFile(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            string key = DecryptionKeys.GetFileKey(fileName);
            return CryptographyUtility.DecryptRsa(filePath, key);
        }
    }
}
