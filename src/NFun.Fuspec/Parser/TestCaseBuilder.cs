using System.Collections.Generic;
using Nfun.Fuspec.Parser.FuspecParserErrors;
using Nfun.Fuspec.Parser.Model;

namespace Nfun.Fuspec.Parser
{
    class TestCaseBuilder  
    {
        public string Name { get; set; }
        public string[] Tags { get; set; } //
        public string Script { get; set; }
        
        public IdType[] ParamsIn { get; set; }
        
        public IdType[] ParamsOut { get; set; }
        
        public SetCheckPair[] SetCheckKits { get; set; }
        
        
        public TestCaseBuilder()
        {
            Script = null;
            Tags = new string[0];
            ParamsIn = new IdType[0];
            ParamsOut=new IdType[0];
            SetCheckKits=new SetCheckPair[0];
        }

        internal FuspecTestCase Build() =>
            new FuspecTestCase(Name, Tags, Script, ParamsIn, ParamsOut,SetCheckKits);
    }
}