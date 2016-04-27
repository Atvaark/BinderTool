using System.IO;

namespace BinderTool.Core.Bhd5
{
    public class Bhd5Range
    {
        public long StartOffset { get; set; }
        public long EndOffset { get; set; }

        public static Bhd5Range Read(BinaryReader reader)
        {
            Bhd5Range result = new Bhd5Range();
            result.StartOffset = reader.ReadInt64();
            result.EndOffset = reader.ReadInt64();
            return result;
        }
    }
}