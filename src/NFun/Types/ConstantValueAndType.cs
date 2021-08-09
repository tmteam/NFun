namespace NFun.Types
{
    internal readonly struct ConstantValueAndType
    {
        public readonly object FunnyValue;
        public readonly FunnyType Type;

        public ConstantValueAndType(object funnyValue, FunnyType type)
        {
            FunnyValue = funnyValue;
            Type = type;
        }

        public override string ToString() => $"Constant {FunnyValue} of {Type}";
    }
}