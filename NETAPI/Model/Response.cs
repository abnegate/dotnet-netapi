using System;

namespace NETAPI.Models
{
    public class ResponseBase<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public virtual T? Data { get; set; }
        public virtual Exception? Exception { get; set; }
    }
}
