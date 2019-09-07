using System.Threading.Tasks;
using NFun.Types;

namespace Nfun.Fuspec.Parser.Model
{
    public class Value
    {
        public string IdName { get;  }
        public string IdValue { get; }

        public Value(string idName, string idValue)
        {
            IdName = idName;
            IdValue = idValue;
        }
    }
    
    
}