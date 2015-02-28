using System;
using System.IO;
using System.Text;
using BinderTool.Core.IO;


namespace BinderTool.Core.Regulation
{
    public class RegulationFile
    {
        private const string RegulationKeyStr = "40178130DF0A94543309E171ECBF254C";
        private static byte[] RegulationKey = new byte[16];
        private static byte[] RegulationIV = new byte[16];
        private const int RegulationKeyLength = 16;
        private const int RegulationHeaderSize = 32;

        private int EncryptedSize { get; set; }
        private int DecryptedSize { get; set; }
        public byte[] EncryptedData { get; private set; }
        public byte[] DecryptedData
        {
            get {
                
                Stream stream = new MemoryStream(EncryptedData);
                return CryptographyUtility.DecryptAesCtrDsII(stream, RegulationKey, RegulationIV).ToArray(); 
            }
        }

        public RegulationFile()
        {
        }

        private RegulationFile(byte[] encryptedData)
        {
            EncryptedData = encryptedData;
        }

        public static RegulationFile DecryptRegulationFile(Stream inputStream)
        {
            RegulationFile result = new RegulationFile();
            BigEndianBinaryReader reader = new BigEndianBinaryReader(inputStream, Encoding.UTF8, true);

            for (int i = 0; i < RegulationKeyLength * 2; i += 2)
            {
                RegulationKey[i / 2] = Convert.ToByte(RegulationKeyStr.Substring(i, 2), 16);
            }

            RegulationIV = reader.ReadBytes(16);
            for (int i = 11; i != 0; i--)
            {
                RegulationIV[i] = RegulationIV[i - 1];
            }

            RegulationIV[00] = 0x80;
            RegulationIV[12] = 0x00;
            RegulationIV[13] = 0x00;
            RegulationIV[14] = 0x00;
            RegulationIV[15] = 0x01;

            result.EncryptedSize = (int)inputStream.Length - RegulationHeaderSize;
            result.DecryptedSize = result.EncryptedSize;

            reader.Seek(RegulationHeaderSize, SeekOrigin.Begin);
            result.EncryptedData = reader.ReadBytes(result.EncryptedSize);           

            return result;
        }
    }
}
