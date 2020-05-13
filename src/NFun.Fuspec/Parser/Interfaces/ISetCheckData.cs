using NFun.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace NFun.Fuspec.Parser.Interfaces
{
     public interface ISetCheckData
    {
        VarVal[] ValuesKit { get; }

        void AddValue(IEnumerable<VarVal> value);
    }
}
