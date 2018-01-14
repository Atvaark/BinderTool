using System;
using System.IO;
using System.Text;
using BinderTool.Core.Dds.Enum;

namespace BinderTool.Core.Dds
{
    public class DdsFile
    {
        private const int MagicNumber = 0x20534444;

        public DdsFileHeader Header { get; set; }

        public DdsFileHeaderDx10 HeaderDx10 { get; set; }

        public byte[] Data { get; set; }
        
        public void Write(Stream outputStream)
        {
            BinaryWriter writer = new BinaryWriter(outputStream, Encoding.Default, true);
            writer.Write(MagicNumber);
            Header.Write(outputStream);
            if (Header.IsDx10())
            {
                HeaderDx10.Write(outputStream);
            }

            writer.Write(Data);
        }

        public static byte[] ConvertData(byte[] sourceBuffer, int height, int width, DxgiFormat dxgiFormat)
        {
            if (sourceBuffer == null)
            {
                return null;
            }

            var inputStream = new MemoryStream(sourceBuffer);
            byte[] targetBuffer = new byte[sourceBuffer.Length];
            byte[] blockBuffer = new byte[64];
            int heightBlock = height / 4;
            int widthBlock = width / 4;
            int bytesPerPixel = DdsPixelFormat.DxgiToBytesPerPixel(dxgiFormat) * 2;
            for (int y = 0; y < heightBlock; y++)
            {
                for (int x = 0; x < widthBlock; x++)
                {
                    int mx = x;
                    int my = y;
                    if (widthBlock > 1 && heightBlock > 1)
                    {
                        MapBlockPosition(x, y, widthBlock, 2, out mx, out my);
                    }

                    if (widthBlock > 2 && heightBlock > 2)
                    {
                        MapBlockPosition(mx, my, widthBlock, 4, out mx, out my);
                    }

                    if (widthBlock > 4 && heightBlock > 4)
                    {
                        MapBlockPosition(mx, my, widthBlock, 8, out mx, out my);
                    }

                    inputStream.Read(blockBuffer, 0, bytesPerPixel);
                    int destinationIndex = bytesPerPixel * (my * widthBlock + mx);
                    Array.Copy(blockBuffer, 0, targetBuffer, destinationIndex, bytesPerPixel);
                }
            }

            return targetBuffer;
        }
        
        private static void MapBlockPosition(int x, int y, int w, int bx, out int mx, out int my)
        {
            int num1 = bx / 2;
            int num2 = x / bx;
            int num3 = y / num1;
            int num4 = x % bx;
            int num5 = y % num1;
            int num6 = w / bx;
            int num7 = 2 * num6;
            int num8 = num2 + num3 * num6;
            int num9 = num8 % num7;
            int num10 = num9 / 2 + num9 % 2 * num6;
            int num11 = num8 / num7 * num7 + num10;
            int num12 = num11 % num6;
            int num13 = num11 / num6;

            mx = num12 * bx + num4;
            my = num13 * num1 + num5;
        }
    }
}
