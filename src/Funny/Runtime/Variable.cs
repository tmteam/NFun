namespace Funny.Runtime
{
    public struct Variable
    {
        public static Variable New(string name, double value) 
            => new Variable(name, value);
        public readonly string Name;
        public readonly double Value;

        public Variable(string name, double value)
        {
            Name = name;
            Value = value;
        }
    }
}