namespace Nfun.Fuspec.Parser.Model
{
    public class Param
    {
        public string Value { get; }
        public string VarType { get; }

        public Param(string value, string varType)
        {
            Value = value;
            VarType = varType;
        }
    }
}