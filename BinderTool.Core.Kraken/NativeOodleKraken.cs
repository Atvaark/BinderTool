using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BinderTool.Core.Kraken
{
    public static class NativeOodleKraken
    {
        /// <summary>
        ///     See: OodleLZ_CompressionLevel_GetName
        /// </summary>
        private enum OodleLZCompressionLevel : ulong
        {
            //None = 0,
            //SuperFast = 1,
            //VeryFast = 2,
            //Fast = 3,
            Normal = 4,
            //Optimal1 = 5,
            //Optimal2 = 6,
            //Optimal3 = 7,
            //Optimal4 = 8,
            //Optimal5 = 9,
            //TooHigh = 10,
        }

        /// <summary>
        ///     See: OodleLZ_Compressor_GetName
        /// </summary>
        private enum OodleLZCompressor : uint
        {
            Kraken = 8,
            Mermaid = 9,
            Leviathan = 13
        }

        [DllImport("oo2core_6_win64.dll")]
        private static extern long OodleLZ_Compress(
            OodleLZCompressor compressor,
            byte[] buffer,
            long bufferSize, 
            byte[] outputBuffer,
            OodleLZCompressionLevel level,
            long param_6,
            long parm_7,
            long parm_8,
            long param_9,
            long param_10);

        [DllImport("oo2core_6_win64.dll")]
        private static extern int OodleLZ_Decompress(
            byte[] buffer,
            long bufferSize,
            byte[] outputBuffer,
            long outputBufferSize,
            uint param_5,
            uint param_6,
            ulong param_7,
            uint param_8,
            uint param_9,
            uint param_10,
            uint param_11,
            uint param_12,
            uint param_13,
            uint param_14);

        [DllImport("oo2core_6_win64.dll")]
        private static extern uint OodleLZ_GetCompressedBufferSizeNeeded(long inSize);

        //[DllImport("oo2core_6_win64.dll")]
        //private static extern IntPtr OodleLZ_CompressOptions_GetDefault(OodleLZCompressor lzCompressor, OodleLZCompressionLevel level);

        //[DllImport("oo2core_6_win64.dll")]
        //private static extern uint OodleLZ_GetCompressedStepForRawStep();

        //[DllImport("oo2core_6_win64.dll")]
        //private static extern uint OodleLZDecoder_MemorySizeNeeded();

        //private static byte[] Compress(byte[] buffer, int size, OodleLZCompressor lzCompressor, OodleLZCompressionLevel level)
        //{
        //    uint compressedBufferSize = OodleLZ_GetCompressedBufferSizeNeeded(size);
        //    byte[] compressedBuffer = new byte[compressedBufferSize];
        //    //OodleLZ_CompressOptions options = OodleLZ_CompressOptions_GetDefault(lzCompressor, level);
        //    long compressedSize = OodleLZ_Compress(
        //        lzCompressor,
        //        buffer,
        //        size,
        //        compressedBuffer,
        //        level,
        //        0, //  options
        //        0,
        //        0,
        //        0,
        //        0);

        //    byte[] outputBuffer = new byte[compressedSize];
        //    Buffer.BlockCopy(compressedBuffer, 0, outputBuffer, 0, (int)compressedSize);
        //    return outputBuffer;
        //}
        
        public static byte[] Decompress(byte[] buffer, int size, int uncompressedSize)
        {
            byte[] decompressedBuffer = new byte[uncompressedSize];
            int decompressedSize = OodleLZ_Decompress(buffer, size, decompressedBuffer, uncompressedSize, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3);

            if (decompressedSize == 0)
            {
                // Fails on Sekiro Data5.bdt > 7052488_Data5.bnd, yet {decompressedBuffer} contains a valid .bnd file
                Debug.WriteLine($"Failed to decompress buffer of size {size} to {uncompressedSize}.");
                //throw new ApplicationException("Failed to decompress buffer");
            }
            else if (decompressedSize != uncompressedSize)
            {
                Array.Resize(ref decompressedBuffer, decompressedSize);
            }

            return decompressedBuffer;
        }
    }
}