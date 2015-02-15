using System.IO;
using BinderTool.Core.Bhd5;

namespace BinderTool.Core.Bdt5
{
    public class Bdt5FileStream
    {
        private readonly Stream _inputStream;

        public Bdt5FileStream(Stream inputStream)
        {
            _inputStream = inputStream;
        }

        public MemoryStream ReadBhd5Entry(Bhd5BucketEntry entry)
        {
            if ((entry.FileOffset + entry.FileSize) > _inputStream.Length)
                throw new EndOfStreamException();
            _inputStream.Seek(entry.FileOffset, SeekOrigin.Begin);

            byte[] buffer = new byte[entry.FileSize];
            _inputStream.Read(buffer, 0, entry.FileSize);
            return new MemoryStream(buffer);
            // TODO: Return a Stream instead of a MemoryStream
        }

        public static Bdt5FileStream OpenFile(string path, FileMode mode, FileAccess access)
        {
            FileStream bdtStream = new FileStream(path, mode, access);
            return new Bdt5FileStream(bdtStream);
        }
    }
}
