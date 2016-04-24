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
    }
}
