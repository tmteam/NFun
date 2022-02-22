using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace NFun {

internal static class Helper {
    public static bool DoesItLooksLikeSuperAnonymousVariable(string id) {
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
    public static TOut[] SelectToArray<TIn, TOut>(this TIn[] input, Func<TIn, TOut> mapFunc) {
        var ans = new TOut[input.Length];
        for (var i = 0; i < input.Length; i++)
            ans[i] = mapFunc(input[i]);
        return ans;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TOut[] SelectToArray<TIn, TOut>(this IList<TIn> input, Func<TIn, TOut> mapFunc) {
        var ans = new TOut[input.Count];
        for (var i = 0; i < input.Count; i++)
        {
            ans[i] = mapFunc(input[i]);
        }

        return ans;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TOut[] SelectToArrayAndAppendTail<TIn, TOut>(this TIn[] input, TOut tail, Func<TIn, TOut> mapFunc) {
        var ans = new TOut[input.Length + 1];
        for (var i = 0; i < input.Length; i++)
        {
            ans[i] = mapFunc(input[i]);
        }

        ans[input.Length] = tail;
        return ans;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TOut[] AppendTail<TOut>(this TOut[] input, TOut tail) {
        if (input.Length == 0)
            return new[] { tail };
        var ans = new TOut[input.Length + 1];
        Array.Copy(input, ans, input.Length);
        ans[input.Length] = tail;
        return ans;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TOut[] AppendTail<TOut>(this TOut[] input, TOut[] tail) {
        if (input.Length == 0)
            return  tail;
        
        var ans = new TOut[input.Length + tail.Length];
        Array.Copy(input, ans, input.Length);
        Array.Copy(tail,0, ans, input.Length, tail.Length);
        return ans;
    }
}

}