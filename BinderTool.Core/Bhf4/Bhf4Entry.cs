using System.IO;
using System.Text;

namespace BinderTool.Core.Bhf4
{
    public class Bhf4Entry
    {
        public string FileName { get; set; }
        public int FileNameOffset { get; set; }
        public int FileOffset { get; set; }
        public int FileSize { get; set; }

        public static Bhf4Entry ReadBhf4Entry(Stream inputStream)
        {
            Bhf4Entry bhf4Entry = new Bhf4Entry();
            bhf4Entry.Read(inputStream);
            return bhf4Entry;
        }

        public void Read(Stream inputStream)
        {
            BinaryReader reader = new BinaryReader(inputStream, Encoding.ASCII, true);
            int unknown1 = reader.ReadInt32(); // Always 64? Endianess? Encoding?
            uint unknown2 = reader.ReadUInt32(); // Always -1?
            FileSize = reader.ReadInt32();
            int unknown4 = reader.ReadInt32();
            int fileSize2 = reader.ReadInt32();
            int unknown6 = reader.ReadInt32();
            FileOffset = reader.ReadInt32();
            int unknown8 = reader.ReadInt32();
            FileNameOffset = reader.ReadInt32();
        }
    }
}
