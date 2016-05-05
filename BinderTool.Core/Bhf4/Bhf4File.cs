using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace BinderTool.Core.Bhf4
{
    public class Bhf4File
    {
        public Bhf4File()
        {
            Entries = new List<Bhf4Entry>();
        }

        public List<Bhf4Entry> Entries { get; private set; }

        public string Version { get; private set; }

        public static Bhf4File OpenBhf4File(string bhf4FilePath)
        {
            using (FileStream inputStream = new FileStream(bhf4FilePath, FileMode.Open))
            {
                Bhf4File bhf4File = new Bhf4File();
                bhf4File.Read(inputStream);
                return bhf4File;
            }
        }

        public void Read(Stream inputStream)
        {
            BinaryReader reader = new BinaryReader(inputStream, Encoding.ASCII, true);
            string signature = reader.ReadString(4); // BHF4
            int unknown1 = reader.ReadInt32(); // Always 00 00 00 00?
            int unknown2 = reader.ReadInt32(); // Always 00 00 01 00?
            int numberFiles = reader.ReadInt32();
            int unknown3 = reader.ReadInt32(); // Always 64?
            int unknown4 = reader.ReadInt32();
            Version = reader.ReadString(8);
            int directoryEntrySize = reader.ReadInt32(); // Always 36?
            int unknown5 = reader.ReadInt32();
            int unknown6 = reader.ReadInt32();
            int unknown7 = reader.ReadInt32();

            byte encoding = reader.ReadByte();
            byte unknown8 = reader.ReadByte();
            byte unknown9 = reader.ReadByte();
            byte unknown10 = reader.ReadByte();

            int unknown11 = reader.ReadInt32();
            int unknown12 = reader.ReadInt32();
            int unknown13 = reader.ReadInt32();

            for (int i = 0; i < numberFiles; i++)
            {
                Entries.Add(Bhf4Entry.ReadBhf4Entry(inputStream));
            }

            long endPosition = inputStream.Position;

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

            foreach (var entry in Entries)
            {
                inputStream.Position = entry.FileNameOffset;
                entry.FileName = reader.ReadNullTerminatedString();
            }

            inputStream.Position = endPosition;
        }

    }
}
