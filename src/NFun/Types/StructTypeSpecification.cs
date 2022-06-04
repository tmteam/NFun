using System;
using System.Collections.Generic;

namespace NFun.Types; 

internal class StructTypeSpecification: Dictionary<string, FunnyType>{
    public StructTypeSpecification(int capacity)
        :base(capacity,StringComparer.InvariantCultureIgnoreCase) {}
}