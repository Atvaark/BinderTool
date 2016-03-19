using System;
using System.Collections.Generic;
using System.IO;
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
            
            if (options.InputType != FileType.EncryptedBhd)
            {
                Directory.CreateDirectory(options.OutputPath);
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
                case FileType.Bnd:
                    UnpackBndFile(options);
                    break;
                case FileType.Savegame:
                    UnpackSl2File(options);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void ShowUsageInfo()
        {
            Console.WriteLine("BinderTool by Atvaark\n" + "  A tool for unpacking Dark Souls II Ebl.Bdt, Ebl.Bhd, Bdt, Bnd, Dcx and Sl2 files\n" + "Usage:\n" + "  BinderTool file_path [output_path]\n" + "Examples:\n" + "  BinderTool GameDataEbl.bdt GameDataDump");
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
                        "gamedata", "gamedata_patch", "dlc_data", "dlc_menu", "map", "chr", "parts", "eventmaker", "ezstate", "menu", "text", "icon"
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

        private static void UnpackBdtFile(Options options)
        {
            string dictionaryPath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Dictionary.csv");
            FileNameDictionary dictionary = FileNameDictionary.OpenFromFile(dictionaryPath);

            string fileNameWithoutExtension = Path.GetFileName(options.InputPath).Replace("Ebl.bdt", "");
            string inputFileWithoutExtensionPath = Path.Combine(Path.GetDirectoryName(options.InputPath), fileNameWithoutExtension);

            Bhd5File bhdFile = Bhd5File.Read(DecryptBhdFile(inputFileWithoutExtensionPath));
            Bdt5FileStream bdtStream = Bdt5FileStream.OpenFile(options.InputPath, FileMode.Open, FileAccess.Read);

            List<string> archiveNames = GetArchiveNamesFromFileName(fileNameWithoutExtension);
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

                        string fileName;
                        if (!dictionary.TryGetFileName(entry.FileNameHash, archiveNames, out fileName))
                        {
                            string extension;
                            if (TryGetFileExtension(signature, out extension) == false)
                            {
                                extension = ".bin";
                            }

                            fileName = string.Format("{0:D10}_{1}{2}", entry.FileNameHash, fileNameWithoutExtension, extension);
                        }

                        string newFileNamePath = Path.Combine(options.OutputPath, fileName);
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

        private static void UnpackBhdFile(Options options)
        {
            string fileNameWithoutExtension = Path.GetFileName(options.InputPath).Replace("Ebl.bhd", "");
            string inputFileWithoutExtensionPath = Path.Combine(Path.GetDirectoryName(options.InputPath), fileNameWithoutExtension);

            using (var inputStream = DecryptBhdFile(inputFileWithoutExtensionPath))
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
                string outputFilePath = Path.Combine(outputPath, entry.FileName);
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
            using (FileStream inputStream = new FileStream(options.InputPath, FileMode.Open))
            {
                RegulationFile encryptedRegulationFile = RegulationFile.ReadRegulationFile(inputStream);
                DcxFile compressedRegulationFile = DcxFile.Read(new MemoryStream(encryptedRegulationFile.DecryptedData));
                UnpackBndFile(new MemoryStream(compressedRegulationFile.DecompressedData), options.OutputPath);
            }
        }

        private static void UnpackDcxFile(Options options)
        {
            string unpackedFileName = Path.GetFileNameWithoutExtension(options.InputPath);
            string outputFilePath = Path.Combine(options.OutputPath, unpackedFileName);

            using (FileStream inputStream = new FileStream(options.InputPath, FileMode.Open))
            {
                DcxFile dcxFile = DcxFile.Read(inputStream);
                File.WriteAllBytes(outputFilePath, dcxFile.DecompressedData);
            }
        }

        private static void UnpackBdf4File(Options options)
        {
            var bdfDirectory = Path.GetDirectoryName(options.InputPath);
            // TODO: Add a command line option to specify the bhf file. (Since bhf4 and bdf4 have different hashes)

            var bhf4FilePath = options.InputPath.Substring(0, options.InputPath.Length - 3) + "bhd";

            if (File.Exists(bhf4FilePath) == false)
            {
                // HACK: Adding 132 to a hash of a text that ends with XXX.bdt will give you the hash of XXX.bhd.
                string[] split = Path.GetFileNameWithoutExtension(options.InputPath).Split('_');
                uint hash;
                if (uint.TryParse(split[0], out hash))
                {
                    hash += 132;
                    split[0] = hash.ToString();
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

        private static MemoryStream DecryptBhdFile(string inputFileWithoutExtensionPath)
        {
            var bhdPath = inputFileWithoutExtensionPath + "Ebl.bhd";
            var pemPath = inputFileWithoutExtensionPath + "KeyCode.pem";
            return CryptographyUtility.DecryptRsa(bhdPath, pemPath);
        }
    }
}
