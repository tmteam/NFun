using System.Linq;
using Nfun.Fuspec.Parser.FuspecParserErrors;

namespace Nfun.Fuspec.Parser.Model
{
    public class FuspecTestCase
    {
        public FuspecTestCase(  string name, 
                                string[] tags, 
                                string body,
                                Param[] paramsIn, Param[] paramsOut, 
                                SetCheckKit[] setCheckKits )
        {
            Name = name;
            Tags = tags;
            Script = body;
            ParamsIn = paramsIn;
            ParamsOut = paramsOut;
            SetCheckKits = setCheckKits;
        }
        public string Name { get; }
        public string[] Tags { get; }
        public string Script{ get; }
        
        public Param[] ParamsIn { get; }
        public Param[] ParamsOut { get; }
        
        public SetCheckKit[] SetCheckKits { get; }

    }
    

}