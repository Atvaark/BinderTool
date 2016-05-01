using System.Collections.Generic;
using System.IO;
using System.Text;
using BinderTool.Core.IO;

namespace BinderTool.Core.Bhf4
{
    public class Bhf4File
    {
        //typedef struct {
        //    char signature[4]; // BHF4
        //    int unknown;  // Always 00 00 00 00?
        //    int unknown;  // Always 00 00 01 00?
        //    int numberFiles;
        //    int unknown;
        //    int unknown;  // 64?
        //    char version[8]; // 00010810?
        //    int directoryEntrySize;  // 36?
        //    int unknown;  
        //    int offsetDataSection;
        //    int unknown;
        //    int unknown; // 00 54 00 00?
        //    int unknown; 
        //    int unknown;
        //    int unknown;
        //} BNDHeader;

        //typedef struct {
        //    int unknown1; // Always 64?
        //    int unknown2; // Always -1?
        //    int fileSize;
        //    int unknown4; // Always 0
        //    int fileOffset;
        //    int fileNameOffset;
        //    int unknown;
        //    int unknown;
        //    int unknown;
        //} BNDDirectoryEntry;

        //typedef struct {
        //    string fileName;
        //} BNDFileNameEntry <optimize=false>;

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
            int encoding = reader.ReadInt32(); // Always 64? Encoding?
            int unknown4 = reader.ReadInt32();
            Version = reader.ReadString(8);
            int directoryEntrySize = reader.ReadInt32(); // Always 36?
            int unknown5 = reader.ReadInt32();
            int unknown6 = reader.ReadInt32();
            int unknown7 = reader.ReadInt32();
            int unknown8 = reader.ReadInt32();
            int unknown9 = reader.ReadInt32();
            int unknown10 = reader.ReadInt32(); // Always 00 74 00 00?
            int unknown11 = reader.ReadInt32();

            for (int i = 0; i < numberFiles; i++)
            {
                Entries.Add(Bhf4Entry.ReadBhf4Entry(inputStream));
            }

            long endPosition = inputStream.Position;

            // TODO: Verify if this value actually represents the encoding
            switch (encoding)
            {
                case 64:
                    reader = new BigEndianBinaryReader(inputStream, Encoding.Unicode, true);
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
