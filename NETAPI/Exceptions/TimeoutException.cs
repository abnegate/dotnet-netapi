using System;

namespace NETAPI.Exceptions
{
    public class ApiTimeoutException : Exception
    {
        public ApiTimeoutException() : base() { }

        public ApiTimeoutException(string message) : base(message) { }

        public ApiTimeoutException(string message, Exception inner) : base(message, inner) { }
    }
}
