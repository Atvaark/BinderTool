using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace BinderTool.Core.Bdt5
{
    public class Bdt5InnerStream : Stream
    {
        public const int MAX_STREAM_LEN = 10_000_000;

        private List<MemoryStream> streams;

        private long length;


        public override long Length => length;

        public override long Position { get; set; }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public Bdt5InnerStream(Stream from, long length)
        {
            this.length = length;
            this.streams = new List<MemoryStream>();
            for (long pos = 0; pos < length; pos += MAX_STREAM_LEN)
            {
                byte[] buf = new byte[MAX_STREAM_LEN];
                from.Read(buf, 0, (int)Math.Min(length - pos, MAX_STREAM_LEN));
                streams.Add(new MemoryStream(buf));
            }
        }
        public Bdt5InnerStream(long length)
        {
            this.length = length;
            this.streams = new List<MemoryStream>();
            for (long pos = 0; pos < length; pos += MAX_STREAM_LEN)
            {
                byte[] buf = new byte[MAX_STREAM_LEN];
                streams.Add(new MemoryStream(buf));
            }
        }

        public byte[] ToArray()
        {
            byte[] ans = new byte[length];
            this.Read(ans, 0, (int)length);
            return ans;
        }

        public override void Flush() {}

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (this.Position >= this.length) return 0;
            long startPos = this.Position;
            long end = Math.Min(this.Position + count, this.length);
            while (this.Position < end)
            {
                var s = this.streams[(int)(this.Position / MAX_STREAM_LEN)];
                s.Seek(this.Position % MAX_STREAM_LEN, SeekOrigin.Begin);
                var read = s.Read(buffer, offset, count);
                this.Position += read;
                offset += read;
                count -= read;
            }
            return (int)(end - startPos);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Begin) this.Position = offset;
            if (origin == SeekOrigin.Current) this.Position += offset;
            if (origin == SeekOrigin.End) this.Position = this.length + offset;
            return this.Position;
        }

        public override void SetLength(long value)
        {
            this.length = value;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            long end = this.Position + count;
            while (this.Position < end)
            {
                var s = this.streams[(int)(this.Position / MAX_STREAM_LEN)];
                s.Seek(this.Position % MAX_STREAM_LEN, SeekOrigin.Begin);
                var toWrite = (int)Math.Min(s.Length - s.Position, count);
                s.Write(buffer, offset, toWrite);
                this.Position += toWrite;
                offset += toWrite;
                count -= toWrite;
            }
        }
    }
}
