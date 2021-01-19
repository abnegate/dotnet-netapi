using System;

namespace NETAPI.Models
{
    public class RequestDebounce
    {
        public DateTime LastRequestTime { get; set; }
        public int RequestDelayMillis { get; set; }

        public RequestDebounce(int requestDelayMillis)
        {
            LastRequestTime = DateTime.MinValue;
            RequestDelayMillis = requestDelayMillis;
        }

        public RequestDebounce(
            int requestDelayMillis,
            DateTime lastRequestTime)
        {
            LastRequestTime = lastRequestTime;
            RequestDelayMillis = requestDelayMillis;
        }
    }
}
