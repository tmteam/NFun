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
        //»з названи€ не пон€тно, что делает
        /*как пон€л, что ищет название фуспека.

         public static string TryFindFuspectestName(string mask, string inputString) =>
            !inputString.Contains(mask)
                ? null
                : inputString.Substring(mask.Length);

         */

        public static string FindKeyWord(string findingkey, string str)
        {
            if (str.Length < findingkey.Length)
                return null;
            string key = str.Substring(0, findingkey.Length);

            /*
             * —мотри, как тут элегантно с тернарным оператором))
             * return key == findingkey
             *              ? str.Substring(findingkey.Length)
             *              : null;
             */
            if (key == findingkey)
            {
                return str.Substring(findingkey.Length);
            }
            else
                return null;
        }


        /*
         * Ёто получаетс€ метод, который определ€ет состоит ли лини€ из вертикальной полоски и зведочек?
         * Ќадо подумать над названием, пока из дебага не запустил - не пон€л, что происходит

           public static bool IsSeparatingStarLine(string inputString, char seekingSymbol, int magicConstant = 8) 
            => inputString.Count(c => c == seekingSymbol) >= magicConstant;

         *
         */

        public static bool IsSeparatingLine(string str, char symbol)
        {
            int i = 0;
            if (str[0] != '|')
                return false;
            foreach (var ch in str.Substring(1))
            {
                if (ch != symbol) return false;
                i++;
            }

            //return i >= 8;
            //почему 8? ј что есть добавитс€ еще? ѕосмотри про антипаттерн "Magic Constant"
            if (i < 8) return false;
            return true;
        }

        /*
         * ќп€ть таки, что за GetParam, какой параметр, пока не знаю, какое название было бы правильнее вз€ть, так как слишком много действий
         * метод выполн€ет, надо бы раздробить
         */
        public static List<Param> GetParam(string paramString)
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
                    if (res.Value == value)
                        return null;

                result.Add(new Param(value, varType));

                if (tokFLow.Current.Type == TokType.Sep)
                    tokFLow.MoveNext();
            }

            if (!result.Any())
                return null;
            return result;
        }

        /*
         * ћетод пытаетс€ найти тэги, которые идут через зап€тую, верно?
         
            ћожно попробовать вот так

            public static string[] TryParseTagsWithSeparator(string inputString, char separator) 
            => Array.ConvertAll(inputString.Split(separator), p => p.Trim());

         */
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