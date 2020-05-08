using System.Collections.Generic;
using System.Linq;
using Nfun.Fuspec.Parser.FuspecParserErrors;
using Nfun.Fuspec.Parser.Model;
using NFun.Types;

namespace Nfun.Fuspec.Parser
{
    class TestCaseBuilder
    {
        private List<SetCheckPair> _setCheckKits;
        public string Name { get; set; }
        public string[] Tags { get; set; } //
        public string Script { get; set; }
        
        public VarInfo[] ParamsIn { get; set; }
        
        public VarInfo[] ParamsOut { get; set; }
        
        public bool IsTestExecuted { get; set; }
        
        public int StartLine { get; set; }

        public SetCheckPair[] SetCheckKits => _setCheckKits.ToArray();
        
        
        public TestCaseBuilder()
        {
            Script = null;
            Tags = new string[0];
            ParamsIn = new VarInfo[0];
            ParamsOut=new VarInfo[0];
            _setCheckKits=new List<SetCheckPair>();
            IsTestExecuted = true;
            StartLine = -1;
        }

        internal FuspecTestCase Build() =>
            new FuspecTestCase(Name, Tags, Script, ParamsIn, ParamsOut,SetCheckKits, IsTestExecuted, StartLine);
        
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