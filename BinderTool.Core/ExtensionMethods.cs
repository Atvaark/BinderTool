using System.IO;
using System.Text;

namespace BinderTool.Core
{
    internal static class ExtensionMethods
    {
        internal static void Skip(this BinaryReader reader, int count)
        {
            reader.BaseStream.Position += count;
        }

        internal static void WriteZeros(this BinaryWriter writer, int count)
        {
            byte[] zeros = new byte[count];
            writer.Write(zeros);
        }

        internal static string ReadString(this BinaryReader reader, int count)
        {
            return new string(reader.ReadChars(count));
        }

        internal static string ReadNullTerminatedString(this BinaryReader reader)
        {
            StringBuilder builder = new StringBuilder();
            char nextCharacter;
            while ((nextCharacter = reader.ReadChar()) != 0x00)
            {
                builder.Append(nextCharacter);
            }
            return builder.ToString();
        }

        internal static long GetPosition(this BinaryReader reader)
        {
            return reader.BaseStream.Position;
        }

        internal static void Seek(this BinaryReader reader, long offset)
        {
            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
        }

        internal static void Seek(this BinaryReader reader, long offset, SeekOrigin origin)
        {
            reader.BaseStream.Seek(offset, origin);
        }

        internal static void Align(this BinaryReader reader, int alignment)
        {
            long alignmentRequired = reader.BaseStream.Position % alignment;
            if (alignmentRequired > 0)
            {
                reader.BaseStream.Position += alignment - alignmentRequired;
            }
        }
    }
}
