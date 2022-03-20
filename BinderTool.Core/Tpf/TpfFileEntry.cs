using System.IO;
using BinderTool.Core.Dds;
using BinderTool.Core.Dds.Enum;

namespace BinderTool.Core.Tpf
{
    public class TpfFileEntry
    {
        public GameVersion GameVersion { get; set; }

        public byte Format { get; set; }

        public byte Type { get; set; }

        public byte MipMapCount { get; set; }

        public byte Flags { get; set; }

        public int Unknown { get; set; }

        public int DxgiFormat { get; set; }

        public short Height { get; set; }

        public short Width { get; set; }

        public byte[] Data { get; set; }

        public string FileName { get; set; }

        public static TpfFileEntry Read(BinaryReader reader, GameVersion gameVersion)
        {
            TpfFileEntry result = new TpfFileEntry();
            result.GameVersion = gameVersion;
            int dataOffset = reader.ReadInt32();
            int dataSize = reader.ReadInt32();

            //   0 = ?
            //   1 = ?
            //   5 = ?
            //   6 = ?
            //   9 = ?
            //  10 = ?
            // 100 = ?
            // 102 = ?
            // 103 = ?
            // 104 = ?
            // 105 = ?
            // 106 = ?
            // 107 = ?
            // 108 = ?
            // 109 = ?
            // 113 = ?
            result.Format = reader.ReadByte();
            result.Type = reader.ReadByte(); // 0 = texture, 1 = cubemap

            result.MipMapCount = reader.ReadByte();
            result.Flags = reader.ReadByte();

            int fileNameOffset;
            if (gameVersion == GameVersion.Bloodborne)
            {
                result.Width = reader.ReadInt16();
                result.Height = reader.ReadInt16();
                int unknown1 = reader.ReadInt32(); // 1
                int unknown2 = reader.ReadInt32(); // 13
                fileNameOffset = reader.ReadInt32();
                int unknown3 = reader.ReadInt32(); // 0
                result.DxgiFormat = reader.ReadInt32();
            }
            else
            {
                fileNameOffset = reader.ReadInt32();
                result.Unknown = reader.ReadInt32(); // 0 = ?, 1 = ?
            }
            
            long position = reader.GetPosition();
            if (fileNameOffset > 0)
            {
                reader.Seek(fileNameOffset);
                result.FileName = reader.ReadNullTerminatedString();
                result.FileName += ".dds";
            }

            if (dataOffset > 0)
            {
                reader.Seek(dataOffset);
                result.Data = reader.ReadBytes(dataSize);
            }

            reader.Seek(position);

            return result;
        }
        
        public ulong Write(Stream outputStream)
        {
            if (Data == null)
            {
                return 0;
            }

            byte[] data = Data;
            if (GameVersion == GameVersion.Bloodborne)
            {
                DxgiFormat dxgiFormat = (DxgiFormat) DxgiFormat;
                DdsPixelFormat pixelFormat = DdsPixelFormat.DxgiToDdsPixelFormat(dxgiFormat);
                DdsFile ddsFile = new DdsFile
                {
                    Header = new DdsFileHeader
                    {
                        Flags = DdsFileHeaderFlags.Texture | (MipMapCount > 1 ? DdsFileHeaderFlags.MipMap : 0),
                        Height = Height,
                        Width = Width,
                        Depth = 0,
                        PitchOrLinearSize = Data.Length,
                        MipMapCount = MipMapCount,
                        PixelFormat = pixelFormat,
                        Caps = DdsSurfaceFlags.Texture | (MipMapCount > 1 ? DdsSurfaceFlags.MipMap : 0)
                    },
                    HeaderDx10 = pixelFormat.FourCc != DdsPixelFormat.Dx10FourCc
                        ? null
                        : new DdsFileHeaderDx10
                              {
                                  Format = dxgiFormat,
                                  ResourceDimension = D3D10ResourceDimension.Texture2D,
                                  MiscFlag = 0,
                                  ArraySize = 1,
                                  MiscFlags2 = 0
                              },
                    Data = DdsFile.ConvertData(data, Height, Width, dxgiFormat)
                };
                return ddsFile.Write(outputStream);
            } else {
                outputStream.Write(data, 0, data.Length);
                return (ulong)data.Length;
            }
        }
    }
}