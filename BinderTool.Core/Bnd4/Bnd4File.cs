using System;
using System.Collections.Generic;
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

        public static Bnd4File Read(Stream inputStream)
        {
            Bnd4File result = new Bnd4File();
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
            reader.Skip(20);


            //TODO: Create a new reader object if the text encoding was guessed incorrectly


            // Directory section
            for (int i = 0; i < fileCount; i++)
            {
                int fileEntrySize;
                int fileEntryOffset;
                int fileNameOffset;
                if (directoryEntrySize == 36)
                {
                    reader.Skip(8);
                    fileEntrySize = reader.ReadInt32();
                    reader.Skip(12);
                    fileEntryOffset = reader.ReadInt32();
                    reader.Skip(4);
                    fileNameOffset = reader.ReadInt32();
                }
                else
                {
                    reader.Skip(8);
                    fileEntrySize = reader.ReadInt32();
                    reader.Skip(4);

                    fileEntryOffset = reader.ReadInt32();
                    fileNameOffset = reader.ReadInt32();
                }


                long position = reader.Position();
                string fileName = "";
                if (fileNameOffset > 0)
                {
                    reader.Seek(fileNameOffset);
                    fileName = reader.ReadNullTerminatedString();
                }
                reader.Seek(fileEntryOffset);
                result._entries.Add(Bnd4FileEntry.Read(inputStream, fileEntrySize, fileName));
                reader.Seek(position);
            }
            return result;
        }
    }
}
