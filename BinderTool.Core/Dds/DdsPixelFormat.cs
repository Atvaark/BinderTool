using System;
using System.IO;
using System.Text;
using BinderTool.Core.Dds.Enum;

namespace BinderTool.Core.Dds
{
    public class DdsPixelFormat
    {
        private const int DefaultSize = 32;
        internal const int Dxt1FourCc = 0x31545844;
        internal const int Dxt2FourCc = 0x32545844;
        internal const int Dxt3FourCc = 0x33545844;
        internal const int Dtx4FourCc = 0x34545844;
        internal const int Dtx5FourCc = 0x35545844;
        internal const int Dx10FourCc = 0x30315844;
        internal const int Ati1FourCc = 0x31495441;
        internal const int Ati2FourCc = 0x32495441;
        public int Size { get; set; }
        public DdsPixelFormatFlag Flags { get; set; }
        public int FourCc { get; set; }
        public int RgbBitCount { get; set; }
        public uint RBitMask { get; set; }
        public uint GBitMask { get; set; }
        public uint BBitMask { get; set; }
        public uint ABitMask { get; set; }
        
        public void Write(Stream outputStream)
        {
            BinaryWriter writer = new BinaryWriter(outputStream, Encoding.Default, true);
            writer.Write(Size);
            writer.Write(Convert.ToUInt32(Flags));
            writer.Write(FourCc);
            writer.Write(RgbBitCount);
            writer.Write(RBitMask);
            writer.Write(GBitMask);
            writer.Write(BBitMask);
            writer.Write(ABitMask);
        }
        
