namespace Nfun.Fuspec.Parser
{
    public enum TestCaseParseState
    {
        ReadingName,
        ReadingTags,
        ReadingBody,
        ReadingParamsIn,
        ReadingParamsOut,
        ReadingValues,
        FindingOpeningString,
    }
}