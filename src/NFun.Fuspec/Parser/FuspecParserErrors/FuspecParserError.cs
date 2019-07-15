using Nfun.Fuspec.Parser.Model;
using ParcerV1;

namespace Nfun.Fuspec.Parser.FuspecParserErrors
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