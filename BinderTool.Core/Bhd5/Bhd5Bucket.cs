using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BinderTool.Core.Bhd5
{
    public class Bhd5Bucket
    {
        private readonly List<Bhd5BucketEntry> _entries;

        public Bhd5Bucket()
        {
            _entries = new List<Bhd5BucketEntry>();
        }

        public IEnumerable<Bhd5BucketEntry> GetEntries()
        {
            return _entries.AsEnumerable();
        }

        public static Bhd5Bucket Read(BinaryReader reader, DSVersion version)
        {
            Bhd5Bucket result = new Bhd5Bucket();

            int bucketEntryCount = reader.ReadInt32();
            int bucketOffset = reader.ReadInt32();

            long currentPosition = reader.GetPosition();
            reader.Seek(bucketOffset, SeekOrigin.Begin);

            for (int i = 0; i < bucketEntryCount; i++)
            {
                result._entries.Add(Bhd5BucketEntry.Read(reader, version));
            }

            reader.Seek(currentPosition, SeekOrigin.Begin);
            return result;
        }
    }
}
