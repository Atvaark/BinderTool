using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BinderTool.Core.Bhd5;

namespace BinderTool.Core.BHD5
{
    public class Bhd5File
    {
        private const string Bhd5Signature = "BHD5";
        private const byte Bhd5UnknownConstant1 = 255;
        private const int Bh5UnknownConstant2 = 1;
        private readonly List<Bhd5Bucket> _buckets = new List<Bhd5Bucket>();

        public IEnumerable<Bhd5Bucket> GetBuckets()
        {
            return _buckets.AsEnumerable();
        }

        public static Bhd5File Read(Stream inputStream)
        {
            Bhd5File result = new Bhd5File();

            BinaryReader reader = new BinaryReader(inputStream, Encoding.UTF8, true);

            string signature = new string(reader.ReadChars(4));
            if (signature != Bhd5Signature)
                throw new Bhd5FileReadException("Invalid signature");
            reader.Skip(12);
            int bucketDirectoryEntryCount = reader.ReadInt32();
            int bucketDirectoryOffset = reader.ReadInt32();
            int saltLength = reader.ReadInt32();
            string salt = new string(reader.ReadChars(saltLength));

            for (int i = 0; i < bucketDirectoryEntryCount; i++)
            {
                result._buckets.Add(Bhd5Bucket.Read(inputStream));
            }


            return result;
        }
    }
}
