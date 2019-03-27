using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Funny.Types
{
    public enum BaseVarType
    {
        Empty = 0,
        Bool =    1,
        Int =     2,
        Real =    3,
        Text =    4,
        Any =5,
        ArrayOf = 6,
        Fun = 7,
        Generic = 8,
    }
}