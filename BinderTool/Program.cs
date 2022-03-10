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
using CommandLine;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using BinderTool.Core.Enfl;

namespace BinderTool
{
    public static class Program
    {
        private static void Main(string[] args)
        {

            var options = Parser.Default.ParseArguments<Options>(args).MapResult(
                o => o,
                e => { e.Select(v => { Console.WriteLine(v); return 0; }); return null; }
            );
            if (options == null) return;

            if (!File.Exists(options.InputPath) && !Directory.Exists(options.InputPath)) {
                throw new FormatException("Input file not found");
            }

            if (options.InputType == FileType.Detect || options.InputGameVersion == GameVersion.Detect) {
                var (ty, g) = Options.GetFileType(options.InputPath);
                if (options.InputType == FileType.Detect) options.InputType = ty;
                if (options.InputGameVersion == GameVersion.Detect) options.InputGameVersion = g;
                if (options.InputGameVersion == GameVersion.Detect && options.InputType != FileType.Folder) {
                    Console.WriteLine("Unable to detect game verison. Please specify a game version.");
                    return;
                }
            }

            if (options.InputType == FileType.Unknown) {
                throw new FormatException("Unsupported input file format");
            }

            if (options.OutputPath == null) {
                options.OutputPath = Path.Combine(
                    Path.GetDirectoryName(options.InputPath),
                    Path.GetFileNameWithoutExtension(options.InputPath));
                switch (options.InputType) {
                    case FileType.EncryptedBhd:
                        options.OutputPath += "_decrypted.bhd";
                        break;
                    case FileType.Dcx:
                    case FileType.Fmg:
                        break;
                }
            }

            if (options.InputType == FileType.Folder) {
                Directory.CreateDirectory(options.OutputPath);
                var stack = new List<string>();
                stack.AddRange(Directory.EnumerateFiles(options.InputPath));
                if (options.Recurse) stack.AddRange(Directory.EnumerateDirectories(options.InputPath));
                while (stack.Count > 0) {
                    var curr = stack[stack.Count - 1]; 
                    stack.RemoveAt(stack.Count - 1);
                    if (Directory.Exists(curr)) {
                        stack.AddRange(Directory.EnumerateFiles(curr));
                        continue;
                    }
                    var (ty, g) = Options.GetFileType(curr);
                    if (ty == FileType.Unknown) {
                        Console.WriteLine($"Skipping {curr} because the file type could not be detected");
                        continue;
                    }
                    if (ty == FileType.Detect) {
                        Console.WriteLine($"Skipping {curr} because the file type could not be detected");
                        continue;
                    }
                    if (g == GameVersion.Detect) {
                        if (options.InputGameVersion == GameVersion.Detect) {
                            Console.WriteLine($"Skipping {curr} because the game could not be detected");
                            continue;
                        }
                        g = options.InputGameVersion;
                    }
                    var opt = options.Clone();
                    opt.InputGameVersion = g;
                    opt.InputType = ty;
                    opt.InputPath = curr;
                    opt.OutputPath = options.OutputPath + '\\' + curr.Substring(options.InputPath.Length);
                    try {
                        Process(opt);
                    } catch (Exception e) {
                        Console.WriteLine($"Error processing {curr}:");
                        Console.WriteLine(e);
                    }
                }
            } else {
                Process(options);
            }

        }

        static void Process(Options options) {

            switch (options.InputType)
            {
                // These files have a single output file. 
                case FileType.EncryptedBhd:
                case FileType.Bhd:
                case FileType.Dcx:
                case FileType.Fmg:
                case FileType.Enfl:
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
                case FileType.Enfl:
                    UnpackEnflFile(options);
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Unable to handle type '{options.InputType}'");
            }
        }

        private static void ShowUsageInfo()
        {
            Console.WriteLine(
                "BinderTool by Atvaark\n" +
                "  A tool for unpacking Dark Souls I/II/III/Bloodborne/Sekiro Bdt, Bhd, Dcx, Sl2, Tpf, Param and Fmg files\n" +
                "Usage:\n" +
                "  BinderTool file_path [output_path]\n" +
                "Examples:\n" +
                "  BinderTool data1.bdt\n" +
                "  BinderTool data1.bdt data1");
        }

