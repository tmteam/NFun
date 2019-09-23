using System.Collections.Generic;
using Nfun.Fuspec.Parser.FuspecParserErrors;
using Nfun.Fuspec.Parser.Model;

namespace Nfun.Fuspec.Parser
{
    class TestCaseBuilder  
    {
        public string Name { get; set; }
        public List<string> Tags { get; set; } //
        public string Script { get; set; }
        
        //todo cr: public properties has to be immutable
        //Use T[] or IEnumerable<T> instead

        public List<IdType> ParamsIn { get; set; }
        
        public List<IdType> ParamsOut { get; set; }
        //todo cr: remove not used
    //    public ParamValues SetCheckKit { get; set; }
        
        public List<SetCheckPair> SetCheckKits { get; set; }
        
        
        public TestCaseBuilder()
        {
            Script = null;
            Tags = new List<string>();
            ParamsIn = new List<IdType>();
            ParamsOut=new List<IdType>();
            //todo cr: remove not used
          //  SetCheckKit=new ParamValues();
            SetCheckKits=new List<SetCheckPair>();
        }

        internal FuspecTestCase Build() =>
            new FuspecTestCase(Name, Tags.ToArray(), Script, ParamsIn.ToArray(), ParamsOut.ToArray(),SetCheckKits.ToArray());
    }
}