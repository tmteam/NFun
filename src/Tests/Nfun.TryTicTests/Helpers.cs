using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NFun.UnitTests {
    public static class IEnumerableExtensions {
        public static IEnumerable<T> Append<T>(this IEnumerable<T> collection, T item) {
            if (collection == null)
            {
                throw new ArgumentNullException("Collection should not be null");
            }

            return collection.Concat(Enumerable.Repeat(item, 1));
        }
    }
}
