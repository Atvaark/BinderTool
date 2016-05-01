using System.IO;
using System.Text;
using BinderTool.Core.IO;

namespace BinderTool.Core.Regulation
{
    public class RegulationFile
    {
        private const int RegulationHeaderSize = 16;
       
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public RegulationFile(byte[] key)
        {
            _key = key;
            _iv = new byte[16];
        }

        public byte[] EncryptedData { get; private set; }
        
        public static RegulationFile ReadRegulationFile(Stream inputStream, byte[] key)
        {
            RegulationFile regulationFile = new RegulationFile(key);
            regulationFile.Read(inputStream);
            return regulationFile;
        }

        public byte[] DecryptData()
        {
            return CryptographyUtility.DecryptAesCbc(new MemoryStream(EncryptedData), _key, _iv).ToArray();
        }

        private void Read(Stream inputStream)
        {
            BigEndianBinaryReader reader = new BigEndianBinaryReader(inputStream, Encoding.UTF8, true);
            inputStream.Seek(RegulationHeaderSize, SeekOrigin.Begin);
            EncryptedData = reader.ReadBytes((int)inputStream.Length - RegulationHeaderSize);
        }
    }
}
