using System;
using System.IO;
using System.Text;

namespace BinderTool.Core.IO
{
    internal class BigEndianBinaryReader : BinaryReader
    {
        private const byte DecimalSignBit = 128;

        public BigEndianBinaryReader(Stream input) : base(input)
        {
        }

        public BigEndianBinaryReader(Stream input, Encoding encoding) : base(input, encoding)
        {
        }

        public BigEndianBinaryReader(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen)
        {
        }

        private byte[] Reverse(byte[] buffer)
        {
            Array.Reverse(buffer);
            return buffer;
        }

        public override char ReadChar()
        {
            return base.ReadChar();
        }

        public override char[] ReadChars(int count)
        {
            return base.ReadChars(count);
        }

        public override decimal ReadDecimal()
        {
            byte[] src = Reverse(ReadBytes(sizeof (decimal)));
            return new decimal(
                BitConverter.ToInt32(src, 0),
                BitConverter.ToInt32(src, 4),
                BitConverter.ToInt32(src, 8),
                src[15] == DecimalSignBit,
                src[14]);
        }

        public override double ReadDouble()
        {
            return BitConverter.ToDouble(Reverse(ReadBytes(sizeof (double))), 0);
        }

        public override short ReadInt16()
        {
            return BitConverter.ToInt16(Reverse(ReadBytes(sizeof (short))), 0);
        }

        public override int ReadInt32()
        {
            return BitConverter.ToInt32(Reverse(ReadBytes(sizeof (int))), 0);
        }

        public override long ReadInt64()
        {
            return BitConverter.ToInt64(Reverse(ReadBytes(sizeof (long))), 0);
        }

        public override float ReadSingle()
        {
            return BitConverter.ToSingle(Reverse(ReadBytes(sizeof (float))), 0);
        }

        public override string ReadString()
        {
            int size = ReadInt32();
            return new string(ReadChars(size));
        }

        public override ushort ReadUInt16()
        {
            return BitConverter.ToUInt16(Reverse(ReadBytes(sizeof (ushort))), 0);
        }

        public override uint ReadUInt32()
        {
            return BitConverter.ToUInt32(Reverse(ReadBytes(sizeof (uint))), 0);
        }

        public override ulong ReadUInt64()
        {
            return BitConverter.ToUInt64(Reverse(ReadBytes(sizeof (ulong))), 0);
        }
    }
}
