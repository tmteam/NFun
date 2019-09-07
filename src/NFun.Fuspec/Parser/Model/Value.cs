using System.Threading.Tasks;
using NFun.Types;

namespace Nfun.Fuspec.Parser.Model
{
    public class Value
    {
        public string IdName { get;  }
        public object IdValue { get; }
        public VarType IdType { get; }

        public Value(string idName, object idValue,VarType idType)
        {
            IdName = idName;
            IdValue = idValue;
            IdType = idType;
        }
    }
    
    
}