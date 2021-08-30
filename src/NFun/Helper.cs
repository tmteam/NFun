using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using NFun.Tic.SolvingStates;

namespace NFun
{
    internal static class Helper
    {
        public static bool DoesItLooksLikeSuperAnonymousVariable(string id)
        {
            if (id.Length < 2)
                return false;
            if (id[0] != 'i' && id[0] != 'I')
                return false;
            if (id[1] != 't' && id[1] != 'T')
                return false;
            if (id.Length == 2)
                return true;
            return char.IsDigit(id[2]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOut[] SelectToArray<TIn, TOut>(this TIn[] input, Func<TIn, TOut> mapFunc)
        {
            var ans = new TOut[input.Length];
            for (int i = 0; i < input.Length; i++)
                ans[i] = mapFunc(input[i]);
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