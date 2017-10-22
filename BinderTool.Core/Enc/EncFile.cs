using System.IO;
using System.Text;
using BinderTool.Core.IO;

namespace BinderTool.Core.Enc
{
    public class EncFile
    {
        private EncFile(byte[] key)
        {
            _key = key;
        }

        private const int EncryptionIvCbcSize = 16;

        private const int EncryptionIvCtrSize = 32;
        
        private readonly byte[] _key;

        private readonly byte[] _iv = new byte[EncryptionIvCbcSize];

        public MemoryStream Data { get; private set; }

        public static EncFile ReadEncFile(Stream inputStream, byte[] key, GameVersion version = GameVersion.Common)
        {
            EncFile encFile = new EncFile(key);

            if (version == GameVersion.DarkSouls2)
            {
                encFile.ReadCtr(inputStream);
            }
            else
            {
                encFile.ReadCbc(inputStream);
            }

            return encFile;
        }

        private void ReadCbc(Stream inputStream)
        {
            BigEndianBinaryReader reader = new BigEndianBinaryReader(inputStream, Encoding.ASCII, true);
            reader.Read(_iv, 0, EncryptionIvCbcSize);
            byte[] encryptedData = new byte[inputStream.Length - EncryptionIvCbcSize];
            reader.Read(encryptedData, 0, encryptedData.Length);
            Data = CryptographyUtility.DecryptAesCbc(new MemoryStream(encryptedData), _key, _iv);
        }
        


        private void ReadCtr(Stream inputStream)
        {
            BigEndianBinaryReader reader = new BigEndianBinaryReader(inputStream, Encoding.ASCII, true);
            _iv[00] = 0x80;
            for (int i = 1; i <= 11; i++)
            {
                _iv[i] = reader.ReadByte();
            }
            _iv[12] = 0x00;
            _iv[13] = 0x00;
            _iv[14] = 0x00;
            _iv[15] = 0x01;
            inputStream.Seek(EncryptionIvCtrSize, SeekOrigin.Begin);
            byte[] encryptedData = reader.ReadBytes((int)inputStream.Length - EncryptionIvCtrSize);
            Data = CryptographyUtility.DecryptAesCtr(new MemoryStream(encryptedData), _key, _iv);
        }
    }
}
