using System.IO;

namespace BinderTool.Core.Bhd5
{
    public class Bhd5SaltedShaHash
    {
        public byte[] Hash { get; set; }
        public Bhd5Range[] Ranges { get; private set; }

        public static Bhd5SaltedShaHash Read(BinaryReader reader)
        {
            Bhd5SaltedShaHash result = new Bhd5SaltedShaHash();

            result.Hash = reader.ReadBytes(32);
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
