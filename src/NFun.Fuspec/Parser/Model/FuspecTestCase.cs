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
                                SetCheckPair[] setChecks,
                                bool isTestExecuted,
                                int startLine)
        {
            Name = name;
            Tags = tags;
            Script = body;
            InputVarList = inputVarList;
            OutputVarList = outputVarList;
            SetChecks = setChecks;
            IsTestExecuted = isTestExecuted;
            StartLine = startLine;
        }
        public string Name { get; }
        public string[] Tags { get; }
        public string Script{ get; }
        
        public IdType[] InputVarList { get; }
        public IdType[] OutputVarList { get; }
        
        public SetCheckPair[] SetChecks { get; }
        
        public bool IsTestExecuted { get; }
        
        public int StartLine { get; }

    }
    

}