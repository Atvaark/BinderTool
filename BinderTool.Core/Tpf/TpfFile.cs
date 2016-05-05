using System.Collections.Generic;
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
            int flags = reader.ReadInt32(); // 00 03 01 00

            reader = new BinaryReader(inputStream, Encoding.Unicode, true);
            List<TpfFileEntry> entries = new List<TpfFileEntry>(entryCount);
            for (int i = 0; i < entryCount; i++)
            {
                entries.Add(TpfFileEntry.Read(reader));
            }

            Entries = entries;
        }
    }
}