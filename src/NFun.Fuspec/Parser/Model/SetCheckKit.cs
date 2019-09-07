using System;
using System.Collections.Generic;

namespace Nfun.Fuspec.Parser.Model
{
    public class SetCheckKit
    {
        public List<Value> Set { get; private set; }
        public List<Value> Check { get; private set; }

        public SetCheckKit()
        {
            Set = new List<Value>();
            Check=new List<Value>();
        }

        public void AddSet(List<Value> set)
        {
            Set.AddRange(set); 
        }

        public void AddGet(List<Value> get)
        {
            Check.AddRange(get);
        }

        public void RefreshCheck(List<Value> check)
        {
            Check = check;
        }
        
        
    }
}