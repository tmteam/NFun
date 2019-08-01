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

        //Из названия не понятно, что делает
        /*как понял, что ищет название фуспека.

         public static string TryFindFuspectestName(string mask, string inputString) =>
            !inputString.Contains(mask)
                ? null
                : inputString.Substring(mask.Length);

         */
        //Юра: я бы назвал GetKeywordOrNull
        public static string FindKeyWord(string findingkey, string str)
        {
            if (str.Length < findingkey.Length)
                return null;
            string key = str.Substring(0, findingkey.Length);


            /*
             * Смотри, как тут элегантно с тернарным оператором))
             * return key == findingkey
             *              ? str.Substring(findingkey.Length)
             *              : null;
             */
            //Юра: на вкус и цвет.
            //Что не очень хорошо - если использовать if else то либо оба должны быть в скобках
            //либо оба без
            if (key == findingkey)
            {
                return str.Substring(findingkey.Length);
            }
            else
                return null;
        }


        /*
         * Это получается метод, который определяет состоит ли линия из вертикальной полоски и зведочек?
         * Надо подумать над названием, пока из дебага не запустил - не понял, что происходит

           public static bool IsSeparatingStarLine(string inputString, char seekingSymbol, int magicConstant = 8) 
            => inputString.Count(c => c == seekingSymbol) >= magicConstant;

         *
         */
        //Юра: не согласен с егорм. Буквальный перевод ЯвляетсяЛиРазделительнойЛинией
        //Что можно улучшить  -  symbol. Лучше назвать lineSymbol
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
            //почему 8? А что есть добавится еще? Посмотри про антипаттерн "Magic Constant"
            //Юра: По спеке фуспека может быть 8 или более. Но нужно вытащить в костанту - да 
            // i >= MinSeparatorLineLength предпочтительнее
            if (i < 8) return false;
            return true;
        }

        /*
         * Опять таки, что за GetParam, какой параметр, пока не знаю, какое название было бы правильнее взять, так как слишком много действий
         * метод выполняет, надо бы раздробить
         */
        //Юра: Согласен. Не понятно. Вероятно тут нужны комментаии, и может разбить метод на подметоды
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
         * Метод пытается найти тэги, которые идут через запятую, верно?
         
            Можно попробовать вот так

            public static string[] TryParseTagsWithSeparator(string inputString, char separator) 
            => Array.ConvertAll(inputString.Split(separator), p => p.Trim());

         */
        //Юра: не - это именно SplitWithTrim. То есть разделение с отрезанием
        //Что не очень хорошо - это нейминг char ch
        public static List<string> SplitWithTrim(string str, char ch)
        {
            //Юра - и var ;)
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