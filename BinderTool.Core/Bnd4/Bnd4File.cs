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

        public List<Bnd4FileEntry> Entries
        {
            get { return _entries; }
        }

        public static Bnd4File ReadBnd4File(Stream inputStream)
        {
            Bnd4File bnd4File = new Bnd4File();
            bnd4File.Read(inputStream);
            return bnd4File;
        }

        private void Read(Stream inputStream)
        {
            BinaryReader reader = new BinaryReader(inputStream, Encoding.UTF8, true);
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
            reader.Skip(20); // encoding could also be here as a flag

            // Directory section
            for (int i = 0; i < fileCount; i++)
            {
                int fileEntryOffset;
                int fileNameOffset;

                int encoding = reader.ReadInt32();
                reader.Skip(4);
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

                // TODO: Check encoding ids are correct. 
                // DSII (192 = unicode , 64 = ascii?)
                // DSIII (64 = unicode , 192 = ascii?)
                switch (encoding)
                {
                    case 64:
                        reader = new BinaryReader(inputStream, Encoding.Unicode, true);
                        break;
                    case 192:
                        break;
                    default:
                        Debug.WriteLine("Unknown encoding " + encoding);
                            break;
                }
                
                long position = reader.Position();
                string fileName = "";
                if (fileNameOffset > 0)
                {
                    reader.Seek(fileNameOffset);
                    fileName = reader.ReadNullTerminatedString();
                }
                reader.Seek(fileEntryOffset);
                _entries.Add(Bnd4FileEntry.Read(inputStream, fileEntrySize, fileName));
                reader.Seek(position);
            }
        }
    }
}
