using System;
using System.IO.Enumeration;
using Nfun.Fuspec.Parser.Model;
using NFun.Types;

namespace FuspecHandler
{
    public class TestCaseResult
    {
        private Exception _error;

        public readonly string FileName;
        public readonly FuspecTestCase Fus;
        public Exception Error => _error;
        private VarInfo[] Inputs;

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
    }
}