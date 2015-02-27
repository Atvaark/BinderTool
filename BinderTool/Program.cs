using System;
using System.Collections.Generic;
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

namespace BinderTool
{
    internal static class Program
    {
        public static readonly Dictionary<string, Dictionary<uint, List<string>>> PossibleFileNamesDictionaries =
            new Dictionary<string, Dictionary<uint, List<string>>>();

        private static void Main(string[] args)
        {
            if (args.Length < 1 || args.Length > 2)
            {
                ShowUsageInfo();
                return;
            }

            string path = args[0];
            if (File.Exists(path) == false)
            {
                ShowUsageInfo();
                return;
            }
            string outputPath = args.Length == 2
                ? args[1]
                : Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
            Directory.CreateDirectory(outputPath);

            if (path.EndsWith("dcx", StringComparison.InvariantCultureIgnoreCase))
            {
                UnpackDcxFile(path, outputPath);
            }
            else if (path.EndsWith("Ebl.bdt", StringComparison.InvariantCultureIgnoreCase))
            {
                InitPossibleFileNames();
                UnpackBdtFile(path, outputPath);
            }
            else if (path.EndsWith("bdt", StringComparison.InvariantCultureIgnoreCase))
            {
                UnpackBdf4File(path, outputPath);
            }
            else if (path.EndsWith("bnd", StringComparison.InvariantCultureIgnoreCase))
            {
                UnpackBndFile(path, outputPath);
            }
        }

        private static void ShowUsageInfo()
        {
            Console.WriteLine("BinderTool by Atvaark\n" +
                              "  A tool for unpacking Dark Souls II Ebl.Bdt, Bdt, Bnd and Dcx files\n" +
                              "Usage:\n" +
                              "  BinderTool file_path [output_path]\n" +
                              "Examples:\n" +
                              "  BinderTool GameDataEbl.bdt GameDataDump");
        }

        private static void InitPossibleFileNames()
        {
            // TODO: Find out the names of the high quality files.
            // e.g. this is pair of texture packs has different name hashes while the latter contains the same textures but in higher quality.
            // 2500896703   gamedata   /model/chr/c3096.texbnd
            // 1276904764   chrhq      /???.texbnd
            // TODO: Remove the hash value from the dictionary and calculate it here.
            string dictionaryPath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
                "PossibleFilenames.csv");
            string[] lines = File.ReadAllLines(dictionaryPath);
            foreach (string line in lines)
            {
                string[] splitLine = line.Split('\t');
                uint hash = uint.Parse(splitLine[0]);
                string archiveName = splitLine[1];
                string fileName = splitLine[2];

                Dictionary<uint, List<string>> archiveDictionary;
                if (PossibleFileNamesDictionaries.TryGetValue(archiveName, out archiveDictionary) == false)
                {
                    archiveDictionary = new Dictionary<uint, List<string>>();
                    PossibleFileNamesDictionaries.Add(archiveName, archiveDictionary);
                }

                List<string> fileNameList;
                if (archiveDictionary.TryGetValue(hash, out fileNameList) == false)
                {
                    fileNameList = new List<string>();
                    archiveDictionary.Add(hash, fileNameList);
                }

                if (fileNameList.Contains(fileName) == false)
                {
                    fileNameList.Add(fileName);
                }
            }
        }

        private static string GetFileName(uint hash, List<string> archiveNames)
        {
            foreach (var archiveName in archiveNames)
            {
                string fileName;
                if (TryGetFileName(hash, archiveName, out fileName))
                {
                    return fileName;
                }
            }
            return "";
        }

        private static List<string> GetArchiveNamesFromFileName(string archiveFileName)
        {
            // TODO: Find out how the game loads high quality assets
            List<string> archiveNames;
            switch (archiveFileName)
            {
                case "GameData":
                    archiveNames = new List<string>
                    {
                        "gamedata",
                        "gamedata_patch",
                        "dlc_data",
                        "dlc_menu",
                        "map",
                        "chr",
                        "parts",
                        "eventmaker",
                        "ezstate",
                        "menu",
                        "text",
                        "icon"
                    };
                    break;
                case "HqChr":
                    archiveNames = new List<string> {"chrhq"};
                    break;
                case "HqMap":
                    archiveNames = new List<string> {"maphq"};
                    break;
                case "HqObj":
                    archiveNames = new List<string> {"objhq"};
                    break;
                case "HqParts":
                    archiveNames = new List<string> {"partshq"};
                    break;
                default:
                    archiveNames = new List<string>();
                    break;
            }
            return archiveNames;
        }

