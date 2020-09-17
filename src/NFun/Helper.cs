using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace NFun
{
    static class Helper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOut[] SelectToArray<TIn, TOut>(this TIn[] input, Func<TIn, TOut> mapFunc)
        {
            TOut[] ans = new TOut[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                ans[i] = mapFunc(input[i]);
            }
            return ans;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOut[] SelectToArray<TIn, TOut>(this IList<TIn> input, Func<TIn, TOut> mapFunc)
        {
            TOut[] ans = new TOut[input.Count];
            for (int i = 0; i < input.Count; i++)
            {
                ans[i] = mapFunc(input[i]);
            }
            return ans;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOut[] SelectToArrayAndAppendTail<TIn, TOut>(this TIn[] input, TOut tail, Func<TIn, TOut> mapFunc)
        {
            TOut[] ans = new TOut[input.Length+1];
            for (int i = 0; i < input.Length; i++)
            {
                ans[i] = mapFunc(input[i]);
            }

            ans[input.Length] = tail;
            return ans;
        }
    }
}