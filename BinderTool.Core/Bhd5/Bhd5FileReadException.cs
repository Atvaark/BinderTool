using System;

namespace BinderTool.Core.Bhd5
{
    internal class Bhd5FileReadException : Exception
    {
        public Bhd5FileReadException()
        {
        }

        public Bhd5FileReadException(string message) : base(message)
        {
        }

        public Bhd5FileReadException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
