using System.IO;
using System.Text;

namespace BinderTool.Core.Param
{
    public class ParamFile
    {
        public string StructName { get; set; }
        public int Version { get; set; }
        public int EntrySize { get; set; }
        public ParamEntry[] Entries { get; set; }
        public short Type1 { get; set; }
        public short Type2 { get; set; }

        public static ParamFile ReadParamFile(Stream inputStream)
        {
            ParamFile paramFile = new ParamFile();
            paramFile.Read(inputStream);
            return paramFile;
        }

        public void Read(Stream inputStream)
        {
            BinaryReader reader = new BinaryReader(inputStream, Encoding.ASCII, true);
            int fileSize = reader.ReadInt32();
            short unknown1 = reader.ReadInt16(); // 0
            short type1 = reader.ReadInt16(); // 0-2
            short type2 = reader.ReadInt16(); // 0-10
            short count = reader.ReadInt16();
            int unknown4 = reader.ReadInt32();
            int fileSize2 = reader.ReadInt32();
            int unknown6 = reader.ReadInt32();
            int unknown7 = reader.ReadInt32();
            int unknown8 = reader.ReadInt32();
            int unknown9 = reader.ReadInt32();
            int unknown10 = reader.ReadInt32();
            int unknown13 = reader.ReadInt32();
            int version = reader.ReadInt32();
            int dataOffset = reader.ReadInt32();
            int unknown14 = reader.ReadInt32();
            int unknown15 = reader.ReadInt32();
            int unknown16 = reader.ReadInt32();

            ParamEntry[] entries = new ParamEntry[count];
            for (int i = 0; i < count; i++)
            {
                entries[i] = new ParamEntry();
                entries[i].Read(reader);
            }

            const int headerSize = 64;
            const int dictionarySize = 24;

            int entrySize = (fileSize - headerSize - count * dictionarySize) / count;
            for (int i = 0; i < count; i++)
            {
                entries[i].Data = reader.ReadBytes(entrySize);
            }

            string paramName = reader.ReadNullTerminatedString();

            StructName = paramName;
            Version = version;
            Type1 = type1;
            Type2 = type2;
            EntrySize = entrySize;
            Entries = entries;
        }
    }
}