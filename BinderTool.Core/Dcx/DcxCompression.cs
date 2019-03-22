using System.IO;

namespace BinderTool.Core.Dcx
{
    public abstract class DcxCompression
    {
        //public abstract MemoryStream CompressData(byte[] uncompressedData);
        public abstract MemoryStream DecompressData(byte[] compressedData, int uncompressedSize);
    }
}
