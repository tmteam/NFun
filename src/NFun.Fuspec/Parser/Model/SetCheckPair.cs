using System;
using System.Collections.Generic;
using NFun.Types;

namespace Nfun.Fuspec.Parser.Model
{
    public class SetCheckPair
    {
        private List<VarVal> _set;
        private List<VarVal> _check;

        public VarVal[] Set
        {
            get { return _set.ToArray(); }
        }

        public VarVal[] Check
        {
            get{return _check.ToArray();}
        }

        public SetCheckPair()
        {
            _set = new List<VarVal>();
            _check=new List<VarVal>();
        }

        internal void AddSet(VarVal[] set)
        {
            _set.AddRange(set); 
        }

        internal void AddGet(VarVal[] get)
        {
            _check.AddRange(get);
        }
    }
}