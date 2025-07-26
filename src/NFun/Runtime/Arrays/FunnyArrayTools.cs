using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using NFun.Types;

namespace NFun.Runtime.Arrays;

using System.Linq;
using Tic.SolvingStates;

public static class FunnyArrayTools {
    public static TextFunnyArray AsFunText(this string txt) => new(txt);

    internal static string JoinElementsToFunString(IEnumerable enumerable) {
        var sb = new StringBuilder("[");
        bool first = true;
        foreach (var item in enumerable)
        {
            if (!first)
                sb.Append(",");

            first = false;
            sb.Append(TypeHelper.GetFunText(item));
        }

        sb.Append("]");
        return sb.ToString();
    }

    internal static string JoinElementsToFunString(IEnumerable<object> enumerable) {
        bool allAreChars = true;
        int count = 0;
        foreach (var item in enumerable)
        {
            count++;
            if (item is not char)
                allAreChars = false;
        }

        if (allAreChars)
        {
            var chars = new char[count];
            int i = 0;
            foreach (var item in enumerable)
                chars[i++] = (char)item;

            return new string(chars);
        }

        var sb = new StringBuilder("[");
        bool first = true;
        foreach (var item in enumerable)
        {
            if (!first)
                sb.Append(",");

            first = false;
            sb.Append(TypeHelper.GetFunText(item));
        }

        sb.Append("]");
        return sb.ToString();
    }

    internal static IFunnyArray CreateEnumerable(IEnumerable<object> items, FunnyType elementType) {
        if(elementType== FunnyType.Char)
        {
            var s = items as IEnumerable<char> ?? items.Select(i => (char)i);
            //todo - create TextEnumerable type
            var str = new string(s.ToArray());
            return new TextFunnyArray(str);
        }
        return new EnumerableFunnyArray(items, elementType);
    }

    internal static IFunnyArray CreateEmptyArray(FunnyType elementType) {
        if (elementType == FunnyType.Char)
            return TextFunnyArray.Empty;
        else
            return new ImmutableFunnyArray(Array.Empty<object>(), elementType);
    }

    internal static IFunnyArray CreateArray(Array values, FunnyType elementType) {
        if (elementType == FunnyType.Char)
        {
            //todo - create TextEnumerable type
            var str = new string(values.Cast<char>().ToArray());
            return new TextFunnyArray(str);
        }
        else
            return new ImmutableFunnyArray(values, elementType);
    }

    internal static IFunnyArray SliceToImmutable(
        Array array,
        FunnyType elementType,
        int? startIndex, int? endIndex, int? step) {
        if (array.Length == 0)
            return CreateArray(array, elementType);

        var start = startIndex ?? 0;
        if (start > array.Length - 1)
            return CreateEmptyArray(elementType);

        var end = array.Length - 1;
        if (endIndex.HasValue)
            end = endIndex.Value >= array.Length ? array.Length - 1 : endIndex.Value;

        object[] newArr;
        if (step == null || step == 1)
        {
            var size = end - start + 1;
            newArr = new object[size];
            System.Array.Copy(array, start, newArr, 0, size);
        }
        else
        {
            var size = (int)Math.Floor((end - start) / (double)step) + 1;
            newArr = new object[size];
            for (int i = start, index = 0; i <= end; i += step.Value, index++)
                newArr[index] = array.GetValue(i);
        }

        return CreateArray(newArr, elementType);
    }
}