        private static void UnpackBdtFile(Options options)
        {
            FileNameDictionary dictionary = FileNameDictionary.OpenFromFile(options.InputGameVersion);
            string fileNameWithoutExtension = Path.GetFileName(options.InputPath).Replace("Ebl.bdt", "").Replace(".bdt", "");
            string archiveName = fileNameWithoutExtension.ToLower();

            using (Bdt5FileStream bdtStream = Bdt5FileStream.OpenFile(options.InputPath, FileMode.Open, FileAccess.Read))
            {
                Bhd5File bhdFile = Bhd5File.Read(
                    inputStream: DecryptBhdFile(
                        filePath: Path.ChangeExtension(options.InputPath, "bhd"),
                        version: options.InputGameVersion),
                    version: options.InputGameVersion
                    );
                foreach (var bucket in bhdFile.GetBuckets())
                {
                    foreach (var entry in bucket.GetEntries())
                    {
                        Stream data;
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
                            if (DecryptionKeys.TryGetAesFileKey(options.InputGameVersion, Path.GetFileName(fileName), out decryptionKey))
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

                        if (options.AutoExtractBnd && extension == ".bnd")
                        {
                            UnpackBndFile(data, options.OutputPath, options);
                            continue;
                        }
                        if (options.AutoExtractParam && extension == ".param") {
                            UnpackParamFile(data, fileNameWithoutExtension, Path.Combine(options.OutputPath, fileNameWithoutExtension));
                            continue;
                        }
                        if (options.AutoExtractFmg && extension == ".fmg") {
                            UnpackFmgFile(data, Path.Combine(options.OutputPath, fileName));
                            continue;
                        }
                        if (options.AutoExtractEnfl && extension == ".enfl") {
                            var ans = UnpackEnflFile(data);
                            File.WriteAllText(Path.Combine(options.OutputPath, fileName+".csv"), ans);
                            continue;
                        }
                        string newFileNamePath = Path.Combine(options.OutputPath, fileName);
                        Directory.CreateDirectory(Path.GetDirectoryName(newFileNamePath));
                        FileStream outS = new FileStream(newFileNamePath, FileMode.OpenOrCreate);
                        byte[] buf = new byte[1000];
                        data.Seek(0, SeekOrigin.Begin);
                        for (long pos = 0; pos < data.Length; pos += buf.Length)
                        {
                            int read = data.Read(buf, 0, buf.Length);
                            outS.Write(buf, 0, read);
                        }
                        outS.Close();
                    }
                }
            }
        }

        private static bool TryReadFileSize(Bhd5BucketEntry entry, Bdt5FileStream bdtStream, out long fileSize)
        {
            fileSize = 0;

            const int sampleLength = 48;
            Stream data = bdtStream.Read(entry.FileOffset, sampleLength);

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

        private static string GetDataExtension(Stream data)
        {
            string signature;
            string extension;

            if (TryGetAsciiSignature(data, 4, out signature)
                && TryGetFileExtension(signature, out extension))
            {
                return extension;
            }

            // TODO: Sekiro
            //if (TryGetUnicodeSignature(data, 4, out signature)
            //    && TryGetFileExtension(signature, out extension))
            //{
            //    return extension;
            //}

            if (TryGetAsciiSignature(data, 26, out signature)
                && TryGetFileExtension(signature.Substring(12, 14), out extension))
            {
                return extension;
            }

            //Debug.WriteLine($"Unknown signature: '{BitConverter.ToString(Encoding.ASCII.GetBytes(signature)).Replace("-", " ")}'");
            return ".bin";
        }

        private static bool TryGetAsciiSignature(Stream stream, int signatureLength, out string signature)
        {
            const int asciiBytesPerChar = 1;
            return TryGetSignature(stream, Encoding.ASCII, asciiBytesPerChar, signatureLength, out signature);
        }

        private static bool TryGetUnicodeSignature(Stream stream, int signatureLength, out string signature)
        {
            const int unicodeBytesPerChar = 2;
            return TryGetSignature(stream, Encoding.Unicode, unicodeBytesPerChar, signatureLength, out signature);
        }

        private static bool TryGetSignature(Stream stream, Encoding encoding, int bytesPerChar, int signatureLength, out string signature)
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
                    extension = ".bdf";
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
                    extension = ".enfl";
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
            using (var inputStream = DecryptBhdFile(options.InputPath, options.InputGameVersion))
            using (var outputStream = File.OpenWrite(options.OutputPath))
            {
                inputStream.WriteTo(outputStream);
            }
        }

