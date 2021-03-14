using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using NFun.Exceptions;
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
                typeof(object),
                null,
            };
            
            DefaultPrimitiveValues = new[]
            {
                null,
                default(char),
                default(bool),
                default(byte),
                default(ushort),
                default(uint),
                default(ulong),
                default(short),
                default(int),
                default(long),
                default(double),
                null,
                null,
                null,
                new object(),
                null,
            };
        }

        private static readonly object[] DefaultPrimitiveValues;
        private static readonly Type[] FunToClrTypesMap;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type GetClrType (this BaseVarType varType) => FunToClrTypesMap[(int) varType];
        public static string GetFunSignature<T>(string name, T returnType, IEnumerable<T> arguments)
            => name + "(" + string.Join(",", arguments) + "):" + returnType;
        public static string GetFunSignature<T>(T returnType, IEnumerable<T> arguments)
            => "(" + string.Join(",", arguments) + "):" + returnType;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string GetFunText(object obj)
        {
            if (obj is IFunArray funArray)
                return funArray.ToText();
            if (obj is double dbl)
                return dbl.ToString(CultureInfo.InvariantCulture);
            return obj.ToString();
        }

        public static object GetDefaultValueOrNull(this VarType type)
        {
            var defaultValue  =  DefaultPrimitiveValues[(int) type.BaseType];
            if (defaultValue != null)
                return defaultValue;
            if (type.ArrayTypeSpecification!=null)
            {
                var arr = type.ArrayTypeSpecification;
                if (arr.VarType.BaseType == BaseVarType.Char)
                    return TextFunArray.Empty;
                return new ImmutableFunArray(new object[0], arr.VarType);
            }

            return null;
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