using System;
using System.IO;

namespace BinderTool
{
    internal class Options
    {
        public FileType InputType { get; set; }

        public string InputPath { get; set; }

        public string OutputPath { get; set; }

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

            options.InputType = GetFileType(options.InputPath);

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
                if (options.InputType == FileType.EncryptedBhd)
                {
                    options.OutputPath += "_decrypted.bhd";
                }
            }

            return options;
        }

        internal static FileType GetFileType(string path)
        {
            if (path.EndsWith("enc_regulation.bnd.dcx", StringComparison.InvariantCultureIgnoreCase))
            {
                return FileType.Regulation;
            }

            if (path.EndsWith("dcx", StringComparison.InvariantCultureIgnoreCase))
            {
                return FileType.Dcx;
            }

            if (path.EndsWith("Ebl.bdt", StringComparison.InvariantCultureIgnoreCase))
            {
                return FileType.EncryptedBdt;
            }

            if (path.EndsWith("Ebl.bhd"))
            {
                return FileType.EncryptedBhd;
            }

            if (path.EndsWith("bdt", StringComparison.InvariantCultureIgnoreCase))
            {
                return FileType.Bdt;
            }

            if (path.EndsWith("bnd", StringComparison.InvariantCultureIgnoreCase))
            {
                return FileType.Bnd;
            }

            if (path.EndsWith("sl2", StringComparison.CurrentCultureIgnoreCase))
            {
                return FileType.Savegame;
            }

            return FileType.Unknown;
        }
    }
}