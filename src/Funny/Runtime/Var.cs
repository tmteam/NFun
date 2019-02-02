namespace Funny.Runtime
{
    public struct Var
    {
        public static Var New(string name, double value) 
            => new Var(name, value);
        public readonly string Name;
        public readonly double Value;

        public Var(string name, double value)
        {
            Name = name;
            Value = value;
        }
    }
}