        public static int DxgiToBytesPerPixel(DxgiFormat dxgiFormat)
        {
            switch (dxgiFormat)
            {
                case DxgiFormat.Unknown:
                    return 0;
                case DxgiFormat.R32G32B32A32Typeless:
                case DxgiFormat.R32G32B32A32Float:
                case DxgiFormat.R32G32B32A32Uint:
                case DxgiFormat.R32G32B32A32Sint:
                    return 128;
                case DxgiFormat.R32G32B32Typeless:
                case DxgiFormat.R32G32B32Float:
                case DxgiFormat.R32G32B32Uint:
                case DxgiFormat.R32G32B32Sint:
                    return 96;
                case DxgiFormat.R16G16B16A16Typeless:
                case DxgiFormat.R16G16B16A16Float:
                case DxgiFormat.R16G16B16A16Unorm:
                case DxgiFormat.R16G16B16A16Uint:
                case DxgiFormat.R16G16B16A16Snorm:
                case DxgiFormat.R16G16B16A16Sint:
                case DxgiFormat.R32G32Typeless:
                case DxgiFormat.R32G32Float:
                case DxgiFormat.R32G32Uint:
                case DxgiFormat.R32G32Sint:
                case DxgiFormat.R32G8X24Typeless:
                case DxgiFormat.D32FloatS8X24Uint:
                case DxgiFormat.R32FloatX8X24Typeless:
                case DxgiFormat.X32TypelessG8X24Uint:
                    return 64;
                case DxgiFormat.R10G10B10A2Typeless:
                case DxgiFormat.R10G10B10A2Unorm:
                case DxgiFormat.R10G10B10A2Uint:
                case DxgiFormat.R11G11B10Float:
                case DxgiFormat.R8G8B8A8Typeless:
                case DxgiFormat.R8G8B8A8Unorm:
                case DxgiFormat.R8G8B8A8UnormSrgb:
                case DxgiFormat.R8G8B8A8Uint:
                case DxgiFormat.R8G8B8A8Snorm:
                case DxgiFormat.R8G8B8A8Sint:
                case DxgiFormat.R16G16Typeless:
                case DxgiFormat.R16G16Float:
                case DxgiFormat.R16G16Unorm:
                case DxgiFormat.R16G16Uint:
                case DxgiFormat.R16G16Snorm:
                case DxgiFormat.R16G16Sint:
                case DxgiFormat.R32Typeless:
                case DxgiFormat.D32Float:
                case DxgiFormat.R32Float:
                case DxgiFormat.R32Uint:
                case DxgiFormat.R32Sint:
                case DxgiFormat.R24G8Typeless:
                case DxgiFormat.D24UnormS8Uint:
                case DxgiFormat.R24UnormX8Typeless:
                case DxgiFormat.X24TypelessG8Uint:
                    return 32;
                case DxgiFormat.R8G8Typeless:
                case DxgiFormat.R8G8Unorm:
                case DxgiFormat.R8G8Uint:
                case DxgiFormat.R8G8Snorm:
                case DxgiFormat.R8G8Sint:
                case DxgiFormat.R16Typeless:
                case DxgiFormat.R16Float:
                case DxgiFormat.D16Unorm:
                case DxgiFormat.R16Unorm:
                case DxgiFormat.R16Uint:
                case DxgiFormat.R16Snorm:
                case DxgiFormat.R16Sint:
                    return 16;
                case DxgiFormat.R8Typeless:
                case DxgiFormat.R8Unorm:
                case DxgiFormat.R8Uint:
                case DxgiFormat.R8Snorm:
                case DxgiFormat.R8Sint:
                case DxgiFormat.A8Unorm:
                    return 8;
                case DxgiFormat.R1Unorm:
                    return 1;
                case DxgiFormat.R9G9B9E5Sharedexp:
                case DxgiFormat.R8G8B8G8Unorm:
                case DxgiFormat.G8R8G8B8Unorm:
                    return 32;
                case DxgiFormat.Bc1Typeless:
                case DxgiFormat.Bc1Unorm:
                case DxgiFormat.Bc1UnormSrgb:
                    return 4;
                case DxgiFormat.Bc2Typeless:
                case DxgiFormat.Bc2Unorm:
                case DxgiFormat.Bc2UnormSrgb:
                case DxgiFormat.Bc3Typeless:
                case DxgiFormat.Bc3Unorm:
                case DxgiFormat.Bc3UnormSrgb:
                    return 8;
                case DxgiFormat.Bc4Typeless:
                case DxgiFormat.Bc4Unorm:
                case DxgiFormat.Bc4Snorm:
                    return 4;
                case DxgiFormat.Bc5Typeless:
                case DxgiFormat.Bc5Unorm:
                case DxgiFormat.Bc5Snorm:
                    return 8;
                case DxgiFormat.B5G6R5Unorm:
                case DxgiFormat.B5G5R5A1Unorm:
                    return 16;
                case DxgiFormat.B8G8R8A8Unorm:
                case DxgiFormat.B8G8R8X8Unorm:
                case DxgiFormat.R10G10B10XrBiasA2Unorm:
                case DxgiFormat.B8G8R8A8Typeless:
                case DxgiFormat.B8G8R8A8UnormSrgb:
                case DxgiFormat.B8G8R8X8Typeless:
                case DxgiFormat.B8G8R8X8UnormSrgb:
                    return 32;
                case DxgiFormat.Bc6HTypeless:
                case DxgiFormat.Bc6HUf16:
                case DxgiFormat.Bc6HSf16:
                case DxgiFormat.Bc7Typeless:
                case DxgiFormat.Bc7Unorm:
                case DxgiFormat.Bc7UnormSrgb:
                    return 8;
                case DxgiFormat.Ayuv:
                case DxgiFormat.Y410:
                case DxgiFormat.Y416:
                case DxgiFormat.Nv12:
                case DxgiFormat.P010:
                case DxgiFormat.P016:
                case DxgiFormat.Opaque420:
                case DxgiFormat.Yuy2:
                case DxgiFormat.Y210:
                case DxgiFormat.Y216:
                case DxgiFormat.Nv11:
                case DxgiFormat.Ai44:
                case DxgiFormat.Ia44:
                case DxgiFormat.P8:
                case DxgiFormat.A8P8:
                    return 0;
                case DxgiFormat.B4G4R4A4Unorm:
                    return 16;
                default:
                    return 0;
            }
        }

