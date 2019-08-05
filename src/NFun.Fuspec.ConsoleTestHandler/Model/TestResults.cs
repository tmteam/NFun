namespace NFun.Fuspec.ConsoleTestHandler
{
    public class TestResults
    {
        public TestResults(int totalTestsCount, int totalExceptionsCount, int totalGoodResult, double averageTime)
        {
            TotalTestsCount = totalTestsCount;
            TotalExceptionsCount = totalExceptionsCount;
            TotalGoodResult = totalGoodResult;
            AverageTime = averageTime;
        }

        public int TotalTestsCount { get; }
        public int TotalExceptionsCount { get; }
        public int TotalGoodResult { get; }
        public double AverageTime { get; }
    }
}