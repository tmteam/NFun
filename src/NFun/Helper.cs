using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace NFun; 

internal static class Helper {
    public static bool DoesItLooksLikeSuperAnonymousVariable(string id, out int num) {
        num = -1;
        if (id.Length < 2)
            return false;
        if (id[0] != 'i' && id[0] != 'I')
            return false;
        if (id[1] != 't' && id[1] != 'T')
            return false;
        
        if (id.Length == 2) {
            num = -1;
            return true;
        }
        
        num = id[2] switch {
                  '0' => 0,
                  '1' => 1,
                  '2' => 2,
                  '3' => 3,
                  '4' => 4,
                  '5' => 5,
                  '6' => 6,
                  '7' => 7,
                  '8' => 8,
                  '9' => 9,
                  _   => -1
              };

        return num != -1;
    }
    public static bool DoesItLooksLikeSuperAnonymousVariable(string id) => DoesItLooksLikeSuperAnonymousVariable(id, out _);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty<TIn>(this IEnumerable<TIn> input)  => !input.Any();
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty<TIn>(this TIn[] input) => input.Length == 0;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty<TIn>(this List<TIn> input) => input.Count == 0;

    public static int IndexOf<TIn>(this IEnumerable<TIn> input, TIn searched) {
        int index = -1;
        foreach (var value in input)
        {
            index++;
            if (value.Equals(searched))
                return index;
        }
        return -1;
    }
    
    public static TIn[] ToArray<TIn>(this IEnumerable<TIn> input, int size) {
        var ans = new TIn[size];
        var i = 0;
        foreach (var tIn in input)
        {
            ans[i] = tIn;
            i++;
        }
        return ans;
    }
    
    public static TOut[] SelectToArray<TIn, TOut>(this IEnumerable<TIn> input, int size, Func<TIn, TOut> mapFunc) {
        var ans = new TOut[size];
        var i = 0;
        foreach (var tIn in input)
        {
            ans[i] = mapFunc(tIn);
            i++;
        }
        return ans;
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
    
    public static TOut[] SelectToArray<TIn, TOut>(this IReadOnlyList<TIn> input, Func<TIn, TOut> mapFunc) {
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
    
    public static bool ValueEquals<TKey, TValue>(this Dictionary<TKey, TValue> dict1, Dictionary<TKey, TValue> dict2)
    {
        if (dict1 == dict2) return true;
        if ((dict1 == null) || (dict2 == null)) return false;
        if (dict1.Count != dict2.Count) return false;

        var valueComparer = EqualityComparer<TValue>.Default;

        foreach (var kvp in dict1)
        {
            TValue value2;
            if (!dict2.TryGetValue(kvp.Key, out value2)) return false;
            if (!valueComparer.Equals(kvp.Value, value2)) return false;
        }
        return true;
    }
}
//netstandard 2.0 tuple deconstruct helper extension
static class KvpExtensions
{
    public static void Deconstruct<TKey, TValue>(
        this KeyValuePair<TKey, TValue> kvp,
        out TKey key,
        out TValue value)
    {
        key = kvp.Key;
        value = kvp.Value;
    }
}

static class QueueExtensions
{
    public static bool TryPeek<TValue>(
        this Queue<TValue> queue,
        out TValue result)
    {
        if (queue.IsEmpty())
        {
            result = default;
            return false;
        }
        result = queue.Peek();
        return true;
    }
}
static class DictionaryExtensions
{
    public static TValue? GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key)
    {
        //perfomance hit, double key retrieving.
        if (!dictionary.ContainsKey(key))
            return default;
        return dictionary[key];
    }
    public static TValue GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
    {
        //perfomance hit, double key retrieving.
        if (!dictionary.ContainsKey(key))
            return defaultValue;
        return dictionary[key];
    }
}