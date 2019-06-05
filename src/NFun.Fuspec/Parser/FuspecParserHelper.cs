using System;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
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
        //    if (separatingString != null)
            
            int i = 0;
            if (str[0] != '|')
                return false;
            foreach (var ch in str.Substring(1))
            {
                if (ch != '*') return false;
                i++;
            }

            if (i < 8 ) return false;
            return true;
            //            return str.Substring(1).All(ch => ch == symbol);
        }
    }

   

}