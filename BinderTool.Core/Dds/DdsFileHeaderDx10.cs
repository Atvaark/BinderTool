using System;
using System.IO;
using System.Text;
using BinderTool.Core.Dds.Enum;

namespace BinderTool.Core.Dds
{
    public class DdsFileHeaderDx10
    {
        public DxgiFormat Format { get; set; }

        public D3D10ResourceDimension ResourceDimension { get; set; }

        public uint MiscFlag { get; set; }

        public uint ArraySize { get; set; }

        public uint MiscFlags2 { get; set; }
        
        public void Write(Stream outputStream)
        {
            BinaryWriter writer = new BinaryWriter(outputStream, Encoding.Default, true);
            writer.Write(Convert.ToUInt32(Format));
            writer.Write(Convert.ToInt32(ResourceDimension));
            writer.Write(MiscFlag);
            writer.Write(ArraySize);
            writer.Write(MiscFlags2);
        }
    }
}
