using System;
using System.IO;
using System.Text.RegularExpressions;
using BinderTool.Core;

namespace BinderTool
{
    internal class Options
    {
        public DSVersion InputVersion { get; set; }

        public FileType InputType { get; private set; }

        public string InputPath { get; private set; }

        public string OutputPath { get; private set; }

        internal static Options Parse(string[] args)
        {
            Options options = new Options();
            if (args.Length == 0)
            {
                throw new FormatException("Missing arguments");
            }

            options.InputPath = args[0];
            if (File.Exists(options.InputPath) == false)
            {
                throw new FormatException("Input file not found");
            }

            (FileType type, DSVersion version) fileType = GetFileType(Path.GetFileName(options.InputPath));
            options.InputType = fileType.type;
            options.InputVersion = fileType.version;

            if (options.InputType == FileType.Unknown)
            {
                throw new FormatException("Unsupported input file format");
            }

            if (args.Length >= 2)
            {
                options.OutputPath = args[1];
            }
            else
            {
                options.OutputPath = Path.Combine(
                    Path.GetDirectoryName(options.InputPath),
                    Path.GetFileNameWithoutExtension(options.InputPath));
                switch (options.InputType)
                {
                    case FileType.EncryptedBhd:
                        options.OutputPath += "_decrypted.bhd";
                        break;
                    case FileType.Dcx:
                    case FileType.Fmg:
                        break;
                }
            }

            return options;
        }

        private static (FileType, DSVersion) GetFileType(string fileName)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            // file.dcx
            // file.bnd.dcx
            if (fileName.EndsWith(".dcx", StringComparison.InvariantCultureIgnoreCase))
            {
                return (FileType.Dcx, DSVersion.Common);
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
                return (FileType.Bnd, DSVersion.Common);
            }

            // DS30000.sl2
            if (fileName.EndsWith(".sl2", StringComparison.CurrentCultureIgnoreCase))
            {
                return (FileType.Savegame, DSVersion.Common);
            }

            if (fileName == "Data0.bdt")
            {
                return (FileType.Regulation, DSVersion.Common);
            }

            if (Regex.IsMatch(fileName, @"^(?:Data|DLC)\d\.bdt$", RegexOptions.IgnoreCase))
            {
                return (FileType.EncryptedBdt, DSVersion.DarkSouls3);
            }

            if (Regex.IsMatch(fileName, @"^[^\W_]+Ebl\.bdt$", RegexOptions.IgnoreCase))
            {
                return (FileType.EncryptedBdt, DSVersion.DarkSouls2);
            }

            if (Regex.IsMatch(fileName, @"^(?:Data|DLC|)\d\.bhd$", RegexOptions.IgnoreCase))
            {
                return (FileType.EncryptedBhd, DSVersion.DarkSouls3);
            }

            if (Regex.IsMatch(fileName, @"^[^\W_]+Ebl\.bhd$", RegexOptions.IgnoreCase))
            {
                return (FileType.EncryptedBhd, DSVersion.DarkSouls2);
            }

            // file.bdt
            // file.hkxbdt
            // file.tpfbdt
            if (fileName.EndsWith("bdt", StringComparison.InvariantCultureIgnoreCase))
            {
                return (FileType.Bdt, DSVersion.Common);
            }

            // file.bhd
            // file.hkxbhd
            // file.tpfbhd
            if (fileName.EndsWith("bhd", StringComparison.InvariantCultureIgnoreCase))
            {
                return (FileType.Bhd, DSVersion.Common);
            }

            if (fileName.EndsWith(".tpf", StringComparison.InvariantCultureIgnoreCase))
            {
                return (FileType.Tpf, DSVersion.Common);
            }

            if (fileName.EndsWith(".param", StringComparison.InvariantCultureIgnoreCase))
            {
                return (FileType.Param, DSVersion.Common);
            }

            if (fileName.EndsWith(".fmg", StringComparison.InvariantCultureIgnoreCase))
            {
                return (FileType.Fmg, DSVersion.Common);
            }

            return (FileType.Unknown, DSVersion.Common);
        }
    }
}