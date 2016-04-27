using System.IO;
using System.Text;
using BinderTool.Core.IO;

namespace BinderTool.Core.Regulation
{
    public class RegulationFile
    {
        private const int RegulationHeaderSize = 32;

        // TODO: Find out where the regulation key is in DSIII.
        private static readonly byte[] RegulationFileKey =
        {
        };

        private readonly byte[] _iv;

        public RegulationFile()
        {
            _iv = new byte[16];
        }

        public byte[] EncryptedData { get; private set; }

        public byte[] DecryptedData => CryptographyUtility.DecryptAesCtr(new MemoryStream(EncryptedData), RegulationFileKey, _iv).ToArray();

        public static RegulationFile ReadRegulationFile(Stream inputStream)
        {
            RegulationFile regulationFile = new RegulationFile();
            regulationFile.Read(inputStream);
            return regulationFile;
        }

        private void Read(Stream inputStream)
        {
            BigEndianBinaryReader reader = new BigEndianBinaryReader(inputStream, Encoding.UTF8, true);

            _iv[00] = 0x80;
            for (int i = 1; i <= 11; i++)
            {
                _iv[i] = reader.ReadByte();
            }
            _iv[12] = 0x00;
            _iv[13] = 0x00;
            _iv[14] = 0x00;
            _iv[15] = 0x01;

            inputStream.Seek(RegulationHeaderSize, SeekOrigin.Begin);
            EncryptedData = reader.ReadBytes((int) inputStream.Length - RegulationHeaderSize);
        }
    }
}