        public static DdsPixelFormat DxgiToDdsPixelFormat(DxgiFormat dxgiFormat)
        {
            DdsPixelFormat pixelFormat;
            switch (dxgiFormat)
            {
                case DxgiFormat.Bc1Unorm:
                case DxgiFormat.Bc1UnormSrgb:
                    pixelFormat = DdsPfDxt1();
                    break;
                case DxgiFormat.Bc2Unorm:
                    pixelFormat = DdsPfDxt3();
                    break;
                case DxgiFormat.Bc3Unorm:
                    pixelFormat = DdsPfDxt5();
                    break;
                case DxgiFormat.Bc4Unorm:
                    pixelFormat = DdsPfAti1();
                    break;
                case DxgiFormat.Bc5Unorm:
                    pixelFormat = DdsPfAti2();
                    break;
                default:
                    pixelFormat = DdsPfDx10();
                    break;
            }

            return pixelFormat;
        }

        private static int DxgiToFourCc(DxgiFormat dxgiFormat)
        {
            int fourCc;
            switch (dxgiFormat)
            {
                case DxgiFormat.Bc1Unorm:
                case DxgiFormat.Bc1UnormSrgb:
                    fourCc = Dxt1FourCc; // DXT1
                    break;
                case DxgiFormat.Bc2Unorm:
                    fourCc = Dxt3FourCc; // DXT3
                    break;
                case DxgiFormat.Bc3Unorm:
                    fourCc = Dtx5FourCc; // DXT5
                    break;
                case DxgiFormat.Bc4Unorm:
                    fourCc = Ati1FourCc; // ATI1
                    break;
                case DxgiFormat.Bc5Unorm:
                    fourCc = Ati2FourCc; // ATI2
                    break;
                default:
                    fourCc = Dx10FourCc; // DX10
                    break;
            }

            return fourCc;
        }
        
        public static DdsPixelFormat DdsPfDxt1()
        {
            return DdsPfDx(Dxt1FourCc); // DXT1
        }

        public static DdsPixelFormat DdsPfDxt2()
        {
            return DdsPfDx(Dxt2FourCc); // DXT2
        }

        public static DdsPixelFormat DdsPfDxt3()
        {
            return DdsPfDx(Dxt3FourCc); // DXT3
        }

        public static DdsPixelFormat DdsPfDxt4()
        {
            return DdsPfDx(Dtx4FourCc); // DXT4
        }

        public static DdsPixelFormat DdsPfDxt5()
        {
            return DdsPfDx(Dtx5FourCc); // DXT5
        }

        public static DdsPixelFormat DdsPfAti1()
        {
            return DdsPfDx(Ati1FourCc); // ATI1
        }

        public static DdsPixelFormat DdsPfAti2()
        {
            return DdsPfDx(Ati2FourCc); // ATI2
        }

        public static DdsPixelFormat DdsPfDx10()
        {
            return DdsPfDx(Dx10FourCc); // DX10
        }

        private static DdsPixelFormat DdsPfDx(int fourCc)
        {
            DdsPixelFormat pixelFormat = new DdsPixelFormat
            {
                Size = DefaultSize,
                Flags = DdsPixelFormatFlag.FourCc,
                FourCc = fourCc
            };
            return pixelFormat;
        }
        
        protected bool Equals(DdsPixelFormat other)
        {
            return Size == other.Size &&
                   Flags == other.Flags &&
                   FourCc == other.FourCc &&
                   RgbBitCount == other.RgbBitCount &&
                   RBitMask == other.RBitMask &&
                   GBitMask == other.GBitMask &&
                   BBitMask == other.BBitMask &&
                   ABitMask == other.ABitMask;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((DdsPixelFormat) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Size;
                hashCode = (hashCode*397) ^ (int) Flags;
                hashCode = (hashCode*397) ^ FourCc;
                hashCode = (hashCode*397) ^ RgbBitCount;
                hashCode = (hashCode*397) ^ (int) RBitMask;
                hashCode = (hashCode*397) ^ (int) GBitMask;
                hashCode = (hashCode*397) ^ (int) BBitMask;
                hashCode = (hashCode*397) ^ (int) ABitMask;
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"Size: {Size}, Flags: {Flags}, FourCc: {FourCc}, RgbBitCount: {RgbBitCount}," +
                   $" RBitMask: {RBitMask}, GBitMask: {GBitMask}, BBitMask: {BBitMask}, ABitMask: {ABitMask}";
        }
    }
}
