using NFun.Fuspec.Parser.Interfaces;
using NFun.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nfun.Fuspec.Parser.Model
{
    public class SetData: ISetCheckData
    {
        readonly List<VarVal> _set = new List<VarVal>();
        public VarVal[] ValuesKit => _set.ToArray();
      
        public void AddValue(IEnumerable<VarVal> value) => _set.AddRange(value);
    }
}
