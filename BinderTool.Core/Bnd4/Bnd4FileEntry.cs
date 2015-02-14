using System.IO;
using System.Text;

namespace BinderTool.Core.Bnd4
{
    public class Bnd4FileEntry
    {
        public byte[] EntryData { get; private set; }
        public string FileName { get; private set; }

        public static Bnd4FileEntry Read(Stream inputStream, int fileSize, string fileName)
        {
            Bnd4FileEntry result = new Bnd4FileEntry();
            BinaryReader reader = new BinaryReader(inputStream, Encoding.UTF8, true);
            result.FileName = fileName;
            result.EntryData = reader.ReadBytes(fileSize);
            return result;
        }
    }
}
