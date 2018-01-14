using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BinderTool.Core.Fmg
{
    public class FmgFile
    {
        public List<FmgFileEntry> Entries { get; set; }

        public static FmgFile ReadFmgFile(Stream inputStream)
        {
            FmgFile fmgFile = new FmgFile();
            fmgFile.Read(inputStream);
            return fmgFile;
        }

        public void Read(Stream inputStream)
        {
            BinaryReader reader = new BinaryReader(inputStream, Encoding.Unicode, true);

            int unknown1 = reader.ReadInt32(); // 131072
            int fileSize = reader.ReadInt32();
            int unknown2 = reader.ReadInt32(); // 1
            int idRangeCount = reader.ReadInt32();
            int stringOffsetCount = reader.ReadInt32();
            int unknown3 = reader.ReadInt32(); // 255
            long stringOffsetSectionOffset = reader.ReadInt64();
            int unknown4 = reader.ReadInt32(); // 0
            int unknown5 = reader.ReadInt32(); // 0
            
            FmgIdRange[] idRanges = new FmgIdRange[idRangeCount];
            for (int i = 0; i < idRangeCount; i++)
            {
                idRanges[i] = new FmgIdRange();
                idRanges[i].Read(reader);
            }

            inputStream.Seek(stringOffsetSectionOffset, SeekOrigin.Begin);
            long[] offsets = new long[stringOffsetCount];
            for (int i = 0; i < stringOffsetCount; i++)
            {
                offsets[i] = reader.ReadInt64();
            }

            List<FmgFileEntry> entries = new List<FmgFileEntry>();
            foreach (FmgIdRange idRange in idRanges)
            {
                for (int i = 0; i < idRange.IdCount; i++)
                {
                    FmgFileEntry entry = new FmgFileEntry();
                    entry.Id = idRange.FirstId + i;

                    long offset = offsets[idRange.OffsetIndex + i];
                    if (offset > 0)
                    {
                        reader.Seek(offset, SeekOrigin.Begin);
                        entry.Value = reader.ReadNullTerminatedString();
                    }
                    else
                    {
                        entry.Value = string.Empty;
                    }

                    entries.Add(entry);
                }
            }

            Entries = entries;
        }
    }
}