using System.Linq;
using Nfun.Fuspec.Parser.FuspecParserErrors;

namespace Nfun.Fuspec.Parser.Model
{
    public class FuspecTestCase
    {
        public FuspecTestCase(  string name, 
                                string[] tags, 
                                string body,
                                IdType[] inputVarList, IdType[] outputVarList, 
                                SetCheckPair[] setChecks )
        {
            Name = name;
            Tags = tags;
            Script = body;
            InputVarList = inputVarList;
            OutputVarList = outputVarList;
            SetChecks = setChecks;
        }
        public string Name { get; }
        public string[] Tags { get; }
        public string Script{ get; }
        
        public IdType[] InputVarList { get; }
        public IdType[] OutputVarList { get; }
        
        public SetCheckPair[] SetChecks { get; }

    }
    

}