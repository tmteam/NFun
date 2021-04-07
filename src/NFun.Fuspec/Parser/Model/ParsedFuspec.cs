using NFun.Fuspec.Parser.FuspecParserErrors;

namespace NFun.Fuspec.Parser.Model
{
    internal class ParsedFuspec
    {
        public FuspecTestCase[] FuspecTestCases { get; }

        public ParsedFuspec(FuspecTestCases fuspecTests)
        {
            if (fuspecTests.HasErrors)
                throw new FuspecParserException( fuspecTests.Errors);
            FuspecTestCases = fuspecTests.TestCases;
        }
    }
}