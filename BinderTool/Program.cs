using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BinderTool.Core;
using BinderTool.Core.Bdt5;
using BinderTool.Core.BHD5;
using BinderTool.Core.Bnd4;
using BinderTool.Core.Dcx;

namespace BinderTool
{
    internal static class Program
    {
        public static readonly Dictionary<string, Dictionary<uint, List<string>>> PossibleFileNamesDictionaries =
            new Dictionary<string, Dictionary<uint, List<string>>>();

        private static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                ShowUsageInfo();
                return;
            }

            string path = args[0];
            string outputPath = args[1];

            if (File.Exists(path) == false)
            {
                ShowUsageInfo();
                return;
            }

            if (path.EndsWith("dcx"))
            {
                UnpackDcxFile(path, outputPath);
            }
            else if (path.EndsWith("bdt"))
            {
                if (path.EndsWith("Ebl.bdt", StringComparison.InvariantCultureIgnoreCase) == false)
                {
                    Console.WriteLine("Error: The BDT file has to end with Ebl.bdt");
                    return;
                }

                InitPossibleFileNames();
                UnpackBdtFile(path, outputPath);
            }
            else if (path.EndsWith("bnd") || path.EndsWith("bnd4")) // TODO: Add the remaining bnd4 file extensions
            {
                UnpackBndFile(path, outputPath);
            }

            // TODO: Create a BDF4 unpacker
        }

        private static void ShowUsageInfo()
        {
            Console.WriteLine("BinderTool by Atvaark\n" +
                              "  A tool for unpacking Dark Souls II Ebl.Bdt, Bnd4 and Dcx files\n" +
                              "Usage:\n" +
                              "  BinderTool file_path output_path\n" +
                              "Examples:\n" +
                              "  BinderTool GameDataEbl.bdt GameDataDump");
        }

        private static void InitPossibleFileNames()
        {
            // TODO: Find out the names of the high quality files.
            // e.g. this is pair of texture packs has different name hashes while the latter contains the same textures but in higher quality.
            // 2500896703   gamedata   /model/chr/c3096.texbnd
            // 1276904764   chrhq      /???.texbnd
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
                    if (fileNames.Count == 1)
                    {
                        fileName = fileNames.Single().Replace('/', '\\').TrimStart('\\');
                        return true;
                    }
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
                            string extension = "";
                            if (signature == "BND4")
                            {
                                extension = ".bnd4";
                                //Bnd4File bnd4File = Bnd4File.Read(data);
                            }
                            else if (signature == "DCX\0")
                            {
                                extension = ".dcx";
                                DcxFile dcxFile = DcxFile.Read(data);
                                data = new MemoryStream(dcxFile.DecompressedData);
                                // TODO: Remove dcx and guess the file extension again?
                            }
                            else if (signature == "TAE ")
                            {
                                extension = ".tae";
                            }
                            else if (signature == "fSSL")
                            {
                                extension = ".fssl";
                            }
                            else if (signature == "TPF\0")
                            {
                                extension = ".tpf";
                            }
                            else if (signature == "PFBB")
                            {
                                extension = ".pfbb";
                            }
                            else if (signature == "BHF4")
                            {
                                extension = ".bhf4";
                            }
                            else if (signature == "BDF4")
                            {
                                extension = ".bdf4";
                            }
                            else if (signature == "OBJB")
                            {
                                extension = ".breakobj";
                            }
                            else if (signature == "filt")
                            {
                                extension = ".fltparam";
                            }
                            else if (signature == "VSDF")
                            {
                                extension = ".vsd";
                            }
                            else if (signature == "NVG2")
                            {
                                extension = ".ngp";
                            }
                            else
                            {
                                extension = ".dat";
                            }


                            fileName = string.Format("{0:D10}_{1:D10}_{2:D10}_{3}{4}", entry.FileNameHash,
                                entry.FileOffset, entry.FileSize, fileNameWithoutExtension, extension);
                        }

                        string newFileNamePath = Path.Combine(outputDirectory, fileName);
                        Directory.CreateDirectory(Path.GetDirectoryName(newFileNamePath));
                        File.WriteAllBytes(newFileNamePath, data.ToArray());
                    }
                }
            }
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

        private static void UnpackDcxFile(string path, string outputPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            using (FileStream input = new FileStream(path, FileMode.Open))
            {
                DcxFile dcxFile = DcxFile.Read(input);
                File.WriteAllBytes(outputPath, dcxFile.DecompressedData);
            }
        }
    }
}
