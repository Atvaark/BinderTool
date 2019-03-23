using System.IO;
using OodleSharp;

namespace BinderTool.Core.Dcx
{
    public class OodleCompression : DcxCompression
    {
        public const string OodleSignature = "KRAK";
        public int Level { get; private set; }

        public override MemoryStream CompressData(byte[] uncompressedData)
        {
            //MemoryStream compressedBufferStream = new MemoryStream();
            //using (DeflaterOutputStream deflaterStream = new DeflaterOutputStream(compressedBufferStream))
            //{
            //    deflaterStream.Write(uncompressedData, 0, uncompressedData.Length);
            //}
            //return compressedBufferStream;
            return null;
        }

        public override MemoryStream DecompressData(byte[] compressedData, int unCompressedSize)
        {
            MemoryStream outputStream = new MemoryStream(Oodle.Decompress(compressedData, compressedData.Length, unCompressedSize))
            {
                Position = 0
            };
            return outputStream;
        }

        public static OodleCompression Read(BinaryReader reader)
        {
            OodleCompression result = new OodleCompression();
            int headerSize = reader.ReadInt32();
            result.Level = reader.ReadInt32();
            reader.Skip(16);
            return result;
        }
    }
}
