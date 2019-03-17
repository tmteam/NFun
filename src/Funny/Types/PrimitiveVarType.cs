using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Funny.Types
{
    public enum PrimitiveVarType
    {
        Bool = 1,
        Int = 2,
        Real = 3,
        Text = 4,
        ArrayOf = 5,
    }
    public static class VarTypeExtensions{
        public static Func<IEnumerable<object>,IEnumerable> GetArrayCaster(this VarType type)
        {
            switch (type.BaseType)
            {
                case PrimitiveVarType.Bool:
                    return Enumerable.Cast<bool>;
                case PrimitiveVarType.Int:
                    return Enumerable.Cast<int>;
                case PrimitiveVarType.Real:
                    return Enumerable.Cast<double>;
                case PrimitiveVarType.Text:
                    return Enumerable.Cast<string>;
                case PrimitiveVarType.ArrayOf:
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