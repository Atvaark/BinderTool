using System.IO;

namespace BinderTool.Core.Fmg
{
    public class FmgIdRange
    {

        public int OffsetIndex { get; set; }

        public int FirstId { get; set; }

        public int LastId { get; set; }

        public int Unknown { get; set; }

        public int IdCount => LastId - FirstId + 1;

        public void Read(BinaryReader reader)
        {
            OffsetIndex = reader.ReadInt32();
            FirstId = reader.ReadInt32();
            LastId = reader.ReadInt32();
            Unknown = reader.ReadInt32();
        }
    }
}