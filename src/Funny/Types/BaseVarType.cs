using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Funny.Types
{
    public enum BaseVarType
    {
        Bool = 1,
        Int = 2,
        Real = 3,
        Text = 4,
        ArrayOf = 5,
        Any
    }
    public static class VarTypeExtensions{
        public static Func<IEnumerable<object>,IEnumerable> GetArrayCaster(this VarType type)
        {
            switch (type.BaseType)
            {
                case BaseVarType.Bool:
                    return Enumerable.Cast<bool>;
                case BaseVarType.Int:
                    return Enumerable.Cast<int>;
                case BaseVarType.Real:
                    return Enumerable.Cast<double>;
                case BaseVarType.Text:
                    return Enumerable.Cast<string>;
                case BaseVarType.ArrayOf:
                    throw new ArgumentOutOfRangeException();
                
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
    /*
     * |Primitive
     *     - bool
     *     - int
     *     - real
     *     - text
     * | array
     *     - Type
     * | Any
     */
}