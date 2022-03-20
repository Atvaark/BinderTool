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
using static BinderTool.FileSearch;

namespace BinderTool
{
    public static class Program
    {

        private static HashSet<string> enflCollateSet = null;

        private static void Main(string[] args)
        {
            //parse command line args
            var options = Parser.Default.ParseArguments<Options>(args).MapResult(
                o => o,
                e => { e.Select(v => { Console.WriteLine(v); return 0; }); return null; }
            );
            if (options == null) return;

            //ensure the input file/directory exists
            if (!File.Exists(options.InputPath) && !Directory.Exists(options.InputPath)) {
                throw new FormatException("Input file not found");
            }

            //do a file name search over the hash list
            //the input file is a .hashlist made by CreateHashList
            //TODO: support games other than elden ring
            if (options.FileNameSearch != "") {
                var field = typeof(FileSearch).GetField(options.FileNameSearch);
                if (field == null) {
                    Console.WriteLine("Specified search not found.");
                    Console.ReadKey();
                    return;
                }
                var search = (FileSearch)field.GetValue(null);
                var hashes = FileSearch.ReadHashList(options.InputPath);
                if (options.OutputPath == "") search.Search(hashes);
                else search.SearchAndUpdate(hashes, options.OutputPath);
                return;
            }

            //creates a .hashlist file (binary file, just a u64 number of entries and then those entries as u64s)
            //TODO: support games other than elden ring
            if (options.CreateHashList) {
                var bhds = new List<string>();
                var stack = new List<string>();
                stack.AddRange(Directory.GetDirectories(options.InputPath));
                stack.AddRange(Directory.GetFiles(options.InputPath));
                while (stack.Count > 0) {
                    var curr = stack.Last();
                    stack.RemoveAt(stack.Count - 1);
                    if (File.Exists(curr) && curr.EndsWith(".bhd")) bhds.Add(curr);
                    if (Directory.Exists(curr)) stack.AddRange(Directory.GetFiles(curr));
                }
                if (options.OutputPath == null) options.OutputPath = Path.Combine(options.InputPath, "filename_hashes.hashlist");
                var ans = FileSearch.CreateHashList(options.OutputPath, bhds.ToArray());
                Console.WriteLine($"Created hash list with {ans} hashes.");
                return;
            }

            //if the options didn't include a filetype or gameversion, they default to "detect"
            //here we try to detect the type and/or game version automatically
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

            //if the options didn't include an output path, make the output path the default for the given file type
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

            //if we're collating entryfilelists, make sure the output file exists
            if (options.CollateEnflPath.Length > 0 && !File.Exists(options.CollateEnflPath)) {
                File.Create(options.CollateEnflPath).Close();
            }

            //handle input type = folder
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
                //handle input type != folder
                Process(options);
            }
            //stop the console from closing automatically in debug builds
#if (DEBUG)
            Console.ReadKey();
#endif
        }

        /// <summary>
        /// Top-level method to process an input file. Should only be called from Main.
        /// </summary>
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

            Console.WriteLine($"Unpacking encrypted bdt file \"{options.InputPath}\" to \"{options.OutputPath}\"...");
            FileNameDictionary dictionary = FileNameDictionary.OpenFromFile(options.InputGameVersion);
            string bdtName = Path.GetFileName(options.InputPath);
            string fileNameWithoutExtension = bdtName.Replace("Ebl.bdt", "").Replace(".bdt", "");
            string archiveName = fileNameWithoutExtension.ToLower();
            
            if (options.CollateEnflPath.Length != 0) {
                enflCollateSet = new HashSet<string>();
                foreach (var line in File.ReadAllLines(options.CollateEnflPath)) {
                    enflCollateSet.Add(line);
                }
            }

