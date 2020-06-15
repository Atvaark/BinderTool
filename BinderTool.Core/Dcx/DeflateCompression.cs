using System.IO;
using Ionic.Zlib;

namespace BinderTool.Core.Dcx
{
    public class DeflateCompression : DcxCompression
    {
        public const string DeflateSignature = "DFLT";
        public int Level { get; private set; }

        //public override MemoryStream CompressData(byte[] uncompressedData)
        //{
        //    MemoryStream compressedBufferStream = new MemoryStream();
        //    using (var zlibStream = new ZlibStream(compressedBufferStream, CompressionMode.Compress, (CompressionLevel)Level))
        //    {
        //        zlibStream.Write(uncompressedData, 0, uncompressedData.Length);
        //    }

        //    return compressedBufferStream;
        //}

        public override MemoryStream DecompressData(byte[] compressedData, int uncompressedSize)
        {
            var decompressedData = ZlibStream.UncompressBuffer(compressedData);
            return new MemoryStream(decompressedData);
        }

        public static DeflateCompression Read(BinaryReader reader)
        {
            DeflateCompression result = new DeflateCompression();
            int headerSize = reader.ReadInt32();
            result.Level = reader.ReadInt32();
            reader.Skip(16);
            return result;
        }
    }
}
