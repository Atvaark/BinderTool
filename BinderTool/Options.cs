using System;
using System.IO;
using System.Text.RegularExpressions;

namespace BinderTool
{
    internal class Options
    {
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

            options.InputType = GetFileType(Path.GetFileName(options.InputPath));

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

        private static FileType GetFileType(string fileName)
        {
            if (fileName == null) throw new ArgumentNullException(nameof(fileName));

            // file.dcx
            // file.bnd.dcx
            if (fileName.EndsWith(".dcx", StringComparison.InvariantCultureIgnoreCase))
            {
                return FileType.Dcx;
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
                return FileType.Bnd;
            }

            // DS30000.sl2
            if (fileName.EndsWith(".sl2", StringComparison.CurrentCultureIgnoreCase))
            {
                return FileType.Savegame;
            }

            if (fileName == "Data0.bdt")
            {
                return FileType.Regulation;
            }

            if (Regex.IsMatch(fileName, @"^(?:Data|DLC)\d\.bdt$", RegexOptions.IgnoreCase))
            {
                return FileType.EncryptedBdt;
            }

            if (Regex.IsMatch(fileName, @"^(?:Data|DLC)\d\.bhd$", RegexOptions.IgnoreCase))
            {
                return FileType.EncryptedBhd;
            }

            // file.bdt
            // file.hkxbdt
            // file.tpfbdt
            if (fileName.EndsWith("bdt", StringComparison.InvariantCultureIgnoreCase))
            {
                return FileType.Bdt;
            }

            // file.bhd
            // file.hkxbhd
            // file.tpfbhd
            if (fileName.EndsWith("bhd", StringComparison.InvariantCultureIgnoreCase))
            {
                return FileType.Bhd;
            }

            if (fileName.EndsWith(".tpf", StringComparison.InvariantCultureIgnoreCase))
            {
                return FileType.Tpf;
            }

            if (fileName.EndsWith(".param", StringComparison.InvariantCultureIgnoreCase))
            {
                return FileType.Param;
            }

            if (fileName.EndsWith(".fmg", StringComparison.InvariantCultureIgnoreCase))
            {
                return FileType.Fmg;
            }

            return FileType.Unknown;
        }
    }
}