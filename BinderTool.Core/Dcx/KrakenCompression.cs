using System.IO;

namespace BinderTool.Core.Dcx
{
    public class KrakenCompression : DcxCompression
    {
        public const string KrakenSignature = "KRAK";

        public override MemoryStream CompressData(byte[] uncompressedData)
        {
            // TODO: Call oo2core_6_win64.dll.OodleLZ_Compress
            return new MemoryStream(uncompressedData);
        }

        public override MemoryStream DecompressData(byte[] compressedData)
        {
            // TODO: oo2core_6_win64.dll.OodleLZ_Compress
            // 8 byte header
            // uncompressed header + magic number of content
            
            if (compressedData.Length >= 8)
            {
                return new MemoryStream(compressedData, 8, compressedData.Length - 8);
            }

            return new MemoryStream(compressedData);
        }

        public static KrakenCompression Read(BinaryReader reader)
        {
            KrakenCompression result = new KrakenCompression();
            //00 00 00 20  32 BE
            //06 00 00 00   6 LE
            //00 00 00 00
            //00 00 00 00
            //00 00 00 00
            //00 01 01 00 65792
            reader.Skip(24);
            return result;
        }
    }
}