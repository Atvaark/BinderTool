using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace BinderTool.Core.Dcx
{
    public class DeflateCompression : DcxCompression
    {
        public const string DeflateSignature = "DFLT";
        public int Level { get; private set; }

        public override MemoryStream CompressData(byte[] uncompressedData)
        {
            MemoryStream compressedBufferStream = new MemoryStream();
            using (DeflaterOutputStream deflaterStream = new DeflaterOutputStream(compressedBufferStream))
            {
                deflaterStream.Write(uncompressedData, 0, uncompressedData.Length);
            }
            return compressedBufferStream;
        }

        public override MemoryStream DecompressData(byte[] compressedData)
        {
            InflaterInputStream inflaterStream = new InflaterInputStream(new MemoryStream(compressedData));
            MemoryStream outputStream = new MemoryStream();
            inflaterStream.CopyTo(outputStream);
            outputStream.Position = 0;
            return outputStream;
        }

        public static DeflateCompression Read(Stream inputStream)
        {
            DeflateCompression result = new DeflateCompression();
            BinaryReader reader = new BinaryReader(inputStream, Encoding.Default, true);
            int headerSize = reader.ReadInt32();
            result.Level = reader.ReadInt32();
            reader.Skip(16);
            return result;
        }
    }
}
