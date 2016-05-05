using System.IO;

namespace BinderTool.Core.Tpf
{
    public class TpfFileEntry
    {
        public byte Format { get; set; }

        public byte Type { get; set; }

        public byte MipMapCount { get; set; }

        public byte Flags { get; set; }

        public int Unknown { get; set; }

        public string FileName { get; set; }

        public byte[] Data { get; set; }

        public static TpfFileEntry Read(BinaryReader reader)
        {
            TpfFileEntry result = new TpfFileEntry();
            int dataOffset = reader.ReadInt32();
            int dataSize = reader.ReadInt32();

            //   0 = ?
            //   1 = ?
            //   5 = ?
            //   6 = ?
            //   9 = ?
            //  10 = ?
            // 100 = ?
            // 102 = ?
            // 103 = ?
            // 104 = ?
            // 105 = ?
            // 106 = ?
            // 107 = ?
            // 108 = ?
            // 109 = ?
            // 113 = ?
            result.Format = reader.ReadByte();
            result.Type = reader.ReadByte(); // 0 = texture, 1 = cubemap
            result.MipMapCount = reader.ReadByte();
            result.Flags = reader.ReadByte(); // 00

            int fileNameOffset = reader.ReadInt32();
            result.Unknown = reader.ReadInt32(); // 0 = ?, 1 = ?

            long position = reader.GetPosition();
            if (fileNameOffset > 0)
            {
                reader.Seek(fileNameOffset);
                result.FileName = reader.ReadNullTerminatedString();
                result.FileName += ".dds";
            }

            if (dataOffset > 0)
            {
                reader.Seek(dataOffset);
                result.Data = reader.ReadBytes(dataSize);
            }

            reader.Seek(position);

            return result;
        }

    }
}