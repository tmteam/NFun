using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NFun.Tic.SolvingStates;
using NFun.Types;

namespace NFun.Runtime.Arrays
{
    public static class ArrayTools
    {
        public static readonly ImmutableFunArray Empty = new ImmutableFunArray(new object[0], VarType.Anything);
        public static TextFunArray AsFunText(this  string txt)=> new TextFunArray(txt);
        public static string JoinElementsToFunString(IEnumerable enumerable)
        {
            var sb = new StringBuilder("[");
            bool first = true;
            foreach (var item in enumerable)
            {
                if (!first)
                {
                    sb.Append(",");
                }
                first = false;
                sb.Append(TypeHelper.GetFunText(item));
            }
            sb.Append("]");
            return sb.ToString();
        }
        public static string JoinElementsToFunString(IEnumerable<object> enumerable)
        {
            bool allAreChars = true;
            int count = 0;
            foreach (var item in enumerable)
            {
                count++;
                if (!(item is char)) 
                    allAreChars = false;
            }
            if (allAreChars)
            {
                var chars = new char[count];
                int i = 0;
                foreach (var item in enumerable)
                {
                    chars[i] = (char) item;
                }   
                return new string(chars);
            }
            var sb = new StringBuilder("[");
            bool first = true;
            foreach (var item in enumerable)
            {
                if (!first)
                {
                    sb.Append(",");
                }
                first = false;
                sb.Append(TypeHelper.GetFunText(item));
            }
            sb.Append("]");
            return sb.ToString();
        }
        public static IFunArray SliceToImmutable(
            Array array,
            VarType elementType,
            int? startIndex, int? endIndex, int? step)
        {
            if (array.Length == 0)
                return new ImmutableFunArray(array,elementType);
            
            var start = startIndex ?? 0;
            if(start > array.Length-1) 
                return new ImmutableFunArray(new Object[0], elementType);
            
            var end = array.Length - 1;
            if(endIndex.HasValue)
                end = endIndex.Value >= array.Length ? array.Length - 1 : endIndex.Value;
            object[] newArr;
            if (step == null || step == 1)
            {
                var size = end - start + 1;
                newArr = new object[size];
                System.Array.Copy(array, start, newArr,  0, size);
            }
            else
            {    
                var size = (int)Math.Floor((end - start) / (double)step)+1;
                newArr = new object[size];
                for (int i = start, index = 0; i <= end; i+= step.Value, index++)
                    newArr[index] = array.GetValue(i);
            }
            return new ImmutableFunArray(newArr,elementType);
        }   
    }
}