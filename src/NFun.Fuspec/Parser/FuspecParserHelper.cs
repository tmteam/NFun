using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using Nfun.Fuspec.Parser.Model;
using NFun;
using NFun.ParseErrors;
using NFun.Tokenization;
using NFun.Types;

using ParcerV1;

namespace Nfun.Fuspec.Parser
{
    internal static class FuspecParserHelper
    {
        private const int MinSeparatorLineLength = 8;

        internal static string FindStringOrNullByKeyWord(string keyWord, string str)
        {
            if (str.Length < keyWord.Length)
                return null;
            string key = str.Substring(0, keyWord.Length);
            if (key == keyWord)
                return str.Substring(keyWord.Length);
            return null;
        }

        internal static bool IsSeparatingLine(string str, char lineSymbol)
        {
            str = str.Trim();
            if (str[0] != '|')
                return false;
            return (str.Substring(1).All(c => c == lineSymbol) && str.Length > MinSeparatorLineLength);
        }



        /*
         * Опять таки, что за GetParam, какой параметр, пока не знаю, какое название было бы правильнее взять, так как слишком много действий
         * метод выполняет, надо бы раздробить
         */
        //Юра: Согласен. Не понятно. Вероятно тут нужны комментаии, и может разбить метод на подметоды

        /* Наташа: Param - это модель фуспека. Возможно нужно дать лучший нейминг для модели? Метод получает список объектов Param
         * Вынесла всю логику работы с NFun в отдельный метод ParseStringByNFun, чтобы она не вводила смуту в понимание.
         * В методе написала комментарии, что делают методы NFun.
         * Думаю Юра поправит, если я что-то не так поняла )
         */

        internal static List<IdType> GetInOutParam(string paramString)
        {
            List<IdType> result = new List<IdType>();

            result = GetVarTypeByNFun(paramString);

            if (!result.Any())
                return null;
            return result;
        }

        private static List<IdType> GetVarTypeByNFun(string paramString)
        {
            string value;
            VarType varType;
            List<IdType> result = new List<IdType>();

            //генерим поток токенов
            var tokFLow = Tokenizer.ToFlow(paramString);

            //пока не кончатся токены
            while (tokFLow.Current.Type != TokType.Eof)
            {
                tokFLow.MoveNext();

                //проверяет предыдуший и следующий токен
                if ((tokFLow.Previous.Type != TokType.Id ||
                     tokFLow.Current.Type != TokType.Colon) ||
                    (tokFLow.Peek == null))
                    return null;
                //если выражение нам подходит, то берем имя переменной(Value) и ее тип(VarType)
                value = tokFLow.Previous.Value;
                tokFLow.MoveNext();
                varType = tokFLow.ReadVarType();

                // если такое имя переменной уже есть
                if (result.Any(param => param.Id == value))
                    return null;

                result.Add(new IdType(value, varType));

                // если слудующий токен - запятая
                if (tokFLow.Current.Type == TokType.Sep)
                    tokFLow.MoveNext();
            }
            return result;
        }

        internal static List<string> SplitWithTrim(string str, char dividingSymbol)
        {
            var res = Array.ConvertAll(str.Split(dividingSymbol), p => p.Trim()).ToList();
            res.RemoveAll(p => p == "");
            return res;
        }

        internal static List<VarVal> GetValue(string valueStr)
        {
            List<VarVal> result = new List<VarVal>();
            //генерим поток токенов
            var tokFLow = Tokenizer.ToFlow(valueStr);

            //пока не кончатся токены
            while (tokFLow.Current.Type != TokType.Eof)
            {
                tokFLow.MoveNext();

                //проверяет предыдуший и следующий токен
                if ((tokFLow.Previous.Type != TokType.Id ||
                     tokFLow.Current.Type != TokType.Colon) ||
                    (tokFLow.Peek == null))
                    return null;
                //если выражение нам подходит, то берем имя переменной(Value) и парсим ее тип и значение
                var idName = tokFLow.Previous.Value;
                tokFLow.MoveNext();
                var valVarType = ValueParser.ParseValue(tokFLow);
                var value = new VarVal(idName, valVarType.Item1, valVarType.Item2);
                result.Add(value);

                //  tokFLow.MoveNext();
                // если слудующий токен - запятая
                if (tokFLow.Current.Type == TokType.Sep)
                    tokFLow.MoveNext();
            }
            return result;
        }
    }

} 