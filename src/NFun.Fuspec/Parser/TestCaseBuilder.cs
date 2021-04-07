using System.Collections.Generic;
using System.Linq;
using NFun.Fuspec.Parser.Interfaces;
using NFun.Fuspec.Parser.Model;
using NFun.Types;

namespace NFun.Fuspec.Parser
{
    class TestCaseBuilder
    {
        private List<ISetCheckData> _setChecks;
        public string Name { get; set; }
        public string[] Tags { get; set; } 
        public string Script { get; set; }
        
        public VarInfo[] ParamsIn { get; set; }
        
        public VarInfo[] ParamsOut { get; set; }
        
        public bool IsTestExecuted { get; set; }
        
        public int StartLine { get; set; }

        public ISetCheckData[] SetChecks => _setChecks.ToArray();
        
        
        public TestCaseBuilder()
        {
            Script = null;
            Tags = new string[0];
            ParamsIn = new VarInfo[0];
            ParamsOut=new VarInfo[0];
            _setChecks=new List<ISetCheckData>();
            IsTestExecuted = true;
            StartLine = -1;
        }

        public FuspecTestCase Build() =>
            new FuspecTestCase(Name, Tags, Script, ParamsIn, ParamsOut, SetChecks, IsTestExecuted, StartLine);
        
        internal  void BuildSetCheckKits(List<ISetCheckData> setChecks)
        {
            if (setChecks.Any())
                _setChecks=setChecks;
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