using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BinderTool.Core.Bhf4;
using BinderTool.Core.Common;

namespace BinderTool.Core.Bdf4
{
    public class Bdf4File
    {
        public string Version { get; set; }

        public static Bdf4File ReadBdf4File(Stream inputStream)
        {
            Bdf4File bdf4File = new Bdf4File();
            bdf4File.Read(inputStream);
            return bdf4File;
        }

        public void Read(Stream inputStream)
        {
            BinaryReader reader = new BinaryReader(inputStream, Encoding.Default, true);
            string signature = reader.ReadString(4); // BDF4
            int unknown1 = reader.ReadInt32(); // Always 00 00 00 00?
            int unknown2 = reader.ReadInt32(); // Always 00 00 01 00?
            int unknown3 = reader.ReadInt32();
            int unknown4 = reader.ReadInt32(); // Always 48?
            int unknown5 = reader.ReadInt32();
            Version = reader.ReadString(8);
        }

        public void Write(Stream ouputStream)
        {
            BinaryWriter writer = new BinaryWriter(ouputStream, Encoding.Default, true);
            // TODO: Implement Bdf4File.Write
            throw new NotImplementedException();
        }

        public IEnumerable<DataContainer> ReadData(Stream inputStream, Bhf4File bhf4File)
        {
            foreach (var entry in bhf4File.Entries)
            {
                inputStream.Position = entry.FileOffset;
                byte[] data = new byte[entry.FileSize];
                inputStream.Read(data, 0, entry.FileSize);
                yield return new DataContainer
                {
                    DataStream = new MemoryStream(data),
                    Name = entry.FileName
                };
            }
        }
    }
}
