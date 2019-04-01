using NFun.Runtime;

namespace NFun.Types
{
    public static class TypeHelper
    {
        public static bool AreEqual(object left, object right)
        {
            if (left is FunArray le)
            {
                if (!(right is FunArray re))
                    return false;
                return le.IsEquivalent(re);
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