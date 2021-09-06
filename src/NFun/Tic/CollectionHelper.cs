using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace NFun.Tic {

public static class CollectionHelper {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T GetOrEnlarge<T>(this List<T> list, int index) {
        if (list.Count <= index)
        {
            list.Capacity = index + 1;
            while (list.Count <= index)
                list.Add(default);
        }

        return list[index];
    }

    public static void EnlargeAndSet<T>(this List<T> list, int index, T value) {
        if (list.Count <= index)
        {
            list.Capacity = index + 1;
            while (list.Count < index)
                list.Add(default);
            list.Add(value);
        }
        else
        {
            list[index] = value;
        }
    }
}

}