using System.Collections.Generic;
using System.Linq;
using Nfun.Fuspec.Parser.FuspecParserErrors;
using Nfun.Fuspec.Parser.Model;

namespace Nfun.Fuspec.Parser
{
    class TestCaseBuilder
    {
        private List<SetCheckPair> _setCheckKits;
        public string Name { get; set; }
        public string[] Tags { get; set; } //
        public string Script { get; set; }
        
        public IdType[] ParamsIn { get; set; }
        
        public IdType[] ParamsOut { get; set; }

        public SetCheckPair[] SetCheckKits => _setCheckKits.ToArray();
        
        
        public TestCaseBuilder()
        {
            Script = null;
            Tags = new string[0];
            ParamsIn = new IdType[0];
            ParamsOut=new IdType[0];
            _setCheckKits=new List<SetCheckPair>();
        }

        internal FuspecTestCase Build() =>
            new FuspecTestCase(Name, Tags, Script, ParamsIn, ParamsOut,SetCheckKits);
        
        internal  void BuildSetCheckKits(List<SetCheckPair> setCheckKits)
        {
            if (setCheckKits.Any())
                _setCheckKits=setCheckKits;
        }
        
        internal void BuildScript(List<string> script)
        {
            Script = "";
            foreach (var strScript in script)
                Script = Script + strScript + "\r\n";
            Script = Script.Substring(0, Script.Length - 2);
        }
    }
}