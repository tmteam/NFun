namespace Nfun.Fuspec.Parser
{
    public enum TestCaseParseState
    {
        ReadingName,
        ReadingTags,
        ReadingBody,
        ReadingParams,
        ReadingValues,
        FindingOpeningString,
    }
}