using System;
using System.Collections.Generic;
using NFun.Types;

namespace Nfun.Fuspec.Parser.Model
{
    public class SetCheckPair
    {
        readonly List<VarVal> _set;
        readonly List<VarVal> _check;

        public VarVal[] Set => _set.ToArray();

        public VarVal[] Check => _check.ToArray();

        public SetCheckPair()
        {
            _set = new List<VarVal>();
            _check = new List<VarVal>();
        }

        internal void AddSet(IEnumerable<VarVal> set) => _set.AddRange(set);

        internal void AddGet(IEnumerable<VarVal> get) => _check.AddRange(get);
    }
}