using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using Nfun.Fuspec.Parser.Model;
using NFun.Tokenization;
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
                if (ch != symbol) return false;
                i++;
            }

            if (i < 8) return false;
            return true;
            //            return str.Substring(1).All(ch => ch == symbol);
        }

        public static List<Param> GetPAram(string paramString)
        {
            string value;
            string varType;
            var tokFLow = Tokenizer.ToFlow(paramString);
            List<Param> result = new List<Param>();

            if (tokFLow.Peek == null)
                return null;

            while (tokFLow.Current.Type != TokType.Eof)
            {
                if (tokFLow.Peek == null)
                    return null;
                tokFLow.MoveNext();
                if ((tokFLow.Previous.Type != TokType.Id ||
                     tokFLow.Current.Type != TokType.Colon) ||
                    (tokFLow.Peek == null))
                    return null;
                value = tokFLow.Previous.Value;

                tokFLow.MoveNext();


                varType = tokFLow.ReadVarType().ToString();

                foreach (var res in result)
                {
                    if (res.Value == value)
                        return null;
                }

                result.Add(new Param(value, varType));

                if (tokFLow.Current.Type == TokType.Sep)
                    tokFLow.MoveNext();

            }

            if (!result.Any())
                return null;
            return result;
        }

        public static List<string> SplitWithTrim(string str, char ch)
        {
            List<string> res = new List<string>();
            var splittedString = str.Split(ch);
            foreach (var s in splittedString)
            {
                if (s.Trim() != "")
                    res.Add(s.Trim());
            }

            return res;
        }

    }
    
   

}

   

