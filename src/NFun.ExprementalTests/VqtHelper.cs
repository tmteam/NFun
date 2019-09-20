using System.Linq;
using NFun.Runtime;

namespace NFun.ExprementalTests
{
    public static class VqtHelper{
        public static object MakeVQT(object value, int q, long t = -1)
        {
            if (value is IFunArray arr)
            {
                return new VQTArray(arr.ToArray())
                {
                    Q = q,
                    T = t
                };
            }
            else
            {
                return new PrimitiveVQT(value)
                {
                    Q = q,
                    T = t
                };
            }
        }
    }
}