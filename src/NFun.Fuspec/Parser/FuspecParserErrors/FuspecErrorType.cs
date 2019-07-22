namespace ParcerV1
{
    public enum FuspecErrorType
    {
        Unknown = 0,
        OpeningStringMissed = 1,
        NamedMissed = 10,
        EndingHeadMissed = 20,
        SeparatedStringMissed=21,
        ScriptMissed = 30,
        NoEndingTestCase = 40,
        ParamInMissed=50,
        ParamOutMissed=60,
        WrongParamType=70
    }
}