        private static void UnpackBndFile(Options options)
        {
            using (FileStream inputStream = new FileStream(options.InputPath, FileMode.Open, FileAccess.Read))
            {
                UnpackBndFile(inputStream, options.OutputPath, options);
            }
        }

        private static void UnpackBndFile(Stream inputStream, string outputPath, Options options)
        {
            Bnd4File file = Bnd4File.ReadBnd4File(inputStream);

            foreach (var entry in file.Entries)
            {
                try
                {
                    string fileName = FileNameDictionary.NormalizeFileName(entry.FileName);
                    string outputFilePath = Path.Combine(outputPath, fileName);
                    if (options.AutoExtractParam && fileName.EndsWith(".param")) {
                        UnpackParamFile(new MemoryStream(entry.EntryData), fileName, outputFilePath);
                        continue;
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
                    File.WriteAllBytes(outputFilePath, entry.EntryData);
                }
                catch (Exception e) { }
            }
        }

        private static void UnpackSl2File(Options options)
        {
            using (FileStream inputStream = new FileStream(options.InputPath, FileMode.Open, FileAccess.Read))
            {
                byte[] key = GetSavegameKey(options.InputGameVersion);
                Sl2File sl2File = Sl2File.ReadSl2File(inputStream, key);
                foreach (var userData in sl2File.UserData)
                {
                    string outputFilePath = Path.Combine(options.OutputPath, userData.Name);
                    File.WriteAllBytes(outputFilePath, userData.DecryptedUserData);
                }
            }
        }

        private static byte[] GetSavegameKey(GameVersion version)
        {
            byte[] key;
            switch (version)
            {
                case GameVersion.DarkSouls2:
                    key = DecryptionKeys.UserDataKeyDs2;
                    break;
                case GameVersion.DarkSouls3:
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
                byte[] key = GetRegulationKey(options.InputGameVersion);
                EncFile encryptedFile = EncFile.ReadEncFile(inputStream, key, options.InputGameVersion);
                DcxFile compressedRegulationFile = DcxFile.Read(encryptedFile.Data);
                UnpackBndFile(new MemoryStream(compressedRegulationFile.Decompress()), options.OutputPath, options);
            }
        }

        private static byte[] GetRegulationKey(GameVersion version)
        {
            byte[] key;
            switch (version)
            {
                case GameVersion.DarkSouls2:
                    key = DecryptionKeys.RegulationFileKeyDs2;
                    break;
                case GameVersion.DarkSouls3:
                    key = DecryptionKeys.RegulationFileKeyDs3;
                    break;
                case GameVersion.EldenRing:
                    key = DecryptionKeys.RegulationFileKeyEr;
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
                    using (var outputStream = File.Create(outputFilePath))
                    {
                        entry.Write(outputStream);
                    }
                }
            }
        }

        private static void UnpackBhf4File(Options options)
        {
            Console.WriteLine($"The file : \'{options.InputPath}\' is already decrypted.");
        }

        private static MemoryStream DecryptBhdFile(string filePath, GameVersion version)
        {
            string fileDirectory = Path.GetDirectoryName(filePath) ?? string.Empty;
            string fileName = Path.GetFileName(filePath) ?? string.Empty;
            string key = null;
            switch (version)
            {
                case GameVersion.DarkSouls2:
                    string keyFileName = Regex.Replace(fileName, @"Ebl\.bhd$", "KeyCode.pem", RegexOptions.IgnoreCase);
                    string keyFilePath = Path.Combine(fileDirectory, keyFileName);
                    if (File.Exists(keyFilePath))
                    {
                        key = File.ReadAllText(keyFilePath);
                    }
                    break;
                case GameVersion.DarkSouls3:
                case GameVersion.Sekiro:
                case GameVersion.EldenRing:
                    DecryptionKeys.TryGetRsaFileKey(version, fileName, out key);
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
                UnpackParamFile(inputStream, options.InputPath, options.OutputPath);
            }
        }

