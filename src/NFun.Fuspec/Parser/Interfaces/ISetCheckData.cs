using NFun.Types;
using System.Collections.Generic;

namespace NFun.Fuspec.Parser.Interfaces
{
     public interface ISetCheckData
    {
        VarVal[] ValuesKit { get; }

        void AddValue(IEnumerable<VarVal> value);
    }
}
