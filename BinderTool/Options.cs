using System;
using System.IO;
using System.Text.RegularExpressions;

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
                if (options.InputType == FileType.EncryptedBhd)
                {
                    options.OutputPath += "_decrypted.bhd";
                }
            }

            return options;
        }

        private static FileType GetFileType(string fileName)
        {
            if (fileName == null) throw new ArgumentNullException("fileName");

            if (fileName.EndsWith("dcx", StringComparison.InvariantCultureIgnoreCase))
            {
                return FileType.Dcx;
            }

            if (fileName.EndsWith("bnd", StringComparison.InvariantCultureIgnoreCase))
            {
                return FileType.Bnd;
            }

            if (fileName.EndsWith("sl2", StringComparison.CurrentCultureIgnoreCase))
            {
                return FileType.Savegame;
            }
            
            if (fileName == @"Data0.bdt")
            {
                return FileType.Regulation;
            }

            if (Regex.IsMatch(fileName, @"Data\d\.bdt"))
            {
                return FileType.EncryptedBdt;
            }

            if (Regex.IsMatch(fileName, @"Data\d\.bhd"))
            {
                return FileType.EncryptedBhd;
            }

            if (fileName.EndsWith("bdt", StringComparison.InvariantCultureIgnoreCase))
            {
                return FileType.Bdt;
            }
            
            return FileType.Unknown;
        }
    }
}