        private static void UnpackParamFile(Stream inputStream, string fileName, string outputPath)
        {
            ParamFile paramFile = ParamFile.ReadParamFile(inputStream);
            ParamFormatDesc d = ParamFormatDesc.Read(fileName);
            if (d == null) {
                var dir = Path.Combine(Directory.GetParent(outputPath).FullName, Path.GetFileNameWithoutExtension(outputPath));
                Directory.CreateDirectory(dir);
                foreach (var entry in paramFile.Entries) {
                    string entryName = $"{entry.Id:D10}.{paramFile.StructName}";
                    string outputFilePath = Path.Combine(dir, entryName);
                    Directory.CreateDirectory(Directory.GetParent(outputFilePath).FullName);
                    File.WriteAllBytes(outputFilePath, entry.Data);
                }
            } else {
                var ans = new StringBuilder();
                ans.Append("Name, ");
                for (int i = 0; i < d.ParamInfo.Count; i++) {
                    ans.Append(d.ParamInfo[i].Item1);
                    if (i != d.ParamInfo.Count - 1) ans.Append(", ");
                }
                ans.AppendLine();
                foreach (var entry in paramFile.Entries) {
                    var reader = new BinaryReader(new MemoryStream(entry.Data));
                    ans.Append($"{entry.Id:D10}, ");
                    for (int i = 0; i < d.ParamInfo.Count; i++) {
                        switch (d.ParamInfo[i].Item2) {
                            case ParamType.Byte:
                                ans.Append(reader.ReadSByte());
                                break;
                            case ParamType.UByte:
                                ans.Append(reader.ReadByte());
                                break;
                            case ParamType.Short:
                                ans.Append(reader.ReadInt16());
                                break;
                            case ParamType.UShort:
                                ans.Append(reader.ReadUInt16());
                                break;
                            case ParamType.Int:
                                ans.Append(reader.ReadInt32());
                                break;
                            case ParamType.UInt:
                                ans.Append(reader.ReadUInt32());
                                break;
                            case ParamType.Long:
                                ans.Append(reader.ReadInt64());
                                break;
                            case ParamType.ULong:
                                ans.Append(reader.ReadUInt64());
                                break;
                            case ParamType.Float:
                                ans.Append(reader.ReadSingle());
                                break;
                            case ParamType.Double:
                                ans.Append(reader.ReadDouble());
                                break;
                        }
                        if (i != d.ParamInfo.Count - 1) ans.Append(", ");
                    }
                    ans.AppendLine();
                }
                Directory.CreateDirectory(Directory.GetParent(outputPath).FullName);
                File.WriteAllText(outputPath + ".csv", ans.ToString());
            }
        }

        private static void UnpackFmgFile(Options options)
        {
            using (FileStream inputStream = new FileStream(options.InputPath, FileMode.Open, FileAccess.Read))
            {
                UnpackFmgFile(inputStream, options.OutputPath);
            }
        }

        public static void UnpackFmgFile(Stream inputStream, string outputPath)
        {
            FmgFile fmgFile = FmgFile.ReadFmgFile(inputStream);

            StringBuilder builder = new StringBuilder();
            foreach (var entry in fmgFile.Entries) {
                string value = entry.Value
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n")
                    .Replace("\t", "\\t");
                builder.AppendLine($"{entry.Id}\t{value}");
            }

            outputPath += ".txt";
            File.WriteAllText(outputPath, builder.ToString());
        }

        private static void UnpackEnflFile(Options options)
        {
            var ans = UnpackEnflFile(new FileStream(options.InputPath, FileMode.Open));
            File.WriteAllText(options.OutputPath + ".csv", ans);
        }

        private static string UnpackEnflFile(Stream input)
        {
            var f = EntryFileListFile.ReadEntryFileListFile(input);
            var ans = new StringBuilder();
            foreach (var i in f.array1) {
                ans.AppendLine($"{i.Unknown1}, {i.Unknown2}");
            }
            foreach (var i in f.array2) {
                ans.AppendLine($"{i.Unknown1}, {i.Unknown2}, {i.EntryFileName}");
            }
            return ans.ToString();
        }
    }
}
