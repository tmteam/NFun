using System;

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

        public string SubString(string origin)
        {
            if(Finish==-1 || Start==-1)
                return String.Empty;
            if(Start>Finish)
                return String.Empty;
            return origin.Substring(Start, Finish - Start);
        }
    }
}