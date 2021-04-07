namespace NFun.Fuspec.Parser
{
    enum TestCaseParseState
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