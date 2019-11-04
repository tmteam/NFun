using System;
using System.IO.Enumeration;
using Nfun.Fuspec.Parser.Model;
using NFun.TypeInference.Solving;

namespace FuspecHandler
{
    public class TestCaseResult
    {
        private Exception _error;

        public string FileName { get; }
        public int StartLine => Fus.StartLine;
        public bool IsTodoTest => Fus.IsTestExecuted;
        public Exception Error => _error;
        public FuspecTestCase Fus { get; }

        
        
        public TestCaseResult(string fileName, FuspecTestCase fus)
        {
            FileName = fileName;
            Fus = fus;
            _error = null;
        }

        public void SetError(Exception e)
        {
            _error = e;
        }
 /*   
    - startLine
    - isSuccesfully
    - Exception: Error
    choise of: 
    - Funparse
    - Funruntime
    - SetCheck
    - InOut
    - Unhandled
  */  }
}