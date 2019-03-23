using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace OodleSharp
{
    public static class Oodle
    {
        [DllImport("oo2core_3_win64.dll")]
        private static extern int OodleLZ_Compress(OodleFormat format, byte[] buffer, long bufferSize, byte[] outputBuffer, OodleCompressionLevel level, uint unused1, uint unused2, uint unused3);

        [DllImport("oo2core_3_win64.dll")]
        private static extern int OodleLZ_Decompress(byte[] buffer, long bufferSize, byte[] outputBuffer, long outputBufferSize,
            uint a, uint b, ulong c, uint d, uint e, uint f, uint g, uint h, uint i, uint threadModule);

        public static byte[] Compress(byte[] buffer, int size, OodleFormat format, OodleCompressionLevel level)
        {
            uint compressedBufferSize = GetCompressionBound((uint)size);
            byte[] compressedBuffer = new byte[compressedBufferSize];

            int compressedCount = OodleLZ_Compress(format, buffer, size, compressedBuffer, level, 0, 0, 0);

            byte[] outputBuffer = new byte[compressedCount];
            Buffer.BlockCopy(compressedBuffer, 0, outputBuffer, 0, compressedCount);
            
            return outputBuffer;
        }

        public static byte[] Decompress(byte[] buffer, int size, int uncompressedSize)
        {
            byte[] decompressedBuffer = new byte[uncompressedSize];
            int decompressedCount = OodleLZ_Decompress(buffer, size, decompressedBuffer, uncompressedSize, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3);

            if (decompressedCount == uncompressedSize)
            {
                return decompressedBuffer;
            }
            else if (decompressedCount < uncompressedSize)
            {
                return decompressedBuffer.Take(decompressedCount).ToArray();
            }
            else
            {
                throw new Exception("There was an error while decompressing");
            }
        }

        private static uint GetCompressionBound(uint bufferSize)
        {
            return bufferSize + 274 * ((bufferSize + 0x3FFFF) / 0x40000);
        }

        /*private static uint GetDecompressionBound(uint bufferSize)
        {
            uint v2 = bufferSize + 272 + 0;
            uint v3 = (bufferSize + 0x3FFFF) / 0x40000;
            if (bufferSize + 16731 + 2 * v3 < v2)
            {
                v2 = bufferSize + 16731 + 2 * v3;
            }
            return v2;
        }*/
    }
}
