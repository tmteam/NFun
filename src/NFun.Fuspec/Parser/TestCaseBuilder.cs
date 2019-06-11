using System.Collections.Generic;
using Nfun.Fuspec.Parser.FuspecParserErrors;
using Nfun.Fuspec.Parser.Model;

namespace Nfun.Fuspec.Parser
{
    public class TestCaseBuilder  
    {
        public string Name { get; set; }
        public List<string> Tags { get; set; } //
        public string Script { get; set; }
        
        public List<Param> ParamsIn { get; set; }
        
        public List<Param> ParamsOut { get; set; }
        public TestCaseBuilder()
        {
            Script = null;
            Tags = new List<string>();
            ParamsIn = new List<Param>();
            ParamsOut=new List<Param>();
        }

        public FuspecTestCase Build() =>
            new FuspecTestCase(Name, Tags.ToArray(), Script, ParamsIn.ToArray(), ParamsOut.ToArray());
    }
}