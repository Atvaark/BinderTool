using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using BinderTool.Core;
using BinderTool.Core.Bdf4;
using BinderTool.Core.Bdt5;
using BinderTool.Core.Bhd5;
using BinderTool.Core.Bhf4;
using BinderTool.Core.Bnd4;
using BinderTool.Core.Dcx;
using BinderTool.Core.Enc;
using BinderTool.Core.Fmg;
using BinderTool.Core.Param;
using BinderTool.Core.Sl2;
using BinderTool.Core.Tpf;

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
                case FileType.Bhd:
                case FileType.Dcx:
                case FileType.Fmg:
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
                case FileType.Tpf:
                    UnpackTpfFile(options);
                    break;
                case FileType.Param:
                    UnpackParamFile(options);
                    break;
                case FileType.Fmg:
                    UnpackFmgFile(options);
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Unable to handle type '{options.InputType}'");
            }
        }

        private static void ShowUsageInfo()
        {
            Console.WriteLine(
                "BinderTool by Atvaark\n" +
                "  A tool for unpacking Dark Souls II/III Bdt, Bhd, Dcx, Sl2, Tpf, Param and Fmg files\n" +
                "Usage:\n" +
                "  BinderTool file_path [output_path]\n" +
                "Examples:\n" +
                "  BinderTool data1.bdt\n" +
                "  BinderTool data1.bdt data1");
        }

        private static void UnpackBdtFile(Options options)
        {
            FileNameDictionary dictionary = FileNameDictionary.OpenFromFile(options.InputVersion);
            string fileNameWithoutExtension = Path.GetFileName(options.InputPath).Replace("Ebl.bdt", "").Replace(".bdt", "");
            string archiveName = fileNameWithoutExtension.ToLower();

            using (Bdt5FileStream bdtStream = Bdt5FileStream.OpenFile(options.InputPath, FileMode.Open, FileAccess.Read))
            {
                Bhd5File bhdFile = Bhd5File.Read(
                    inputStream: DecryptBhdFile(
                        filePath: Path.ChangeExtension(options.InputPath, "bhd"),
                        version: options.InputVersion),
                    version: options.InputVersion
                    );
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
                                Console.WriteLine($"Unable to determine the length of file '{entry.FileNameHash:D10}'");
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
                        string dataExtension = GetDataExtension(data);
                        bool fileNameFound = dictionary.TryGetFileName(entry.FileNameHash, archiveName, out fileName);
                        if (!fileNameFound)
                        {
                            fileNameFound = dictionary.TryGetFileName(entry.FileNameHash, archiveName, dataExtension, out fileName);
                        }

                        string extension;
                        if (fileNameFound)
                        {
                            extension = Path.GetExtension(fileName);

                            if (dataExtension == ".dcx" && extension != ".dcx")
                            {
                                extension = ".dcx";
                                fileName += ".dcx";
                            }
                        }
                        else
                        {
                            extension = dataExtension;
                            fileName = $"{entry.FileNameHash:D10}_{fileNameWithoutExtension}{extension}";
                        }

                        if (extension == ".enc")
                        {
                            byte[] decryptionKey;
                            if (DecryptionKeys.TryGetAesFileKey(Path.GetFileName(fileName), out decryptionKey))
                            {
                                EncFile encFile = EncFile.ReadEncFile(data, decryptionKey);
                                data = encFile.Data;

                                fileName = Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName));
                                extension = Path.GetExtension(fileName);
                            }
                            else
                            {
                                Debug.WriteLine($"No decryption key for file \'{fileName}\' found.");
                            }
                        }

                        if (extension == ".dcx")
                        {
                            DcxFile dcxFile = DcxFile.Read(data);
                            data = new MemoryStream(dcxFile.Decompress());

                            fileName = Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName));

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

            //Debug.WriteLine($"Unknown signature: '{BitConverter.ToString(Encoding.ASCII.GetBytes(signature)).Replace("-", " ")}'");
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
                case "DDS ":
                    extension = ".dds";
                    return true;
                case "TAE ":
                    extension = ".tae";
                    return true;
                case "FSB5":
                    extension = ".fsb";
                    return true;
                case "fsSL":
                case "fSSL":
                    extension = ".esd";
                    return true;
                case "TPF\0":
                    extension = ".tpf";
                    return true;
                case "PFBB":
                    extension = ".pfbbin";
                    return true;
                case "OBJB":
                    extension = ".breakobj";
                    return true;
                case "filt":
                    extension = ".fltparam"; // DS II
                    //extension = ".gparam"; // DS III
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
                case "RIFF":
                    extension = ".fev";
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
                case "ITLIMITER_INFO":
                    extension = ".itl";
                    return true;
                case "EVD\0":
                    extension = ".emevd";
                    return true;
                case "ENFL":
                    extension = ".entryfilelist";
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
                default:
                    extension = ".bin";
                    return false;
            }
        }

        private static void UnpackBhdFile(Options options)
        {
            using (var inputStream = DecryptBhdFile(options.InputPath, options.InputVersion))
            using (var outputStream = File.OpenWrite(options.OutputPath))
            {
                inputStream.WriteTo(outputStream);
            }
        }

        private static void UnpackBndFile(Options options)
        {
            using (FileStream inputStream = new FileStream(options.InputPath, FileMode.Open, FileAccess.Read))
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
            using (FileStream inputStream = new FileStream(options.InputPath, FileMode.Open, FileAccess.Read))
            {
                byte[] key = GetSavegameKey(options.InputVersion);
                Sl2File sl2File = Sl2File.ReadSl2File(inputStream, key);
                foreach (var userData in sl2File.UserData)
                {
                    string outputFilePath = Path.Combine(options.OutputPath, userData.Name);
                    File.WriteAllBytes(outputFilePath, userData.DecryptedUserData);
                }
            }
        }

        private static byte[] GetSavegameKey(DSVersion version)
        {
            byte[] key;
            switch (version)
            {
                case DSVersion.DarkSouls2:
                    key = DecryptionKeys.UserDataKeyDs2;
                    break;
                case DSVersion.DarkSouls3:
                    key = DecryptionKeys.UserDataKeyDs3;
                    break;
                default:
                    key = new byte[16];
                    break;
            }

            return key;
        }

        private static void UnpackRegulationFile(Options options)
        {
            using (FileStream inputStream = new FileStream(options.InputPath, FileMode.Open, FileAccess.Read))
            {
                byte[] key = GetRegulationKey(options.InputVersion);
                EncFile encryptedFile = EncFile.ReadEncFile(inputStream, key, options.InputVersion);
                DcxFile compressedRegulationFile = DcxFile.Read(encryptedFile.Data);
                UnpackBndFile(new MemoryStream(compressedRegulationFile.Decompress()), options.OutputPath);
            }
        }

        private static byte[] GetRegulationKey(DSVersion version)
        {
            byte[] key;
            switch (version)
            {
                case DSVersion.DarkSouls2:
                    key = DecryptionKeys.RegulationFileKeyDs2;
                    break;
                case DSVersion.DarkSouls3:
                    key = DecryptionKeys.RegulationFileKeyDs3;
                    break;
                default:
                    key = new byte[16];
                    break;
            }

            return key;
        }

        private static void UnpackDcxFile(Options options)
        {
            string unpackedFileName = Path.GetFileNameWithoutExtension(options.InputPath);
            string outputFilePath = options.OutputPath;
            bool hasExtension = Path.GetExtension(unpackedFileName) != "";

            using (FileStream inputStream = new FileStream(options.InputPath, FileMode.Open, FileAccess.Read))
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
            string bdfDirectoryPath = Path.GetDirectoryName(options.InputPath);
            string bhf4Extension = Path.GetExtension(options.InputPath).Replace("bdt", "bhd");
            string bhf4FilePath = Path.Combine(bdfDirectoryPath, Path.GetFileNameWithoutExtension(options.InputPath) + bhf4Extension);
            if (!File.Exists(bhf4FilePath))
            {
                // HACK: Adding 132 to a hash of a text that ends with XXX.bdt will give you the hash of XXX.bhd.
                string[] split = Path.GetFileNameWithoutExtension(options.InputPath).Split('_');
                uint hash;
                if (uint.TryParse(split[0], out hash))
                {
                    hash += 132;
                    split[0] = hash.ToString("D10");
                    bhf4FilePath = Path.Combine(bdfDirectoryPath, string.Join("_", split) + ".bhd");
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
                        fileName = Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName));
                    }

                    string outputFilePath = Path.Combine(options.OutputPath, fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
                    File.WriteAllBytes(outputFilePath, data.ToArray());
                }
            }
        }

        private static void UnpackTpfFile(Options options)
        {
            using (FileStream inputStream = new FileStream(options.InputPath, FileMode.Open, FileAccess.Read))
            {
                TpfFile tpfFile = TpfFile.OpenTpfFile(inputStream);
                foreach (var entry in tpfFile.Entries)
                {
                    string outputFilePath = Path.Combine(options.OutputPath, entry.FileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
                    File.WriteAllBytes(outputFilePath, entry.Data);
                }
            }
        }

        private static void UnpackBhf4File(Options options)
        {
            Console.WriteLine($"The file : \'{options.InputPath}\' is already decrypted.");
        }

        private static MemoryStream DecryptBhdFile(string filePath, DSVersion version)
        {
            string fileDirectory = Path.GetDirectoryName(filePath) ?? string.Empty;
            string fileName = Path.GetFileName(filePath) ?? string.Empty;
            string key = null;
            switch (version)
            {
                case DSVersion.DarkSouls2:
                    string keyFileName = Regex.Replace(fileName, @"Ebl\.bhd$", "KeyCode.pem", RegexOptions.IgnoreCase);
                    string keyFilePath = Path.Combine(fileDirectory, keyFileName);
                    if (File.Exists(keyFilePath))
                    {
                        key = File.ReadAllText(keyFilePath);
                    }
                    break;
                case DSVersion.DarkSouls3:
                    DecryptionKeys.TryGetRsaFileKey(fileName, out key);
                    break;
            }

            if (key == null)
            {
                throw new ApplicationException($"Missing decryption key for file \'{fileName}\'");
            }
            
            return CryptographyUtility.DecryptRsa(filePath, key);
        }

        private static void UnpackParamFile(Options options)
        {
            using (FileStream inputStream = new FileStream(options.InputPath, FileMode.Open, FileAccess.Read))
            {
                ParamFile paramFile = ParamFile.ReadParamFile(inputStream);
                foreach (var entry in paramFile.Entries)
                {
                    string entryName = $"{entry.Id:D10}.{paramFile.StructName}";
                    string outputFilePath = Path.Combine(options.OutputPath, entryName);
                    Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
                    File.WriteAllBytes(outputFilePath, entry.Data);
                }
            }
        }

        private static void UnpackFmgFile(Options options)
        {
            using (FileStream inputStream = new FileStream(options.InputPath, FileMode.Open, FileAccess.Read))
            {
                FmgFile fmgFile = FmgFile.ReadFmgFile(inputStream);

                StringBuilder builder = new StringBuilder();
                foreach (var entry in fmgFile.Entries)
                {
                    string value = entry.Value
                        .Replace("\r", "\\r")
                        .Replace("\n", "\\n")
                        .Replace("\t", "\\t");
                    builder.AppendLine($"{entry.Id}\t{value}");
                }

                string outputPath = options.OutputPath + ".txt";
                File.WriteAllText(outputPath, builder.ToString());
            }
        }
    }
}
