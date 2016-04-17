using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using BinderTool.Core.IO;

namespace BinderTool.Core.Dcx
{
    public class DcxFile
    {
        public const string DcxSignature = "DCX\0";
        public const int DcxSize = 76;

        private const int DcxHeaderSize = 24;
        private const string DcsSignature = "DCS\0";
        private const string DcpSignature = "DCP\0";
        private const string DcaSignature = "DCA\0";
        private const int DcaHeaderSize = 8;

        public DcxFile()
        {
        }

        private DcxFile(DcxCompression compression, byte[] compressedData)
        {
            Compression = compression;
            CompressedData = compressedData;
        }

        private DcxCompression Compression { get; set; }
        private int CompressedSize { get; set; }
        private int UncompressedSize { get; set; }
        public byte[] CompressedData { get; private set; }
        
        public static DcxFile Read(Stream inputStream)
        {
            DcxFile result = new DcxFile();
            BigEndianBinaryReader reader = new BigEndianBinaryReader(inputStream, Encoding.UTF8, true);
            result.ReadCommonHeader(reader);
            result.ReadCompressionHeader(reader);
            result.CompressedData = reader.ReadBytes(result.CompressedSize);
            return result;
        }

        public static int ReadCompressedSize(Stream inputStream)
        {
            DcxFile result = new DcxFile();
            BigEndianBinaryReader reader = new BigEndianBinaryReader(inputStream, Encoding.UTF8, true);
            result.ReadCommonHeader(reader);
            return result.CompressedSize;
        }

        private void ReadCommonHeader(BinaryReader reader)
        {
            string signature = reader.ReadString(4);
            if (signature != DcxSignature)
                throw new Exception("Signature was not DCX");
            reader.Skip(4);
            int dcxHeaderSize = reader.ReadInt32();
            if (dcxHeaderSize != DcxHeaderSize)
                throw new Exception("Unsupported DCX header size.");
            reader.Skip(12);

            signature = reader.ReadString(4);
            if (signature != DcsSignature)
                throw new Exception("Signature was not DCS");
            this.UncompressedSize = reader.ReadInt32();
            this.CompressedSize = reader.ReadInt32();
        }

        private void ReadCompressionHeader(BinaryReader reader)
        {
            string signature = reader.ReadString(4);
            if (signature != DcpSignature)
                throw new Exception("Signature was not DCP");
            signature = reader.ReadString(4);
            if (signature != DeflateCompression.DeflateSignature)
                throw new NotImplementedException(String.Format("Compression not implemented ({0}) ", signature));
            this.Compression = DeflateCompression.Read(reader);

            signature = reader.ReadString(4);
            if (signature != DcaSignature)
                throw new Exception("Signature was not DCA");
            int dcaHeaderSize = reader.ReadInt32();
            if (dcaHeaderSize != DcaHeaderSize)
                throw new Exception("Unsupported DCA header size.");
        }

        public byte[] Decompress()
        {
            return Compression.DecompressData(CompressedData).ToArray();
        }
    }
}
