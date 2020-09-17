using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using NFun.Runtime.Arrays;

namespace NFun.Types
{
    public static class TypeHelper
    {
        static TypeHelper()
        {
            FunToClrTypesMap = new[]
            {
                null,
                typeof(char),
                typeof(bool),
                typeof(byte),
                typeof(ushort),
                typeof(uint),
                typeof(ulong),
                typeof(short),
                typeof(int),
                typeof(long),
                typeof(double),
                null,
                null,
                null,
                typeof(object)
            };
        }
        private static readonly Type[] FunToClrTypesMap;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type GetClrType (this BaseVarType varType) => FunToClrTypesMap[(int) varType];

        public static string GetFunSignature<T>(string name, T returnType, IEnumerable<T> arguments)
            => name + "(" + string.Join(",", arguments) + "):" + returnType;
        public static string GetFunSignature<T>(T returnType, IEnumerable<T> arguments)
            => "(" + string.Join(",", arguments) + "):" + returnType;

        public static IEnumerable<string> GetListOfStringOrThrow(this object[] arr, int index) => ((IFunArray) arr[index]).Select(TypeHelper.GetTextOrThrow);

        public static string GetTextOrThrow(object obj)
        {
            var e = (IFunArray)obj;
            if (e is TextFunArray t)
                return t.ToText();
            if (e is ImmutableFunArray f)
                return new string((char[])f.Values);
            char[] result = new char[e.Count];
            for (int i = 0; i < e.Count; i++) 
                result[i] = (char) e.GetElementOrNull(i);
            return new string(result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string GetFunText(object obj)
        {
            if (obj is IFunArray funArray)
                return funArray.ToText();
            if (obj is double dbl)
                return dbl.ToString(CultureInfo.InvariantCulture);
            return obj.ToString();
        }
        
        public static string GetTextOrThrow(this object[] arr, int index)
        {
            var e = (IFunArray)arr[index];
            if (e is TextFunArray t)
                return t.ToText();
            if (e is ImmutableFunArray f)
            {
                if(f.Values is char[] carr)
                    return new string(carr);
                var str = new char[f.Count];
                for (int i = 0; i < f.Count; i++) 
                    str[i] = (char) f.Values.GetValue(i);
                return new string(str);
            }

            char [] result = new char[e.Count];
            for (int i = 0; i < e.Count; i++)
            {
                result[i] = (char) e.GetElementOrNull(i);
            }
            return new string(result);
        }
     
        public static bool AreEqual(object left, object right)
        {
            if (left is IFunArray le)
            {
                if (!(right is IFunArray re))
                    return false;
                return AreEquivalent(le, re);
            }

            if (left.GetType() == right.GetType())
                return left.Equals(right);
            return false;
        }

        public static bool AreEquivalent(IFunArray a, IFunArray b)
        {
            if (a.Count != b.Count)
                return false;
            if (a.Count == 0)
                return true;
            for (int i = 0; i < a.Count; i++)
            {
                var elementA = a.GetElementOrNull(i);
                var elementB = b.GetElementOrNull(i);
                if (!AreEqual(elementA, elementB))
                    return false;
            }

            return true;
        }

        
    }
}