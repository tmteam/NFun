using System;
using System.Collections;
using System.Collections.Generic;
using NFun.Runtime;

namespace NFun.Types
{
    public static class TypeHelper
    {
        public static string GetFunSignature<T>(string name, T returnType, IEnumerable<T> arguments)
            => name + "(" + string.Join(",", arguments) + "):" + returnType;
        public static string GetFunSignature<T>(T returnType, IEnumerable<T> arguments)
            => "(" + string.Join(",", arguments) + "):" + returnType;
        public static object Unbox(this object o)
        {
            if (o is IFunConvertable f)
                return f.GetValue();
            return o;
        }

        public static string GetTextOrThrow(object obj)
        {
            var e = (IFunArray)obj;
            if (e is TextFunArray t)
                return t.Text;
            if (e is FunArray f)
                return new string((char[])f.Values);
            char[] result = new char[e.Count];
            for (int i = 0; i < e.Count; i++)
            {
                result[i] = (char)e.GetElementOrNull(i);
            }
            return new string(result);
        }
        public static string GetTextOrThrow(this object[] arr, int index)
        {
            var e = (IFunArray)arr[index];
            if (e is TextFunArray t)
                return t.Text;
            if (e is FunArray f)
                return new string((char[]) f.Values);
            char [] result = new char[e.Count];
            for (int i = 0; i < e.Count; i++)
            {
                result[i] = (char) e.GetElementOrNull(i);
            }
            return new string(result);
        }
        public static T Get<T>(this object[] arr, int index)
        {
            return arr[index].To<T>();
        }
        public static T To<T>(this object o)
        {
            if (o is IFunConvertable f)
                return f.GetOrThrowValue<T>();
            return (T) o;
        }

        public static bool AreEquivalent(IFunArray a, IFunArray b)
        {
            if (a.Count != b.Count)
                return false;
            if (a.Count == 0)
                return true;

            if (a.GetElementOrNull(0) is IFunArray)
            {
                for (int i = 0; i < a.Count; i++)
                {
                    var foreign = b.GetElementOrNull(i);
                    var origin = a.GetElementOrNull(i);
                    if (foreign is IFunArray f)
                    {
                        if (origin is IFunArray o)
                        {
                            if (!f.IsEquivalent(o))
                                return false;
                        }
                        else return false;
                    }
                    else if (!foreign.Equals(origin))
                        return false;
                }
                return true;
            }
            else
            {
                for (int i = 0; i < b.Count; i++)
                {
                    if (!AreEqual(a.GetElementOrNull(i), b.GetElementOrNull(i)))
                        return false;
                }
                return true;
            }
        }
        public static bool AreEqual(object left, object right)
        {
            left = left.Unbox();
            right = right.Unbox();
            if (left is IFunArray le)
            {
                if (!(right is IFunArray re))
                    return false;
                return AreEquivalent(le,re);
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