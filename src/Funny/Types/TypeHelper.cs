using System.Collections;
using System.Linq;

namespace Funny.Types
{
    public static class TypeHelper
    {
        public static bool AreEqual(object left, object right)
        {
            if (left is IEnumerable le)
            {
                if (!(right is IEnumerable re))
                    return false;

                var leftEnumerator = le.GetEnumerator();
                var rightEnumerator = re.GetEnumerator();

                while (leftEnumerator.MoveNext())
                {
                    if (!rightEnumerator.MoveNext())
                        return false;

                    if (!AreEqual(leftEnumerator.Current, rightEnumerator.Current))
                        return false;
                }
                return !rightEnumerator.MoveNext();
            }

            if (left.GetType() == right.GetType())
                return left.Equals(right);

            switch (left)
            {
                case double ld when right is double rd:
                    return rd == ld;
                case double ld when right is int i:
                    return ld == i;
                case double ld when right is bool b:
                    return ld != 0 == b;
                case int li when right is double rd:
                    return rd == li;
                case int li when right is int i:
                    return li == i;
                case int li when right is bool b:
                    return li != 0 == b;
                case bool lb when right is double rd:
                    return lb == (rd != 0);
                case bool lb when right is int i:
                    return lb == (i != 0);
                case bool lb when right is bool b:
                    return lb == b;
                default:
                    return left.Equals(right);
            }
        }
    }
}