using System;
using System.IO;
using System.Text;

namespace BinderTool.Core.Bdf4
{
    public class Bdf4FileStream : IDisposable
    {
        private readonly Stream _inputStream;
        public string Version { get; set; }

        public Bdf4FileStream(Stream inputStream)
        {
            _inputStream = inputStream;
        }

        public static Bdf4FileStream OpenFile(string inputPath, FileMode mode, FileAccess access)
        {
            FileStream inputStream = new FileStream(inputPath, mode, access);
            Bdf4FileStream bdfStream = new Bdf4FileStream(inputStream);
            bdfStream.ReadHeader();
            return bdfStream;
        }

        private void ReadHeader()
        {
            BinaryReader reader = new BinaryReader(_inputStream, Encoding.ASCII, true);
            string signature = reader.ReadString(4); // BDF4
            int unknown1 = reader.ReadInt32(); // Always 00 00 00 00?
            int unknown2 = reader.ReadInt32(); // Always 00 00 01 00?
            int unknown3 = reader.ReadInt32();
            int unknown4 = reader.ReadInt32(); // Always 48?
            int unknown5 = reader.ReadInt32();
            Version = reader.ReadString(8);
        }
        
        public MemoryStream Read(long fileOffset, long fileSize)
        {
            if (fileOffset + fileSize > _inputStream.Length)
                throw new EndOfStreamException();
            _inputStream.Seek(fileOffset, SeekOrigin.Begin);

            byte[] buffer = new byte[fileSize];
            _inputStream.Read(buffer, 0, (int)fileSize);
            return new MemoryStream(buffer);
        }

        public void Dispose()
        {
            _inputStream.Dispose();
        }
    }
}
