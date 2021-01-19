using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NETAPI.Extensions
{
    public static class CollectionUtils
    {
        public static IEnumerable<T> OrEmpty<T>(this IEnumerable<T> collection) =>
            collection ?? Enumerable.Empty<T>();
    }
}
