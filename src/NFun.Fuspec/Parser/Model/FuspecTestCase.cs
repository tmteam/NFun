using System.Linq;
using Nfun.Fuspec.Parser.FuspecParserErrors;

namespace Nfun.Fuspec.Parser.Model
{
    public class FuspecTestCase
    {
        public FuspecTestCase(string name, string[] tags, string body,string[] paramsIn,string[] paramsOut )
        {
            Name = name;
            Tags = tags;
            Script = body;
            ParamsIn = paramsIn;
            ParamsOut = paramsOut;
        }
        public string Name { get; }
        public string[] Tags { get; }
        public string Script{ get; }
        
        public string[] ParamsIn { get; }
        
        public string[] ParamsOut { get; }
    }
    

}