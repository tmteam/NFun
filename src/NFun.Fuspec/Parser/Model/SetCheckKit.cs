using System.Collections.Generic;

namespace Nfun.Fuspec.Parser.Model
{
    public class SetCheckKit
    {
        public List<Value> Set { get; private set; }
        public List<Value> Get { get; private set; }

        public SetCheckKit()
        {
            Set = new List<Value>();
            Get=new List<Value>();
        }

        public void AddSet(List<Value> set)
        {
            Set.AddRange(set); 
        }

        public void AddGet(Value get)
        {
            Get.Add(get);
        }
        
        
    }
}