        private static bool TryGetFileName(uint hash, string archiveName, out string fileName)
        {
            fileName = "";
            Dictionary<uint, List<string>> archiveDictionary;
            if (PossibleFileNamesDictionaries.TryGetValue(archiveName, out archiveDictionary))
            {
                List<string> fileNames;
                if (archiveDictionary.TryGetValue(hash, out fileNames))
                {
                    // TODO: There should be no hash collisions inside an archive.
                    //if (fileNames.Count == 1)
                    //{
                    //fileName = fileNames.Single().Replace('/', '\\').TrimStart('\\');
                    fileName = fileNames.First().Replace('/', '\\').TrimStart('\\');
                    return true;
                    //}
                }
            }
            return false;
        }

        private static void UnpackBdtFile(string bdtPath, string outputDirectory)
        {
            var fileNameWithoutExtension = Path.GetFileName(bdtPath).Replace("Ebl.bdt", "");
            string inputFileWithoutExtensionPath = Path.Combine(Path.GetDirectoryName(bdtPath), fileNameWithoutExtension);
            var bhdPath = inputFileWithoutExtensionPath + "Ebl.bhd";
            var pemPath = inputFileWithoutExtensionPath + "KeyCode.pem";
            var archiveNames = GetArchiveNamesFromFileName(fileNameWithoutExtension);

            Bhd5File bhdFile = Bhd5File.Read(CryptographyUtility.DecryptRsa(bhdPath, pemPath));
            Bdt5FileStream bdtStream = Bdt5FileStream.OpenFile(bdtPath, FileMode.Open, FileAccess.Read);
            Directory.CreateDirectory(outputDirectory);

            foreach (var bucket in bhdFile.GetBuckets())
            {
                foreach (var entry in bucket.GetEntries())
                {
                    MemoryStream data = bdtStream.ReadBhd5Entry(entry);
                    if (entry.AesKey != null)
                    {
                        data = CryptographyUtility.DecryptAesEcb(data, entry.AesKey.Key);
                    }

                    if (data.Length >= 4)
                    {
                        BinaryReader reader = new BinaryReader(data, Encoding.ASCII, true);
                        string signature = new string(reader.ReadChars(4));
                        data.Position = 0;

                        string fileName = GetFileName(entry.FileNameHash, archiveNames);

                        if (fileName == "")
                        {
                            string extension;
                            if (TryGetFileExtension(signature, out extension) == false)
                            {
                                extension = ".bin";
                            }

                            fileName = string.Format("{0:D10}_{1}{2}", entry.FileNameHash,
                                fileNameWithoutExtension, extension);
                        }

                        string newFileNamePath = Path.Combine(outputDirectory, fileName);
                        Directory.CreateDirectory(Path.GetDirectoryName(newFileNamePath));
                        File.WriteAllBytes(newFileNamePath, data.ToArray());
                    }
                }
            }
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
            }
            return false;
        }

        private static void UnpackBndFile(string path, string outputPath)
        {
            Directory.CreateDirectory(outputPath);
            using (FileStream input = new FileStream(path, FileMode.Open))
            {
                Bnd4File file = Bnd4File.Read(input);

                foreach (var entry in file.Entries)
                {
                    string outputFilePath = Path.Combine(outputPath, entry.FileName);
                    File.WriteAllBytes(outputFilePath, entry.EntryData);
                }
            }
        }

        private static void UnpackDcxFile(string dcxPath, string outputPath)
        {
            string unpackedFileName = Path.GetFileNameWithoutExtension(dcxPath);
            string unpackedFilePath = Path.Combine(outputPath, unpackedFileName);


            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            using (FileStream input = new FileStream(dcxPath, FileMode.Open))
            {
                DcxFile dcxFile = DcxFile.Read(input);
                File.WriteAllBytes(unpackedFilePath, dcxFile.DecompressedData);
            }
        }

        private static void UnpackBdf4File(string bdfPath, string outputPath)
        {
            var bdfDirectory = Path.GetDirectoryName(bdfPath);
            // TODO: Add a command line option to specify the bhf file. (Since bhf4 and bdf4 have different hashes)

            var bhf4FilePath = bdfPath.Substring(0, bdfPath.Length - 3) + "bhd"; 

            if (File.Exists(bhf4FilePath) == false)
            {
                // HACK: Adding 132 to a hash of a text that ends with XXX.bdt will give you the hash of XXX.bhd.
                string[] split = Path.GetFileNameWithoutExtension(bdfPath).Split('_');
                uint hash;
                if (uint.TryParse(split[0], out hash))
                {
                    hash += 132;
                    split[0] = hash.ToString();
                    bhf4FilePath = Path.Combine(bdfDirectory, String.Join("_", split) + ".bhd");
                }
            }


            using (FileStream bhf4Input = new FileStream(bhf4FilePath, FileMode.Open))
            using (FileStream bdf4Input = new FileStream(bdfPath, FileMode.Open))
            {
                Bhf4File bhf4File = Bhf4File.ReadBhf4File(bhf4Input);
                Bdf4File bdf4File = Bdf4File.ReadBdf4File(bdf4Input);
                foreach (var file in bdf4File.ReadData(bdf4Input, bhf4File))
                {
                    ExportFile(file, outputPath);
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
    }
}
