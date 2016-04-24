using System;
using System.IO;
using System.Text;

namespace BinderTool.Core.Bhd5
{
    public class Bhd5SaltedShaHash
    {
        public byte[] Hash { get; set; }

        public static Bhd5SaltedShaHash Read(BinaryReader reader)
        {
            Bhd5SaltedShaHash result = new Bhd5SaltedShaHash();
            result.Hash = reader.ReadBytes(32);

            int version = reader.ReadInt32();

            if (version != 1 && version != 3)
            {
                throw new FormatException();
            }

            int unknown1 = reader.ReadInt32();
            int unknown2 = reader.ReadInt32();
            int unknown3 = reader.ReadInt32();
            int unknown4 = reader.ReadInt32();

            if (version == 3)
            {
                int unknown5 = reader.ReadInt32();
                int unknown6 = reader.ReadInt32();
                int unknown7 = reader.ReadInt32();
                int unknown8 = reader.ReadInt32();

                int unknown9 = reader.ReadInt32();
                int unknown10 = reader.ReadInt32();
                int unknown11 = reader.ReadInt32();
                int unknown12 = reader.ReadInt32();
            }

            return result;
        }
    }
}
