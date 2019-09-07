using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using Nfun.Fuspec.Parser.Model;
using NFun.ParseErrors;
using NFun.Tokenization;
using NFun.Types;
using ParcerV1;

namespace Nfun.Fuspec.Parser
{
    public static class FuspecParserHelper
    {
        private const int MinSeparatorLineLength = 8;

        public static string FindStringOrNullByKeyWord(string keyWord, string str)
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

        public static List<Param> GetInOutParam(string paramString)
        {
            List<Param> result = new List<Param>();

            result = GetVarTypeByNFun(paramString);
            
            if (!result.Any())
                return null;
            return result;
        }

        private static List<Param> GetVarTypeByNFun(string paramString)
        {
            string value;
            VarType varType;
            List<Param> result = new List<Param>();
            
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
                if (result.Any(param => param.Value == value)) 
                    return null;
                
                result.Add(new Param(value, varType));
                
                 // если слудующий токен - запятая
                if (tokFLow.Current.Type == TokType.Sep)
                    tokFLow.MoveNext();
            }
            return result;
        }
        
        public static List<string> SplitWithTrim(string str, char dividingSymbol)
        {
            var res = Array.ConvertAll(str.Split(dividingSymbol), p => p.Trim()).ToList();
            res.RemoveAll(p=>p=="");
            return res;
        }

        public static List<Value> GetValue(string valueStr)
        {

            List<Value> result = new List<Value>();
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
                //если выражение нам подходит, то берем имя переменной(Value) и ее тип(VarType)
                var idName = tokFLow.Previous.Value;
                var startIndex = tokFLow.Current.Finish;
            //    tokFLow.MoveNext();

                var endIndex = ReadEndofValue(tokFLow);
          //      var vartype = tokFLow.ReadVarType();
             
//              var value =new Value(idName,valueStr.Substring(startIndex,tokFLow.Current.Start));
             var value = new Value(idName, valueStr.Substring(startIndex,endIndex-startIndex-1));
                result.Add(value);
                
                tokFLow.MoveNext();
                // если слудующий токен - запятая
                if (tokFLow.Current.Type == TokType.Sep)
                    tokFLow.MoveNext();
                    
                 
             }
            return result;
        }
        
        public static int ReadEndofValue(this TokFlow flow)
        {
            var cur = flow.Current;
            flow.MoveNext();

            while (flow.IsCurrent(TokType.ArrOBr))
            {
                flow.MoveNext();
                if (!flow.MoveIf(TokType.ArrCBr))
                {
                    throw ErrorFactory.ArrTypeCbrMissed(new Interval(cur.Start, flow.Current.Start));
                }

            }

            return flow.Current.Finish-1;
        }

   


    }
    
} 