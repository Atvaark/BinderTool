using System;
using System.IO;
using System.Text.RegularExpressions;
using BinderTool.Core;
using CommandLine;

namespace BinderTool
{
    internal class Options
    {
        [Option('g', "game", Default = GameVersion.Detect, HelpText = "The game the file(s) are from")]
        public GameVersion InputGameVersion { get; set; }

        [Option('t', "type", Default = FileType.Detect, HelpText = "The type of file to extract. Can be detected in some cases.")]
        public FileType InputType { get; set; }

        [Value(0, Required = true, HelpText = "The input file or folder")]
        public string InputPath { get; set; }

        [Value(1, Required = false, Default = null, HelpText = "The output file or folder")]
        public string OutputPath { get; set; }

        [Option("extract-bnd", Default = false, HelpText = "Automatically extract bnd files instead of outputting the .bnf")]
        public bool AutoExtractBnd { get; set; }

        [Option("extract-param", Default = false, HelpText = "Automatically extract param files instead of outputting the .param")]
        public bool AutoExtractParam { get; set; }

        [Option('r', "recurse", Default = false, HelpText = "When using folder input, recurse to child folders")]
        public bool Recurse { get; set; }

        public Options Clone()
        {
            return new Options {
                InputGameVersion = InputGameVersion,
                InputType = InputType,
                InputPath = InputPath,
                OutputPath = OutputPath,
                AutoExtractBnd = AutoExtractBnd,
                AutoExtractParam = AutoExtractParam,
                Recurse = Recurse
            };
        }

        public static (FileType, GameVersion) GetFileType(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (Directory.Exists(path)) return (FileType.Folder, GameVersion.Common);

            var info = new FileInfo(path);
            var fileName = Path.GetFileName(path);

            if (fileName == "Data0.bdt")
            {
                //ds3 data0 should be smaller than 10mb
                if (info.Length > 10_000_000) return (FileType.EncryptedBdt, GameVersion.EldenRing);
                return (FileType.Regulation, GameVersion.DarkSouls3);
            }

            if (fileName == "regulation.bin")
            {
                return (FileType.Regulation, GameVersion.EldenRing);
            }

            if (fileName == "enc_regulation.bnd.dcx")
            {
                return (FileType.Regulation, GameVersion.DarkSouls2);
            }

            // file.dcx
            // file.bnd.dcx
            if (fileName.EndsWith(".dcx", StringComparison.InvariantCultureIgnoreCase))
            {
                return (FileType.Dcx, GameVersion.Common);
            }

            // .anibnd
            // .chrbnd
            // .chrtpfbhd
            // .mtdbnd
            // .shaderbnd
            // .objbnd
            // .partsbnd
            // .rumblebnd
            // .hkxbhd
            // .tpfbhd
            // .shaderbdle
            // .shaderbdledebug
            if (fileName.EndsWith("bnd", StringComparison.InvariantCultureIgnoreCase)
                || fileName.EndsWith("bdle", StringComparison.InvariantCultureIgnoreCase)
                || fileName.EndsWith("bdledebug", StringComparison.InvariantCultureIgnoreCase))
            {
                return (FileType.Bnd, GameVersion.Common);
            }

            if (fileName.EndsWith("enfl") || fileName.EndsWith("entryfilelist")) {
                return (FileType.Enfl, GameVersion.Common);
            }

            // DS30000.sl2
            if (Regex.IsMatch(fileName, @"^DS3\d+.*\.sl2", RegexOptions.IgnoreCase))
            {
                return (FileType.Savegame, GameVersion.DarkSouls3);
            }

            // DARKSII0000.sl2
            if (Regex.IsMatch(fileName, @"^DARKSII\d+.*\.sl2", RegexOptions.IgnoreCase))
            {
                return (FileType.Savegame, GameVersion.DarkSouls2);
            }
            
            if (Regex.IsMatch(fileName, @"^(?:Data|DLC)\d?\.bdt$", RegexOptions.IgnoreCase))
            {
                var parentFolder = Directory.GetParent(path).FullName;
                var hasSekiro = File.Exists(parentFolder + @"\sekiro.exe");
                var hasER = File.Exists(parentFolder + @"\eldenring.exe") || File.Exists(parentFolder + @"\start_protected_game.exe");
                var hasDS3 = File.Exists(parentFolder + @"\DarkSoulsIII.exe");
                if (hasSekiro) return (FileType.EncryptedBdt, GameVersion.Sekiro);
                if (hasER) return (FileType.EncryptedBdt, GameVersion.EldenRing);
                if (hasDS3) return (FileType.EncryptedBdt, GameVersion.DarkSouls3);
                return (FileType.EncryptedBdt, GameVersion.Detect);
            }

            if (Regex.IsMatch(fileName, @"^[^\W_]+Ebl\.bdt$", RegexOptions.IgnoreCase))
            {
                return (FileType.EncryptedBdt, GameVersion.DarkSouls2);
            }

            if (Regex.IsMatch(fileName, @"^(?:Data|DLC|)\d?\.bhd$", RegexOptions.IgnoreCase))
            {
                var parentFolder = Directory.GetParent(path).FullName;
                var hasSekiro = File.Exists(parentFolder + @"\sekiro.exe");
                var hasER = File.Exists(parentFolder + @"\eldenring.exe") || File.Exists(parentFolder + @"\start_protected_game.exe");
                var hasDS3 = File.Exists(parentFolder + @"\DarkSoulsIII.exe");
                if (hasSekiro) return (FileType.EncryptedBhd, GameVersion.Sekiro);
                if (hasER) return (FileType.EncryptedBhd, GameVersion.EldenRing);
                if (hasDS3) return (FileType.EncryptedBhd, GameVersion.DarkSouls3);
                return (FileType.EncryptedBhd, GameVersion.Detect);
            }

            if (Regex.IsMatch(fileName, @"^[^\W_]+Ebl\.bhd$", RegexOptions.IgnoreCase))
            {
                return (FileType.EncryptedBhd, GameVersion.DarkSouls2);
            }

            // file.bdt
            // file.hkxbdt
            // file.tpfbdt
            if (fileName.EndsWith("bdt", StringComparison.InvariantCultureIgnoreCase))
            {
                return (FileType.Bdt, GameVersion.Common);
            }

            // file.bhd
            // file.hkxbhd
            // file.tpfbhd
            if (fileName.EndsWith("bhd", StringComparison.InvariantCultureIgnoreCase))
            {
                return (FileType.Bhd, GameVersion.Common);
            }

            if (fileName.EndsWith(".tpf", StringComparison.InvariantCultureIgnoreCase))
            {
                return (FileType.Tpf, GameVersion.Common);
            }

            if (fileName.EndsWith(".param", StringComparison.InvariantCultureIgnoreCase))
            {
                return (FileType.Param, GameVersion.Common);
            }

            if (fileName.EndsWith(".fmg", StringComparison.InvariantCultureIgnoreCase))
            {
                return (FileType.Fmg, GameVersion.Common);
            }

            return (FileType.Unknown, GameVersion.Common);
        }
    }
}