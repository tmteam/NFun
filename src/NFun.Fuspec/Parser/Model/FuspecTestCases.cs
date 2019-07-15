using System.Linq;
using Nfun.Fuspec.Parser.FuspecParserErrors;

namespace Nfun.Fuspec.Parser.Model
{
    public class FuspecTestCases
    {
        public FuspecTestCase[] TestCases { get; }
        public FuspecParserError[] Errors { get; }
        public bool HasErrors { get; }
        
        public FuspecTestCases(FuspecParserError[] errors)
        {
            if (errors.Any())
                HasErrors = true;
            Errors = errors;
            TestCases=new FuspecTestCase[0];
        }

        public FuspecTestCases(FuspecTestCase[] fuspecTestCases)
        {
            HasErrors = false;
            Errors = new FuspecParserError[0];
            TestCases = fuspecTestCases;
        }
        
        
    }
}