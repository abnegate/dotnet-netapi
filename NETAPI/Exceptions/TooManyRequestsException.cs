using System;

namespace NETAPI.Exceptions
{
    public class TooManyRequestsException : Exception
    {
        public TooManyRequestsException() : base() { }

        public TooManyRequestsException(string message) : base(message) { }

        public TooManyRequestsException(string message, Exception inner) : base(message, inner) { }
    }
}
