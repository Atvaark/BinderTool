using System.IO;

namespace BinderTool.Core.Bhd5
{
    public class Bhd5AesKey
    {
        public byte[] Key { get; private set; }
        public Bhd5Range[] Ranges { get; private set; }

        public static Bhd5AesKey Read(BinaryReader reader)
        {
            Bhd5AesKey result = new Bhd5AesKey();

            result.Key = reader.ReadBytes(16);
            int rangeCount = reader.ReadInt32();
            Bhd5Range[] ranges = new Bhd5Range[rangeCount];
            for (int i = 0; i < rangeCount; i++)
            {
                ranges[i] = Bhd5Range.Read(reader);
            }
            result.Ranges = ranges;

            return result;
        }
    }
}
