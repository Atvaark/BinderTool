using System;
using System.IO;
using System.Text;

namespace BinderTool.Core.Sl2
{
    public class Sl2UserData
    {
        private const int UserDataIvSize = 16;

        private static readonly byte[] UserDataKey =
        {
            0xB7, 0xFD, 0x46, 0x3E, 0x4A, 0x9C, 0x11, 0x02,
            0xDF, 0x17, 0x39, 0xE5, 0xF3, 0xB2, 0xA5, 0x0F
        };

        private readonly byte[] _iv;

        public Sl2UserData()
        {
            _iv = new byte[UserDataIvSize];
        }

        public string UserDataName { get; set; }
        public byte[] EncryptedUserData { get; private set; }

        public byte[] DecryptedUserData
        {
            get
            {
                // TODO: Check if the first 128bit are a hash
                return CryptographyUtility.DecryptAesCbc(new MemoryStream(EncryptedUserData), UserDataKey, _iv).ToArray();
            }
        }

        public void Write(Stream outputStream)
        {
            BinaryWriter writer = new BinaryWriter(outputStream, Encoding.ASCII, true);
            // TODO: Implement Sl2UserData.Write
            throw new NotImplementedException();
        }

        public static Sl2UserData ReadSl2UserData(Stream inputStream, int userDataSize, string userDataName)
        {
            Sl2UserData sl2UserData = new Sl2UserData();
            sl2UserData.UserDataName = userDataName;
            sl2UserData.Read(inputStream, userDataSize);
            return sl2UserData;
        }

        private void Read(Stream inputStream, int userDataSize)
        {
            BinaryReader reader = new BinaryReader(inputStream, Encoding.ASCII, true);
            reader.Read(_iv, 0, UserDataIvSize);
            EncryptedUserData = reader.ReadBytes(userDataSize - UserDataIvSize);
        }
    }
}
