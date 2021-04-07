using ParcerV1;

namespace NFun.Fuspec.Parser.FuspecParserErrors
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
                case FuspecErrorType.SeparatedStringMissed:
                    return "SeparatedStringMissed";
                case FuspecErrorType.NFunMessage_ICantParseValue:
                    return "NFunMessage_ICantParseValue";
                case FuspecErrorType.NFunMessage_ICantParseParamTypeString:
                    return "NFunMessage_ICantParseParamTypeString=";
                case FuspecErrorType.WrongSetCheckKit:
                    return "WrongSetCheckKit";
                case FuspecErrorType.SetOrCheckKitMissed:
                    return "SetOrCheckKitMissed";
                case FuspecErrorType.SetKitMissed:
                    return "SetKitMissed";
                case FuspecErrorType.CheckKitMissed:
                    return "CheckKitMissed";
                case FuspecErrorType.ExpectedOpeningLine:
                    return "ExpectedOpeningLine";
                default:
                    return "UnknownError!";
            }
        }
    }
}