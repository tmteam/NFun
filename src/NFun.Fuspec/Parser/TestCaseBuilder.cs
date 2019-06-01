using System.Collections.Generic;
using Nfun.Fuspec.Parser.FuspecParserErrors;
using Nfun.Fuspec.Parser.Model;

namespace Nfun.Fuspec.Parser
{
    public class TestCaseBuilder  
    {
        public string Name { get; set; }
        public List<string> Tags { get; set; }
        public string Script { get; set; }
        
        public List<string> ParamsIm { get; set; }
        public List<string> ParamsOut { get; set; }
        
        public TestCaseBuilder()
        {
            Script = "";
            Tags = new List<string>();
        }

        public FuspecTestCase Build()=> new FuspecTestCase(Name, Tags.ToArray(), Script);
    }
}