            using (Bdt5FileStream bdtStream = Bdt5FileStream.OpenFile(options.InputPath, FileMode.Open, FileAccess.Read))
            {
                Console.WriteLine("Decrypting the corresponding .bhd...");
                Bhd5File bhdFile = Bhd5File.Read(
                    inputStream: DecryptBhdFile(
                        filePath: Path.ChangeExtension(options.InputPath, "bhd"),
                        version: options.InputGameVersion),
                    version: options.InputGameVersion
                    );
                Console.WriteLine("Finished decrypting the .bhd.");
                var entries = bhdFile.GetBuckets().SelectMany(b => b.GetEntries());
                var numEntries = entries.Count();
                Console.WriteLine($"\"{bdtName}\" has {numEntries} files.");
                (int, int) currEntryPos = (Console.CursorLeft, Console.CursorTop);
                Console.WriteLine("");
                var entryNum = 0;
                ulong bytesWritten = 0;
                var extracted = 0;
                var missingNames = 0;
                List<(string, ulong, Stream)> bdts = new List<(string, ulong, Stream)>();
                Dictionary<string, Stream> bhds = new Dictionary<string, Stream>();
                foreach (var entry in entries) 
                {
                    (int, int) tmpPos = (Console.CursorLeft, Console.CursorTop);
                    Console.SetCursorPosition(currEntryPos.Item1, currEntryPos.Item2);
                    Console.WriteLine($"Processing entry {entryNum}/{numEntries} ({(int)Math.Round(((float)entryNum / (float)numEntries) * 100)}%)");
                    Console.SetCursorPosition(tmpPos.Item1, tmpPos.Item2);
                    entryNum++;
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
                    string dataExtension = GetDataExtension(data, options.InputGameVersion);
                    bool fileNameFound = dictionary.TryGetFileName(entry.FileNameHash, archiveName, out fileName);
                    if (!fileNameFound)
                    {
                        fileNameFound = dictionary.TryGetFileName(entry.FileNameHash, archiveName, dataExtension, out fileName);
                    }
                    if (!fileNameFound) missingNames++;
                    if (options.OnlyOutputUnknown && fileNameFound) continue;

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

                    if (options.AutoExtractDcx && extension == ".dcx")
                    {
                        try {
                            DcxFile dcxFile = DcxFile.Read(data);
                            data = new MemoryStream(dcxFile.Decompress());

                            fileName = Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName));

                            if (fileNameFound) {
                                extension = Path.GetExtension(fileName);
                            } else {
                                extension = GetDataExtension(data, options.InputGameVersion);
                                fileName += extension;
                            }
                        } catch {
                            Debug.WriteLine("Error upnacking dcx, outputting as .dcx instead");
                        }
                    }

