using System.Linq;
using Nfun.Fuspec.Parser.FuspecParserErrors;

namespace Nfun.Fuspec.Parser.Model
{
    public class FuspecTestCase
    {
        public FuspecTestCase(string name, string[] tags, string body )
        {
            Name = name;
            Tags = tags;
            Script = body;
        }
        public string Name { get; }
        public string[] Tags { get; }
        public string Script{ get; }
    }
    

}