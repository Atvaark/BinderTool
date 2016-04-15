using System;
using System.IO;
using System.Text;
using BinderTool.Core.IO;

namespace BinderTool.Core.Dcx
{
    public class DcxFile
    {
        private const string DcxSignature = "DCX\0";
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
            result.UncompressedSize = reader.ReadInt32();
            result.CompressedSize = reader.ReadInt32();
            signature = reader.ReadString(4);
            if (signature != DcpSignature)
                throw new Exception("Signature was not DCP");
            signature = reader.ReadString(4);
            if (signature != DeflateCompression.DeflateSignature)
                throw new NotImplementedException(String.Format("Compression not implemented ({0}) ", signature));
            result.Compression = DeflateCompression.Read(inputStream);

            signature = reader.ReadString(4);
            if (signature != DcaSignature)
                throw new Exception("Signature was not DCA");
            int dcaHeaderSize = reader.ReadInt32();
            if (dcaHeaderSize != DcaHeaderSize)
                throw new Exception("Unsupported DCA header size.");

            result.CompressedData = reader.ReadBytes(result.CompressedSize);

            return result;
        }

        public byte[] Decompress()
        {
            return Compression.DecompressData(CompressedData).ToArray();
        }
    }
}
