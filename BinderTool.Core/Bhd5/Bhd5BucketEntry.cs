using System;
using System.IO;

namespace BinderTool.Core.Bhd5
{
    public class Bhd5BucketEntry
    {
        public ulong FileNameHash { get; private set; }
        public long FileSize { get; set; }
        public long PaddedFileSize { get; set; }
        public long FileOffset { get; private set; }
        public Bhd5AesKey AesKey { get; private set; }
        public Bhd5SaltedShaHash ShaHash { get; private set; }
        public bool IsEncrypted => AesKey != null;

        public static Bhd5BucketEntry Read(BinaryReader reader, GameVersion version)
        {
            Bhd5BucketEntry result = new Bhd5BucketEntry();
            long saltedHashOffset;
            long aesKeyOffset;
            if (version == GameVersion.EldenRing) {
                result.FileNameHash = reader.ReadUInt64();
                result.PaddedFileSize = reader.ReadUInt32();
                result.FileSize = reader.ReadUInt32();
                if (result.FileSize == 0) result.FileSize = result.PaddedFileSize;
                result.FileOffset = reader.ReadInt64();

                saltedHashOffset = reader.ReadInt64();
                aesKeyOffset = reader.ReadInt64();
            } else {
                result.FileNameHash = reader.ReadUInt32();
                result.PaddedFileSize = reader.ReadUInt32();
                result.FileOffset = reader.ReadInt64();
                saltedHashOffset = reader.ReadInt64();
                aesKeyOffset = reader.ReadInt64();

                switch (version) {
                    case GameVersion.DarkSouls3:
                    case GameVersion.Sekiro:
                        result.FileSize = reader.ReadInt64();
                        break;
                    default:
                        result.FileSize = result.PaddedFileSize;
                        break;
                }
            }

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