                    if (options.OnlyProcessExtension.Length > 0 && extension.ToLower() != options.OnlyProcessExtension.ToLower()) {
                        continue;
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

                    extracted++;

                    //.bdts and .bhds require special processing to extract, but since you need 2 files to extract them ProcessFile doesn't work well for them
                    //Fortunately, they seem to only appear in encrypted .bdts, so we add a special case for them
                    if (options.AutoExtractBdt && extension.EndsWith("bdt")) {
                        bdts.Add((fileName, entry.FileNameHash, data));
                        continue;
                    }
                    if (options.AutoExtractBdt && extension.EndsWith("bhd")) {
                        if (!fileNameFound) fileName = entry.FileNameHash.ToString();
                        bhds[fileName] = data;
                        continue;
                    }

                    bytesWritten += ProcessFile(fileName, options, data);
                }
                //process .bdt files if we need to
                if (options.AutoExtractBdt) {
                    Console.WriteLine("Unpacking inner .bdt files...");
                    foreach (var (fileName, hash, data) in bdts) {
                        Stream bhd;
                        if (bhds.TryGetValue(fileName.Replace("bdt", "bhd"), out bhd)) {
                            bhds.Remove(fileName.Replace("bdt", "bhd"));
                        } else {
                            // HACK: Adding 132 to a hash of a text that ends with XXX.bdt will give you the hash of XXX.bhd for games before Elden Ring
                            // (h-d) * prime + (d-t) = 4 * prime - 16
                            // prime = 37 for older games, 133 for elden ring
                            var oldGamesBhdHash = hash + 132;
                            if (bhds.TryGetValue(oldGamesBhdHash.ToString(), out bhd)) {
                                bhds.Remove(oldGamesBhdHash.ToString());
                            } else {
                                // HACK: Adding 516 to a hash of a text that ends with XXX.bdt will give you the hash of XXX.bhd for Elden Ring
                                var newGamesBhdHash = hash + 516;
                                if (bhds.TryGetValue(newGamesBhdHash.ToString(), out bhd)) {
                                    bhds.Remove(newGamesBhdHash.ToString());
                                } else {
                                    //couldn't find the .bhd, output the raw .bdt instead
                                    Console.WriteLine($"Couldn't find .bhd for \"{fileName}\", outputting raw file instead");
                                    bytesWritten += WriteFile(Path.Combine(options.OutputPath, fileName), data);
                                    continue;
                                }
                            }
                        }
                        var newOptions = options.Clone();
                        if (Regex.IsMatch(fileName, @"\d+_.+\.bdt$")) {
                            newOptions.OutputPath = Path.Combine(options.OutputPath, $"{bdts}\\{hash}");
                        } else {
                            newOptions.OutputPath = Path.Combine(options.OutputPath, Path.GetDirectoryName(fileName));
                        }
                        bytesWritten += UnpackBdf4File(data, bhd, newOptions);
                    }
                    //leftover .bhd files that didn't have a .bdt paired
                    foreach (var hash in bhds.Keys) {
                        var file = bhds[hash];
                        var name = $"{hash:D10}_{fileNameWithoutExtension}.bhd";
                        bytesWritten += WriteFile(Path.Combine(options.OutputPath, name), file);
                    }
                    Console.WriteLine("Dne unpacking inner .bdt files.");
                }
                Console.WriteLine($"Succesfully extracted {extracted}/{numEntries} files ({numEntries - extracted} failed), totaling {bytesWritten} bytes written to disk");
                Console.WriteLine($"{missingNames} file names were unknown");
            }
        }

        /// <summary>
        /// Writes binary data from a <code>Stream</code> to a file.
        /// </summary>
        /// <returns>The number of bytes written to disk</returns>
        public static ulong WriteFile(string path, Stream data)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            FileStream outS = new FileStream(path, FileMode.Create);
            byte[] buf = new byte[1000];
            data.Seek(0, SeekOrigin.Begin);
            for (long pos = 0; pos < data.Length; pos += buf.Length) {
                int read = data.Read(buf, 0, buf.Length);
                outS.Write(buf, 0, read);
            }
            outS.Close();
            return (ulong)data.Length;
        }

        /// <summary>
        /// Inner processing method to handle files which may need further processing before being written to disk (e.g. a .bnd inside a .bdt).
        /// </summary>
        /// <param name="filename">The path and name of the file to be processed</param>
        /// <param name="options"></param>
        /// <param name="data">The binary contents of the file to be processed</param>
        /// <returns>The number of bytes written to disk</returns>
        private static ulong ProcessFile(string filename, Options options, Stream data)
        {
            string[] sp = filename.Split('.');
            string[] sp2 = filename.Replace('\\', '/').Split('/');
            sp2 = sp2[sp2.Length - 1].Split('.');
            string extension = "."+sp[sp.Length - 1];
            if (extension == ".dcx") {
                DcxFile dcxFile = DcxFile.Read(data);
                data = new MemoryStream(dcxFile.Decompress());
                filename = filename.Replace(".dcx", "");
                sp = filename.Split('.');
                extension = "."+sp[sp.Length - 1];
                sp2 = filename.Replace('\\', '/').Split('/');
                sp2 = sp2[sp2.Length - 1].Split('.');
            }
            //auto extract .bnd
            if (options.AutoExtractBnd && extension.EndsWith("bnd")) {
                return UnpackBndFile(data, options);
            }
            //auto extract .tpf
            if (options.AutoExtractTpf && extension == ".tpf") {
                Options newOptions = options.Clone();
                newOptions.OutputPath = Path.Combine(options.OutputPath, filename);
                return UnpackTpfFile(data, newOptions);
            }
            //auto extract .param
            if (options.AutoExtractParam && extension == ".param") {
                return UnpackParamFile(data, sp2[sp.Length - 2], Path.Combine(options.OutputPath, filename.Replace(".param", "")));
            }
            //auto extract .fmg
            if (options.AutoExtractFmg && extension == ".fmg") {
                return UnpackFmgFile(data, Path.Combine(options.OutputPath, filename));
            }
            //auto extract/collate .entryfilelist
            if (options.CollateEnflPath != null && options.CollateEnflPath.Length > 0 && extension == ".entryfilelist") {
                EntryFileListFile file = EntryFileListFile.ReadEntryFileListFile(data);
                File.AppendAllLines(options.CollateEnflPath, file.array2.Select(e => e.EntryFileName).Where(enflCollateSet.Add));
                return 0;
            } else if (options.AutoExtractEnfl && extension == ".entryfilelist") {
                var ans = UnpackEnflFile(data);
                var path = Path.Combine(options.OutputPath, filename + ".csv");
                File.WriteAllText(path, ans);
                return (ulong)new FileInfo(path).Length;
            }
            //not doing any auto extract, just output the raw file
            string newFileNamePath = Path.Combine(options.OutputPath, filename);
            return WriteFile(newFileNamePath, data);
        }

        /// <summary>
        /// Attempts to read the length of a file contained in a .bdt if that size should be different from the size listed in the .bhd.
        /// Currently only needed for .dcx files (compressed files).
        /// </summary>
        /// <returns>Whether the returned size should be used while reading from the .bdt instead of the size from the .bhd</returns>
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

        /// <summary>
        /// Attempts to figure out which extension the file should have based on its contents and returns that extension, or ".bin" if the extension could not be determined.
        /// </summary>
        private static string GetDataExtension(Stream data, GameVersion gameVersion)
        {
            string signature;
            string extension;

            if (TryGetAsciiSignature(data, 4, out signature)
                && TryGetFileExtension(signature, out extension, gameVersion))
            {
                return extension;
            }

            if ((gameVersion == GameVersion.Sekiro || gameVersion == GameVersion.EldenRing) 
                && TryGetUnicodeSignature(data, 4, out signature)
                && TryGetFileExtension(signature, out extension, gameVersion))
            {
                return extension;
            }

            if (TryGetAsciiSignature(data, 26, out signature)
                && TryGetFileExtension(signature.Substring(12, 14), out extension, gameVersion))
            {
                return extension;
            }

            //Debug.WriteLine($"Unknown signature: '{BitConverter.ToString(Encoding.ASCII.GetBytes(signature)).Replace("-", " ")}'");
            return ".bin";
        }

        /// <summary>
        /// Inner method for <code>GetDataExtension</code> that returns the first 4 bytes of a file as a string.
        /// </summary>
        private static bool TryGetAsciiSignature(Stream stream, int signatureLength, out string signature)
        {
            const int asciiBytesPerChar = 1;
            return TryGetSignature(stream, Encoding.ASCII, asciiBytesPerChar, signatureLength, out signature);
        }

        /// <summary>
        /// Inner method for <code>GetDataExtension</code> that returns the first 8 bytes of a file, interpreted as UTF-16 characters, as a string.
        /// </summary>
        private static bool TryGetUnicodeSignature(Stream stream, int signatureLength, out string signature)
        {
            const int unicodeBytesPerChar = 2;
            return TryGetSignature(stream, Encoding.Unicode, unicodeBytesPerChar, signatureLength, out signature);
        }

        /// <summary>
        /// Inner method for <code>GetDataExtension</code> that returns the first <code>signatureLength</code> characters of a file as a string.
        /// </summary>
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

        /// <summary>
        /// Inner method for <code>GetDataExtension</code> that returns the extension for a given signature, or ".bin" if the signature is unrecognized.
        /// </summary>
        /// <returns>Whether the signature was recognized</returns>
        private static bool TryGetFileExtension(string signature, out string extension, GameVersion gameVersion)
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
                    if (gameVersion == GameVersion.DarkSouls2) extension = ".fltparam"; // DS II
                    else extension = ".gparam"; // DS III
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
                    extension = ".nva";
                    return true;
                case "NVMC":
                    extension = ".nvc";
                    return true;
                case "MSB ":
                    extension = ".msb";
                    return true;
                case "BJBO":
                    extension = ".bjbo"; // ?
                    return true;
                case "ONAV":
                    extension = ".onav";
                    return true;
                case "mpWF":
                    extension = ".mpw";
                    return true;
                case "BOEG":
                    extension = ".breakgeom";
                    return true;
                case "BKHD":
                    extension = ".bnk";
                    return true;
                case "PSC\0": //shader pipeline state cache
                    extension = ".dat";
                    return true;
                default:
                    extension = ".bin";
                    return false;
            }
        }

        /// <summary>
        /// Decrypts a .bhd and writes the decrypted binary file to disk
        /// </summary>
        private static void UnpackBhdFile(Options options)
        {
            using (var inputStream = DecryptBhdFile(options.InputPath, options.InputGameVersion))
            using (var outputStream = File.OpenWrite(options.OutputPath))
            {
                inputStream.WriteTo(outputStream);
            }
        }

        /// <summary>
        /// Unpacks a .bnd file from disk and writes the unpacked files to disk
        /// </summary>
        private static void UnpackBndFile(Options options)
        {
            using (FileStream inputStream = new FileStream(options.InputPath, FileMode.Open, FileAccess.Read))
            {
                UnpackBndFile(inputStream, options);
            }
        }

        /// <summary>
        /// Unpacks a .bnd file from a <code>Stream</code> ans writes the unpacked files to disk
        /// </summary>
        /// <returns>The number of bytes written to disk</returns>
        private static ulong UnpackBndFile(Stream inputStream, Options options)
        {
            Bnd4File file = Bnd4File.ReadBnd4File(inputStream);
            ulong bytesWritten = 0;
            foreach (var entry in file.Entries)
            {
                try
                {
                    string fileName = FileNameDictionary.NormalizeFileName(entry.FileName);
                    bytesWritten += ProcessFile(fileName, options, new MemoryStream(entry.EntryData));
                } catch { }
            }
            return bytesWritten;
        }

        /// <summary>
        /// Unpacks a .sl2 file from disk and writes the unpacked user data to disk
        /// </summary>
        /// <param name="options"></param>
        private static void UnpackSl2File(Options options)
        {
            using (FileStream inputStream = new FileStream(options.InputPath, FileMode.Open, FileAccess.Read))
            {
                byte[] key = GetSavegameKey(options.InputGameVersion);
                Sl2File sl2File = Sl2File.ReadSl2File(inputStream, key);
                foreach (var userData in sl2File.UserData)
                {
                    string outputFilePath = Path.Combine(options.OutputPath, userData.Name);
                    File.WriteAllBytes(outputFilePath, userData.DecryptedUserData());
                }
            }
        }

        /// <summary>
        /// Gets the key used to encypt .sl2 save data for a given game
        /// </summary>
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
                case GameVersion.EldenRing:
                    key = DecryptionKeys.UserDataKeyEr;
                    break;
                default:
                    key = new byte[16];
                    break;
            }

            return key;
        }

        /// <summary>
        /// Unpacks a regulation file from disk and writes the unpacked and decrypted files to disk
        /// </summary>
        private static void UnpackRegulationFile(Options options)
        {
            using (FileStream inputStream = new FileStream(options.InputPath, FileMode.Open, FileAccess.Read))
            {
                byte[] key = GetRegulationKey(options.InputGameVersion);
                EncFile encryptedFile = EncFile.ReadEncFile(inputStream, key, options.InputGameVersion);
                DcxFile compressedRegulationFile = DcxFile.Read(encryptedFile.Data);
                UnpackBndFile(new MemoryStream(compressedRegulationFile.Decompress()), options);
            }
        }

        /// <summary>
        /// Gets the key used to encrypt the regulation file for the given game
        /// </summary>
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
                default:
                    key = new byte[16];
                    break;
            }

            return key;
        }

        /// <summary>
        /// Decompresses a .dcx file from disk and writed the decompressed data to disk
        /// </summary>
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
                    string extension = GetDataExtension(new MemoryStream(decompressedData), options.InputGameVersion);
                    if (extension != ".dcx")
                    {
                        outputFilePath += extension;
                    }
                }

                File.WriteAllBytes(outputFilePath, decompressedData);
            }
        }

        /// <summary>
        /// Unpacks an unencrypted bdf4 file (.bdt) and writed the unpacked files to disk
        /// </summary>
        private static void UnpackBdf4File(Options options)
        {
            string bdfDirectoryPath = Path.GetDirectoryName(options.InputPath);
            string bhf4Extension = Path.GetExtension(options.InputPath).Replace("bdt", "bhd");
            string bhf4FilePath = Path.Combine(bdfDirectoryPath, Path.GetFileNameWithoutExtension(options.InputPath) + bhf4Extension);
            if (!File.Exists(bhf4FilePath))
            {
                // HACK: Adding 132 to a hash of a text that ends with XXX.bdt will give you the hash of XXX.bhd for games before Elden Ring
                string[] split = Path.GetFileNameWithoutExtension(options.InputPath).Split('_');
                uint hash;
                if (uint.TryParse(split[0], out hash))
                {
                    hash += 132;
                    split[0] = hash.ToString("D10");
                    bhf4FilePath = Path.Combine(bdfDirectoryPath, string.Join("_", split) + ".bhd");
                }
            }
            if (!File.Exists(bhf4FilePath)) {
                // HACK: Adding 516 to a hash of a text that ends with XXX.bdt will give you the hash of XXX.bhd for Elden Ring
                string[] split = Path.GetFileNameWithoutExtension(options.InputPath).Split('_');
                ulong hash;
                if (ulong.TryParse(split[0], out hash)) {
                    hash += 516;
                    split[0] = hash.ToString("D10");
                    bhf4FilePath = Path.Combine(bdfDirectoryPath, string.Join("_", split) + ".bhd");
                }
            }

            using (FileStream bdf4InputStream = File.Open(options.InputPath, FileMode.Open, FileAccess.Read))
            {
                using (Stream bhf4Stream = File.Open(bhf4FilePath, FileMode.Open)) {
                    UnpackBdf4File(bdf4InputStream, bhf4Stream, options);
                }
            }
        }

        /// <summary>
        /// Unpacks a bdf4 file from its component streams and writes the unpacked files to disk.
        /// </summary>
        /// <returns>The number of bytes written to disk</returns>
        private static ulong UnpackBdf4File(Stream bdt, Stream bhd, Options options)
        {
            ulong bytesWritten = 0;
            var header = Bhf4File.OpenBhf4File(bhd);
            var bdtStream = Bdf4FileStream.OpenStream(bdt);
            foreach (var entry in header.Entries) {
                MemoryStream data = bdtStream.Read(entry.FileOffset, entry.FileSize);
                bytesWritten += ProcessFile(entry.FileName, options, data);
            }
            return bytesWritten;
        }

        /// <summary>
        /// Unpacks a .tpf file from disk and writes the unpacked files to disk
        /// </summary>
        private static void UnpackTpfFile(Options options)
        {
            using (FileStream inputStream = new FileStream(options.InputPath, FileMode.Open, FileAccess.Read))
            {
                UnpackTpfFile(inputStream, options);
            }
        }

        /// <summary>
        /// Unpacks a .tpf file from a given <code>Stream</code> and writes the unpacked files to disk
        /// </summary>
        /// <returns>The number of bytes written to disk</returns>
        private static ulong UnpackTpfFile(Stream data, Options options)
        {
            ulong bytesWritten = 0;
            TpfFile tpfFile = TpfFile.OpenTpfFile(data);
            foreach (var entry in tpfFile.Entries) {
                string outputFilePath = Path.Combine(options.OutputPath, entry.FileName);
                Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
                using (var outputStream = File.Create(outputFilePath)) {
                    bytesWritten += entry.Write(outputStream);
                }
            }
            return bytesWritten;
        }

        /// <summary>
        /// "Unpacks" an unencrypted bhf4 (.bhd) file (has no effect)
        /// </summary>
        private static void UnpackBhf4File(Options options)
        {
            Console.WriteLine($"The file : \'{options.InputPath}\' is already decrypted.");
        }

        /// <summary>
        /// Decrypts an encrypted .bhd file from disk and returns the decrypted data
        /// </summary>
        public static MemoryStream DecryptBhdFile(string filePath, GameVersion version)
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

        /// <summary>
        /// Unpacks a .param file from disk and writes the unpacked files to disk
        /// </summary>
        private static ulong UnpackParamFile(Options options)
        {
            using (FileStream inputStream = new FileStream(options.InputPath, FileMode.Open, FileAccess.Read))
            {
                return UnpackParamFile(inputStream, options.InputPath, options.OutputPath);
            }
        }

        /// <summary>
        /// Unpacks a .param file from a given <code>stream</code> and writes the unpacked files to disk
        /// </summary>
        /// <param name="fileName">The name of the param file (used to find a .csv description of its format)</param>
        /// <param name="outputPath">The path to write the result to</param>
        /// <returns>The number of bytes written to disk</returns>
        private static ulong UnpackParamFile(Stream inputStream, string fileName, string outputPath)
        {
            ParamFile paramFile = ParamFile.ReadParamFile(inputStream);
            ParamFormatDesc d = ParamFormatDesc.Read(fileName);
            if (d == null) {
                var dir = Path.Combine(Directory.GetParent(outputPath).FullName, Path.GetFileNameWithoutExtension(outputPath));
                Directory.CreateDirectory(dir);
                ulong bytesWritten = 0;
                foreach (var entry in paramFile.Entries) {
                    string entryName = $"{entry.Id:D10}.{paramFile.StructName}";
                    string outputFilePath = Path.Combine(dir, entryName);
                    Directory.CreateDirectory(Directory.GetParent(outputFilePath).FullName);
                    File.WriteAllBytes(outputFilePath, entry.Data);
                    bytesWritten += (ulong)entry.Data.Length;
                }
                return bytesWritten;
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
                return (ulong)ans.Length;
            }
        }

        /// <summary>
        /// Unpacks a .fmg file from disk and writes the result to disk
        /// </summary>
        private static void UnpackFmgFile(Options options)
        {
            using (FileStream inputStream = new FileStream(options.InputPath, FileMode.Open, FileAccess.Read))
            {
                UnpackFmgFile(inputStream, options.OutputPath);
            }
        }

        /// <summary>
        /// Unpacks a .fmg file from a given <code>Stream</code> an writes the result to disk
        /// </summary>
        public static ulong UnpackFmgFile(Stream inputStream, string outputPath)
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
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            File.WriteAllText(outputPath, builder.ToString());
            return (ulong)new FileInfo(outputPath).Length;
        }

        /// <summary>
        /// Unpacks an enfl (.entryfilelist) file from disk and writes the result to disk
        /// </summary>
        private static void UnpackEnflFile(Options options)
        {
            var ans = UnpackEnflFile(new FileStream(options.InputPath, FileMode.Open));
            Directory.CreateDirectory(Path.GetDirectoryName(options.OutputPath));
            File.WriteAllText(options.OutputPath + ".csv", ans);
        }

        /// <summary>
        /// Unpacks an enfl (.entryfilelist) file from a given <code>Stream</code> and writes the result to disk
        /// </summary>
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
