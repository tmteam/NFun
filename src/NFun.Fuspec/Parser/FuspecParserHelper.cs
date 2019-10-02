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
        
        internal static string GetContentOrNullIfStartsFromKeyword(string keyWord, string str)
        {
            if (str.Length < keyWord.Length)
                return null;
            string key = str.Substring(0, keyWord.Length);
            if (key == keyWord)
                return str.Substring(keyWord.Length);
            return null;
        }

        public static bool IsSeparatingLine(string str, char lineSymbol)
        {
            str = str.Trim();
            if (str[0] != '|')
                return false;
            return (str.Substring(1).All(c => c == lineSymbol) && str.Length > MinSeparatorLineLength);
        }

        internal static List<string> SplitWithTrim(string str, char separator)
            => Array.ConvertAll(str.Split(separator), p => p.Trim()).Where(p=> p!="").Select(p=>p).ToList();
        
        //todo cr: return empty list, not null
        //Todo cr: answer: null is a marker of Error. Empty list is a marker of empty string of values
        internal static IdType[] ParseVarType(string paramString)
        {
            List<IdType> result = new List<IdType>();

            var tokFLow = Tokenizer.ToFlow(paramString);
            while (tokFLow.Current.Type != TokType.Eof)
            {
                tokFLow.MoveNext();
                var previous = tokFLow.Previous;
                var cur = tokFLow.Current;
                var next = tokFLow.Peek;
                
                //if TokFlow doesn't "Id:[any token]" 
                if ((previous.Type != TokType.Id ||
                     cur.Type != TokType.Colon) ||
                     next.Type == TokType.Eof)
                    return null;
               
                var value = previous.Value;
                // if result already contains Id
                if (result.Any(param => param.Id == value))
                    return null;
                
                tokFLow.MoveNext();
                //ToDo cr: pick out this part or not? 
                //******************************************
                var varType = tokFLow.ReadVarType();
                result.Add(new IdType(value, varType));

                //******************************************
                
                
                
                // if next token is ","
                if (tokFLow.Current.Type == TokType.Sep)
                    tokFLow.MoveNext();
            }
            return result.ToArray();
        }

      internal static VarVal[] ParseValues(string valueStr)
        {
             var result = new List<VarVal>();
            //генерим поток токенов
            var tokFLow = Tokenizer.ToFlow(valueStr);

            //пока не кончатся токены
            while (tokFLow.Current.Type != TokType.Eof)
            {
                //todo cr: Check for DRY for GetValue and GetVarTypeByNFun methods
                tokFLow.MoveNext();
                var previous = tokFLow.Previous;
                var cur = tokFLow.Current;
                var next = tokFLow.Peek;

                //if TokFlow doesn't "Id:[any token]" 
                if ((previous.Type != TokType.Id ||
                     cur.Type != TokType.Colon) ||
                    next.Type == TokType.Eof)
                    return null;
                
                var idName = previous.Value;
                // if result already contains Id
                if (result.Any(param => param.Name == idName))
                    return null;
                
                tokFLow.MoveNext();   
                
                //ToDo cr: pick out this part or not? 
                //*********************************************
                var valVarType = tokFLow.ParseValue();
                result.Add(new VarVal(idName, valVarType.Item1, valVarType.Item2));
                //*********************************************

                if (tokFLow.Current.Type == TokType.Sep)
                    tokFLow.MoveNext();
            }
            return result.ToArray();
        }
        
    
    }

} 