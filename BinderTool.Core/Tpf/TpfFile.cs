using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace BinderTool.Core.Tpf
{
    public class TpfFile
    {
        public List<TpfFileEntry> Entries { get; private set; }

        public static TpfFile OpenTpfFile(Stream inputStream)
        {
            TpfFile tpfFile = new TpfFile();
            tpfFile.Read(inputStream);
            return tpfFile;
        }

        private void Read(Stream inputStream)
        {
            BinaryReader reader = new BinaryReader(inputStream, Encoding.ASCII, true);
            string signature = reader.ReadString(4);
            int sizeSum = reader.ReadInt32();
            int entryCount = reader.ReadInt32();

            byte versionFlag = reader.ReadByte(); // 00 DS / 02 DeS / 04 BB
            byte flag2 = reader.ReadByte(); // 03
            byte encoding = reader.ReadByte();
            byte flag3 = reader.ReadByte(); // 00

            switch (encoding)
            {
                case 1: // Unicode
                    reader = new BinaryReader(inputStream, Encoding.Unicode, true);
                    break;
                case 2: // ASCII
                    break;
                default:
                    reader = new BinaryReader(inputStream, Encoding.Unicode, true);
                    Debug.WriteLine($"Unknown encoding {encoding}");
                    break;
            }

            //  TODO: Verify that versionFlag is actually different between DS and BB
            GameVersion gameVersion = versionFlag == 0x04
                ? GameVersion.Bloodborne
                : GameVersion.Common;

            List<TpfFileEntry> entries = new List<TpfFileEntry>(entryCount);
            for (int i = 0; i < entryCount; i++)
            {
                entries.Add(TpfFileEntry.Read(reader, gameVersion));
            }

            Entries = entries;
        }
    }
}