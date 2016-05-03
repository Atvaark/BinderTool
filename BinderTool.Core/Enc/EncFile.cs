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

        private const int EncryptionIvSize = 16;

        private readonly byte[] _key;

        private readonly byte[] _iv = new byte[EncryptionIvSize];

        public MemoryStream Data { get; private set; }

        public static EncFile ReadEncFile(Stream inputStream, byte[] key)
        {
            EncFile encFile = new EncFile(key);
            encFile.Read(inputStream);
            return encFile;
        }

        private void Read(Stream inputStream)
        {
            BigEndianBinaryReader reader = new BigEndianBinaryReader(inputStream, Encoding.ASCII, true);
            reader.Read(_iv, 0, EncryptionIvSize);
            byte[] encryptedData = new byte[inputStream.Length - EncryptionIvSize];
            reader.Read(encryptedData, 0, encryptedData.Length);
            Data = CryptographyUtility.DecryptAesCbc(new MemoryStream(encryptedData), _key, _iv);
        }
    }
}
