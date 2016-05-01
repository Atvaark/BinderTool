using System;
using System.IO;

namespace BinderTool.Core.Bdt5
{
    public class Bdt5FileStream : IDisposable
    {
        private readonly Stream _inputStream;

        public Bdt5FileStream(Stream inputStream)
        {
            _inputStream = inputStream;
        }

        public long Length => _inputStream.Length;

        public MemoryStream Read(long fileOffset, long fileSize)
        {
            if (fileOffset + fileSize > _inputStream.Length)
                throw new EndOfStreamException();
            _inputStream.Seek(fileOffset, SeekOrigin.Begin);

            byte[] buffer = new byte[fileSize];
            _inputStream.Read(buffer, 0, (int) fileSize);
            return new MemoryStream(buffer);
        }

        public static Bdt5FileStream OpenFile(string path, FileMode mode, FileAccess access)
        {
            FileStream bdtStream = new FileStream(path, mode, access);
            return new Bdt5FileStream(bdtStream);
        }

        public void Dispose()
        {
            _inputStream.Dispose();
        }
    }
}
