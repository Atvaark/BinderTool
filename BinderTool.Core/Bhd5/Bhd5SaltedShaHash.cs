using System.IO;
using System.Text;

namespace BinderTool.Core.Bhd5
{
    public class Bhd5SaltedShaHash
    {
        public byte[] Hash { get; set; }

        public static Bhd5SaltedShaHash Read(Stream inputStream)
        {
            Bhd5SaltedShaHash result = new Bhd5SaltedShaHash();
            BinaryReader reader = new BinaryReader(inputStream, Encoding.UTF8, true);
            result.Hash = reader.ReadBytes(32);
            uint unknown1 = reader.ReadUInt32();
            uint unknown2 = reader.ReadUInt32();
            uint unknown3 = reader.ReadUInt32();
            uint unknown4 = reader.ReadUInt32();
            uint unknown5 = reader.ReadUInt32();

            return result;
        }
    }
}
