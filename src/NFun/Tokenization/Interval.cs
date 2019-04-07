namespace NFun.Tokenization
{
    public struct Interval
    {
        public readonly int Start;
        public readonly int Finish;

        public Interval(int start, int finish)
        {
            Start = start;
            Finish = finish;
        }
    }
}