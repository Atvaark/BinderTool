using System;
using System.IO;
using System.Text;

namespace BinderTool.Core.Sl2
{
    public class Sl2UserData
    {
        private const int UserDataIvSize = 16;

        private readonly byte[] _key;

        private readonly byte[] _iv;

        private readonly bool _encrypted;

        public Sl2UserData(byte[] key)
        {
            _key = key;
            _iv = new byte[UserDataIvSize];
            _encrypted = Equals(key, new byte[key.Length]) ? true : false;
        }

        public string Name { get; set; }

        public byte[] EncryptedUserData { get; private set; }

        public byte[] DecryptedUserData()
        {
            if (!_encrypted)
                return EncryptedUserData;

            MemoryStream ms = new MemoryStream();
            CryptographyUtility.DecryptAesCbc(new MemoryStream(EncryptedUserData), _key, _iv).CopyTo(ms);
            return ms.ToArray();
        }

        public static Sl2UserData ReadSl2UserData(Stream inputStream, byte[] key, int size, string name)
        {
            Sl2UserData sl2UserData = new Sl2UserData(key);
            sl2UserData.Name = name;
            sl2UserData.Read(inputStream, size);
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
