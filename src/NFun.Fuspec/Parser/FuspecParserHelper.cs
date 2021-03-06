using System;
using System.Collections.Generic;
using System.Linq;
using NFun;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Fuspec.Parser
{
    internal static class FuspecParserHelper
    {
        internal static string GetContentOrNullIfStartsFromKeyword(string keyWord, string str)
        {
            if (str.Length < keyWord.Length)
                return null;
            string key = str.Substring(0, keyWord.Length);
            if (key == keyWord)
                return str.Substring(keyWord.Length);
            return null;
        }

      internal static List<string> SplitWithTrim(string str, char separator)
            => Array.ConvertAll(str.Split(separator), p => p.Trim()).Where(p=> p!="").Select(p=>p).ToList();
        
        //todo cr: return empty list, not null
        //Todo cr: answer: null is a marker of Error. Empty list is a marker of empty string of values
        internal static VarInfo[] ParseVarType(string paramString, bool isOutput)
        {
            List<VarInfo> result = new List<VarInfo>();

            var tokFLow = Tokenizer.ToFlow(paramString);
            while (tokFLow.Current.Type != TokType.Eof)
            {
                tokFLow.MoveNext();
                var previous = tokFLow.Previous;
                var cur = tokFLow.Current;
                var next = tokFLow.Peek;
                
                //if TokFlow doesn't "Id:[any token]" 
                if ((!(previous.Type== TokType.Id || previous.Type==TokType.Reserved)||
                     cur.Type != TokType.Colon) ||
                     next.Type == TokType.Eof)
                    return null;
               
                var value = previous.Value;
                // if result already contains Id
                if (result.Any(param => param.Name == value))
                    return null;
                
                tokFLow.MoveNext();
                //ToDo cr: pick out this part or not? 
                //******************************************
                var varType = tokFLow.ReadVarType();
                result.Add(new VarInfo(isOutput,varType, value,true));

                //******************************************
                
                
                
                // if next token is ","
                if (tokFLow.Current.Type == TokType.Sep)
                    tokFLow.MoveNext();
            }
            return result.ToArray();
        }

      public static VarVal[] ParseValues(string valueStr)
        {
             var result = new List<VarVal>();
            //create token flow
            var tokFLow = Tokenizer.ToFlow(valueStr);

            //until end of flow
            while (tokFLow.Current.Type != TokType.Eof)
            {
                tokFLow.MoveNext();
                var previous = tokFLow.Previous;
                var cur = tokFLow.Current;
                var next = tokFLow.Peek;

                //if TokFlow doesn't "Id:[any token]" 
                if ((!(previous.Type== TokType.Id || previous.Type==TokType.Reserved) || 
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