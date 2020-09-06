using System;
using NFun.Tic.SolvingStates;

namespace NFun.Runtime.Arrays
{
    public static class ArrayTools
    {
        public static readonly ImmutableFunArray Empty = new ImmutableFunArray(new object[0]);

        public static IFunArray SliceToImmutable(this System.Array array, int? startIndex, int? endIndex, int? step)
        {
            if (array.Length == 0)
                return new ImmutableFunArray(array);
            
            var start = startIndex ?? 0;
            if(start > array.Length-1) 
                return Empty;
            
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
            return new ImmutableFunArray(newArr);
        }   
    }
}