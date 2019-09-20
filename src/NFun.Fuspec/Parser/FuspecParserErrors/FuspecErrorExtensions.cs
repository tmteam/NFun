using ParcerV1;

namespace Nfun.Fuspec.Parser.FuspecParserErrors
{
    public static class FuspecErrorTypeExtensions
    {
        public static string ToErrorString(this FuspecErrorType type)
        {
            switch (type)
            {
                case FuspecErrorType.NamedMissed:
                    return "NamedMissed";
                case FuspecErrorType.ScriptMissed:
                    return "ScriptMisssed";
                case FuspecErrorType.EndingHeadMissed:
                    return "EndingHeadMissed";
                case FuspecErrorType.OpeningStringMissed:
                    return "OpeningStringMissed";
                case FuspecErrorType.NoEndingTestCase:
                    return "NoEndingTestCase";
                case FuspecErrorType.ParamInMissed:
                    return "ParamInMissed";
                case FuspecErrorType.ParamOutMissed:
                    return "ParamOutMissed";
                default:
                    return "UnknownError!";
            }
        }
    }
}