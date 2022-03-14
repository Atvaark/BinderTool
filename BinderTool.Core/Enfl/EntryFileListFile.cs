using System.Diagnostics;
using System.IO;
using System.Text;
using Ionic.Zlib;

namespace BinderTool.Core.Enfl
{
    public class EntryFileListFile
    {
        public class EntryFileListEntry1
        {
            public int Unknown1 { get; set; }

            public int Unknown2 { get; set; }

            public static EntryFileListEntry1 ReadEntryFileListEntry1(BinaryReader reader)
            {
                EntryFileListEntry1 entry1 = new EntryFileListEntry1();
                entry1.Unknown1 = reader.ReadInt16();
                entry1.Unknown2 = reader.ReadInt16();
                return entry1;
            }
        }

        public class EntryFileListEntry2
        {
            public uint Unknown1 { get; set; }

            public int Unknown2 { get; set; }

            public string EntryFileName { get; set; }

            public static EntryFileListEntry2 ReadEntryFileListEntry2(BinaryReader reader)
            {
                EntryFileListEntry2 entry = new EntryFileListEntry2();
                entry.Unknown1 = reader.ReadUInt32();
                entry.Unknown2 = reader.ReadInt32();
                return entry;
            }
        }

        public EntryFileListEntry1[] array1;
        public EntryFileListEntry2[] array2;


        public static EntryFileListFile ReadEntryFileListFile(Stream inputStream)
        {
            EntryFileListFile entryFileListFile = new EntryFileListFile();
            entryFileListFile.Read(inputStream);
            return entryFileListFile;
        }

        private void Read(Stream inputStream)
        {
            MemoryStream dataStream = Decompress(inputStream);
            BinaryReader reader = new BinaryReader(dataStream, Encoding.Unicode, true);

            int unknown1 = reader.ReadInt32(); // 0
            int count1 = reader.ReadInt32();
            int count2 = reader.ReadInt32();
            int unknown2 = reader.ReadInt32(); // 0

            array1 = new EntryFileListEntry1[count1+1];
            array1[0] = new EntryFileListEntry1();
            array1[0].Unknown1 = unknown1;
            array1[0].Unknown2 = unknown2;
            for (int i = 0; i < count1; i++)
            {
                array1[i+1] = EntryFileListEntry1.ReadEntryFileListEntry1(reader);
            }

            reader.Align(16);

            array2 = new EntryFileListEntry2[count2];
            for (int i = 0; i < count2; i++)
            {
                array2[i] = EntryFileListEntry2.ReadEntryFileListEntry2(reader);
            }

            reader.Align(16);

            short padding = reader.ReadInt16(); // Might as well be an empty string.

            for (int i = 0; i < count2; i++)
            {
                array2[i].EntryFileName = reader.ReadNullTerminatedString();
            }
        }

        private MemoryStream Decompress(Stream inputStream)
        {
            BinaryReader reader = new BinaryReader(inputStream, Encoding.ASCII, true);
            string signature = reader.ReadString(4);
            int version = reader.ReadInt32();
            int compressedSize = reader.ReadInt32();
            int uncompressedSize = reader.ReadInt32();

            byte[] data = reader.ReadBytes(compressedSize);
            byte[] uncompressData = ZlibStream.UncompressBuffer(data);
            return new MemoryStream(uncompressData);
        }
    }
}
