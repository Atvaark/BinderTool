using System.IO;

namespace BinderTool.Core.Param
{
    public class ParamEntry
    {
        public long Id { get; set; }
        public long Offset { get; set; }
        public long Size { get; set; }
        public byte[] Data { get; set; }

        public void Read(BinaryReader reader)
        {
            Id = reader.ReadInt64();
            Offset = reader.ReadInt64();
            Size = reader.ReadInt64();
        }
    }
}