using System.Net;

namespace NETAPI.Utilities
{
    public class NetworkChecker
    {
        public bool HasInternet()
        {
            try {
                using (var client = new WebClient())
                using (client.OpenRead("http://google.com/generate_204")) {
                    return true;
                }
            } catch {
                return false;
            }
        }
    }
}
