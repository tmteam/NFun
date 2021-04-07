using ParcerV1;

namespace NFun.Fuspec.Parser.FuspecParserErrors
{
    public class FuspecParserError
    {
        public FuspecErrorType ErrorType { get; }
        public int LineNumber { get; }

        public FuspecParserError(FuspecErrorType fuspecErrorType, int lineNumber)
        {
           ErrorType = fuspecErrorType;
            LineNumber = lineNumber;
        }
    }
}