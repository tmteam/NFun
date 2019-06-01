using System;
using System.Linq;
using ParcerV1;

namespace Nfun.Fuspec.Parser
{
    public static class FuspecParserHelper
    {
        public static string FindKeyWord(String findingkey, string str)
        {
            if (str.Length < findingkey.Length)
                return null;
            string key = str.Substring(0, findingkey.Length);
            if (key == findingkey)
                return str.Substring(findingkey.Length);
            else
                return null;
        }
        
        public static bool IsSeparatingLine(string str, char symbol)
        {
            if (str[0] != '|')
                return false;
            return str.Substring(1).All(ch => ch == symbol);
        }
    }

   

}