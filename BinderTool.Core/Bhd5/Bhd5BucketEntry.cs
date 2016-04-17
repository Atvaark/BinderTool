using System.IO;
using System.Text;

namespace BinderTool.Core.Bhd5
{
    public class Bhd5BucketEntry
    {
        public uint FileNameHash { get; private set; }
        public uint FileNameHashUnknown { get; private set; }
        public long FileSize { get; set; }
        public long? PaddedFileSize { get; set; }
        public long FileOffset { get; private set; }
        public Bhd5AesKey AesKey { get; private set; }
        public Bhd5SaltedShaHash ShaHash { get; private set; }

        public static Bhd5BucketEntry Read(Stream inputStream)
        {
            Bhd5BucketEntry result = new Bhd5BucketEntry();
            BinaryReader reader = new BinaryReader(inputStream, Encoding.UTF8, true);
            result.FileNameHash = reader.ReadUInt32();
            result.FileNameHashUnknown = reader.ReadUInt32();
            result.FileOffset = reader.ReadInt64();
            long saltedHashOffset = reader.ReadInt64();
            long aesKeyOffset = reader.ReadInt64();
            result.FileSize = reader.ReadInt64();

            if (saltedHashOffset != 0)
            {
                long currentPosition = inputStream.Position;
                inputStream.Seek(saltedHashOffset, SeekOrigin.Begin);
                result.ShaHash = Bhd5SaltedShaHash.Read(inputStream);
                inputStream.Seek(currentPosition, SeekOrigin.Begin);
            }

            if (aesKeyOffset != 0)
            {
                long currentPosition = inputStream.Position;
                inputStream.Seek(aesKeyOffset, SeekOrigin.Begin);
                result.AesKey = Bhd5AesKey.Read(inputStream);
                inputStream.Seek(currentPosition, SeekOrigin.Begin);
            }

            if (result.FileSize == 0)
            {
                // TODO: Try get the file length by reading the file. (99% DCX)
            }

            return result;
        }

    }
}
