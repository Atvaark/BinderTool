using System.IO;
using System.Text;

namespace BinderTool.Core.Bhd5
{
    public class Bhd5AesKey
    {
        public byte[] Key { get; private set; }

        public static Bhd5AesKey Read(Stream inputStream)
        {
            Bhd5AesKey result = new Bhd5AesKey();
            BinaryReader reader = new BinaryReader(inputStream, Encoding.UTF8, true);

            result.Key = reader.ReadBytes(16);
            uint unknown1 = reader.ReadUInt32();
            uint unknown2 = reader.ReadUInt32();
            uint unknown3 = reader.ReadUInt32();
            uint unknown4 = reader.ReadUInt32();
            uint unknown5 = reader.ReadUInt32();

            return result;
        }
    }
}
