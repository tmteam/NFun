using System;

namespace NFun.Tokenization; 

public readonly struct Interval {
    public static Interval Empty => new(0, 0);
    public static Interval Position(int position) => new(position, position);
    
    public readonly int Start;
    public readonly int Finish;
    
    public Interval(int start, int finish) {
#if DEBUG
        if (start > finish)
            throw new InvalidOperationException("Start is greater then finish");
#endif

        Start = start;
        Finish = finish;
    }

    public string SubString(string origin) {
        if (Finish == -1 || Start == -1)
            return String.Empty;
        if (Start > Finish)
            return String.Empty;
        return origin.Substring(Start, Finish - Start);
    }

    public Interval Append(Interval rightInterval) => new Interval(Start, rightInterval.Finish);
}