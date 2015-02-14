using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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

        public static Bhd5Bucket Read(Stream inputStream)
        {
            Bhd5Bucket result = new Bhd5Bucket();
            BinaryReader reader = new BinaryReader(inputStream, Encoding.UTF8, true);

            int bucketEntryCount = reader.ReadInt32();
            int bucketOffset = reader.ReadInt32();

            long currentPosition = inputStream.Position;
            inputStream.Seek(bucketOffset, SeekOrigin.Begin);

            for (int i = 0; i < bucketEntryCount; i++)
            {
                result._entries.Add(Bhd5BucketEntry.Read(inputStream));
            }
            inputStream.Seek(currentPosition, SeekOrigin.Begin);
            return result;
        }
    }
}
