using System.IO;

namespace BinderTool.Core.Bhd5
{
    public class Bhd5BucketEntry
    {
        public uint FileNameHash { get; private set; }
        public long FileSize { get; set; }
        public long PaddedFileSize { get; set; }
        public long FileOffset { get; private set; }
        public Bhd5AesKey AesKey { get; private set; }
        public Bhd5SaltedShaHash ShaHash { get; private set; }
        public bool IsEncrypted => AesKey != null;

        public static Bhd5BucketEntry Read(BinaryReader reader)
        {
            Bhd5BucketEntry result = new Bhd5BucketEntry();
            result.FileNameHash = reader.ReadUInt32();
            result.PaddedFileSize = reader.ReadUInt32();
            result.FileOffset = reader.ReadInt64();
            long saltedHashOffset = reader.ReadInt64();
            long aesKeyOffset = reader.ReadInt64();
            result.FileSize = reader.ReadInt64();

            if (saltedHashOffset != 0)
            {
                long currentPosition = reader.GetPosition();
                reader.Seek(saltedHashOffset, SeekOrigin.Begin);
                result.ShaHash = Bhd5SaltedShaHash.Read(reader);
                reader.Seek(currentPosition, SeekOrigin.Begin);
            }

            if (aesKeyOffset != 0)
            {
                long currentPosition = reader.GetPosition();
                reader.Seek(aesKeyOffset, SeekOrigin.Begin);
                result.AesKey = Bhd5AesKey.Read(reader);
                reader.Seek(currentPosition, SeekOrigin.Begin);
            }

            return result;
        }

    }
}
