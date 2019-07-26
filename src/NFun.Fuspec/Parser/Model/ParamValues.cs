using System.Collections.Generic;

namespace Nfun.Fuspec.Parser.Model
{
    public class ParamValues
    {
        public Dictionary<Param, string> SetKit;
        public Dictionary<Param, string> CheckKit;

        public ParamValues()
        {
            SetKit=new Dictionary<Param, string>();
            CheckKit = new Dictionary<Param, string>();
        }
    }
}