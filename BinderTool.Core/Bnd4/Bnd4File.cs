using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace BinderTool.Core.Bnd4
{
    public class Bnd4File
    {
        private const string Bnd4Signature = "BND4";
        private readonly List<Bnd4FileEntry> _entries;

        public Bnd4File()
        {
            _entries = new List<Bnd4FileEntry>();
        }

        public List<Bnd4FileEntry> Entries => _entries;

        public static Bnd4File ReadBnd4File(Stream inputStream)
        {
            Bnd4File bnd4File = new Bnd4File();
            bnd4File.Read(inputStream);
            return bnd4File;
        }

        private void Read(Stream inputStream)
        {
            BinaryReader reader = new BinaryReader(inputStream, Encoding.ASCII, true);
            string signature = reader.ReadString(4);
            if (signature != Bnd4Signature)
                throw new Exception("Unknown signature");
            reader.Skip(8);
            int fileCount = reader.ReadInt32();
            reader.Skip(8);
            string version = reader.ReadString(8);
            int directoryEntrySize = reader.ReadInt32();
            reader.Skip(4);
            int dataOffset = reader.ReadInt32();
            reader.Skip(4);
            byte encoding = reader.ReadByte();
            reader.Skip(15);

            switch (encoding)
            {
                case 0:
                    break;
                case 1:
                    reader = new BinaryReader(inputStream, Encoding.Unicode, true);
                    break;
                default:
                    Debug.WriteLine("Unknown encoding " + encoding);
                    break;
            }

            // Directory section
            for (int i = 0; i < fileCount; i++)
            {
                int fileEntryOffset;
                int fileNameOffset;

                reader.Skip(8);
                int fileEntrySize = reader.ReadInt32();
                reader.Skip(4);
                if (directoryEntrySize == 36)
                {
                    reader.Skip(8);
                    fileEntryOffset = reader.ReadInt32();
                    reader.Skip(4);
                    fileNameOffset = reader.ReadInt32();
                }
                else
                {
                    fileEntryOffset = reader.ReadInt32();
                    fileNameOffset = reader.ReadInt32();
                }
                
                long position = reader.GetPosition();
                string fileName = "";
                if (fileNameOffset > 0)
                {
                    reader.Seek(fileNameOffset);
                    fileName = reader.ReadNullTerminatedString();
                }

                if (fileName.Length > 0 && fileEntrySize > 0 && fileEntryOffset > 0)
                {
                    reader.Seek(fileEntryOffset);
                    _entries.Add(Bnd4FileEntry.Read(inputStream, fileEntrySize, fileName));
                }
                reader.Seek(position);
            }
        }
    }
}
