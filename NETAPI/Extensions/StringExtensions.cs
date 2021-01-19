using System.IO;


namespace NETAPI.Extensions
{
    public static class StringExtensions
    {
        public static string ToValidFileName(this string s) =>
            string.Join("_", s.Split(Path.GetInvalidFileNameChars()));
    }
}
