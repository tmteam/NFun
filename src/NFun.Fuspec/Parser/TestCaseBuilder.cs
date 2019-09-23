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
        
        public List<IdType> ParamsIn { get; set; }
        
        public List<IdType> ParamsOut { get; set; }
        
    //    public ParamValues SetCheckKit { get; set; }
        
        public List<SetCheckPair> SetCheckKits { get; set; }
        
        
        public TestCaseBuilder()
        {
            Script = null;
            Tags = new List<string>();
            ParamsIn = new List<IdType>();
            ParamsOut=new List<IdType>();
          //  SetCheckKit=new ParamValues();
            SetCheckKits=new List<SetCheckPair>();
        }

        public FuspecTestCase Build() =>
            new FuspecTestCase(Name, Tags.ToArray(), Script, ParamsIn.ToArray(), ParamsOut.ToArray(),SetCheckKits.ToArray());
    }
}