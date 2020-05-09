using System;
using System.Collections.Generic;
using System.Text;

namespace NFun.TypeInferenceCalculator
{
    public static class CollectionHelper
    {
        public static T GetOrEnlarge<T>(this List<T> list, int index)
        {
            while (list.Count<=index) 
                list.Add(default);

            return list[index];
        }
    }
}
