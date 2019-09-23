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
        //todo cr: naming FindStringOrNullByKeyWord -> GetContentIfStartsFromKeywordOrNull
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


        //todo cr : Use List only if you need to modify collection outside
        //Use array or any other readonly collection (like Ienumerable<T> or ReadonlyMemory)
        // otherwise


        //todo cr: Whole method is useless. Use GetVarTypeByNFun instead
        //todo cr: GetInOutParam -> ParseInOutList
        internal static List<IdType> GetInOutParam(string paramString)
        {
            //todo cr: - remove useless line
            List<IdType> result = new List<IdType>();

            result = GetVarTypeByNFun(paramString);

            //todo cr: null is not the best option - return empty collection instead

            if (!result.Any())
                return null;
            return result;
        }

        //todo cr: Naming: GetVarTypeByNFun-> ParseVarTypes
        private static List<IdType> GetVarTypeByNFun(string paramString)
        {
            string value;
            VarType varType;
            List<IdType> result = new List<IdType>();

            //todo cr: translate comments to eng. It's opensource baby!

            //генерим поток токенов
            var tokFLow = Tokenizer.ToFlow(paramString);

            //пока не кончатся токены
            while (tokFLow.Current.Type != TokType.Eof)
            {
                //todo cr: use var previous =  tokFlow.Current; tokFlow.MoveNext();
                // instead of using "tokflow.previous" everywhere

                //todo cr: best style is to read tokens from flow
                // one by one instead of using previous or next tokens 
                tokFLow.MoveNext();

                //проверяет предыдуший и следующий токен
                if ((tokFLow.Previous.Type != TokType.Id ||
                     tokFLow.Current.Type != TokType.Colon) ||
                    (tokFLow.Peek == null))
                    return null;
                //если выражение нам подходит, то берем имя переменной(Value) и ее тип(VarType)
                //todo cr: do not introduce reusable variable. 
                //Use "previous.Value" everywhere
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

        //todo cr: naming: dividingSymbol -> separator
        internal static List<string> SplitWithTrim(string str, char dividingSymbol)
        {
            //todo cr: using linq Where in one chain
            //a.ConvertAll
            // .Where
            // .ToArray 

            //Use experssion style for one-line methods 
            var res = Array.ConvertAll(str.Split(dividingSymbol), p => p.Trim()).ToList();
            res.RemoveAll(p => p == "");
            return res;
        }
        //todo cr: naming: GetValue->ParseValues
        internal static List<VarVal> GetValue(string valueStr)
        {
            //todo cr: var
            List<VarVal> result = new List<VarVal>();
            //генерим поток токенов
            var tokFLow = Tokenizer.ToFlow(valueStr);

            //пока не кончатся токены
            while (tokFLow.Current.Type != TokType.Eof)
            {
                //todo cr: Check for DRY for GetValue and GetVarTypeByNFun methods
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