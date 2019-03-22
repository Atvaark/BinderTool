using System.IO;
using BinderTool.Core.Kraken;

namespace BinderTool.Core.Dcx
{
    public class KrakenCompression : DcxCompression
    {
        public const string KrakenSignature = "KRAK";

        //public override MemoryStream CompressData(byte[] uncompressedData)
        //{
        //    var compressedData = NativeOodleKraken.Compress(
        //        uncompressedData,
        //        uncompressedData.Length,
        //        NativeOodleKraken.OodleLZCompressor.Kraken,
        //        NativeOodleKraken.OodleLZCompressionLevel.Normal);
        //    return new MemoryStream(compressedData);
        //}

        public override MemoryStream DecompressData(byte[] compressedData, int uncompressedSize)
        {
            byte[] decompressedData = NativeOodleKraken.Decompress(compressedData, compressedData.Length, uncompressedSize);
            return new MemoryStream(decompressedData);
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