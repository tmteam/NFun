using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NFun.ParseErrors;
using NFun.Runtime.Arrays;
using NFun.Types;

namespace NFun.Jet
{
    public static class JetSerializationHelper
    {
        public const string InputDefenitionId = "i";
        public const string EquationId = "o";
        public const string UserFunctionId = "u";
        public const string GenericUserFunctionId = "v";
        public const string CastId = "c";
        public const string VariableId = "x";
        public const string ConstId = "n";
        public const string ArrayId = "a";
        public const string IfId = "s";
        public const string FunCallId = "f";
        public const string GenericCallId = "g";

        public const string ParameterlessAttributeId = "w";
        public const string AttributeWithParameterId = "q";

        public static string ToJetTypeText(this VarType type)
        {
            switch (type.BaseType)
            {
                case BaseVarType.Empty:   return "???";
                case BaseVarType.Char:    return "c";
                case BaseVarType.Bool:    return "b";
                case BaseVarType.UInt8:   return "u8";
                case BaseVarType.UInt16:  return "u16";
                case BaseVarType.UInt32:  return "u32";
                case BaseVarType.UInt64:  return "u64";
                case BaseVarType.Int16:   return "i16";
                case BaseVarType.Int32:   return "i32";
                case BaseVarType.Int64:   return "i64";
                case BaseVarType.Real:    return "r";
                case BaseVarType.Any:     return "a";
                case BaseVarType.Generic: return type.GenericId.Value.ToString();

                case BaseVarType.ArrayOf:  return "'" + ToJetTypeText(type.ArrayTypeSpecification.VarType);
                case BaseVarType.Fun: return "(" + string.Join(",", type.FunTypeSpecification.Inputs.Select(ToJetTypeText)) + "):" +
                           ToJetTypeText(type.FunTypeSpecification.Output);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static VarType ParseType(string type)
        {
            switch (type)
            {
                case "???": return VarType.Empty;
                case "c": return VarType.Char;
                case "b": return VarType.Bool;
                case "u8": return VarType.UInt8;
                case "u16": return VarType.UInt16;
                case "u32": return VarType.UInt32; 
                case "u64": return VarType.UInt64;
                case "i16": return VarType.Int16;
                case "i32": return VarType.Int32;
                case "i64": return VarType.Int64;
                case "r": return VarType.Real;
                case "a":return VarType.Anything;
            }

            if (type.StartsWith("'"))
                return VarType.ArrayOf(ParseType(type.Substring(1)));
            if (int.TryParse(type, out var id))
                return VarType.Generic(id);

            throw new NotImplementedException("type "+type+" is not supported");
        }

        public static object ParseConstantValue(VarType type, string value)
        {
            switch (type.BaseType)
            {
                case BaseVarType.Bool:
                    return bool.Parse(value);
                case BaseVarType.UInt8:
                    return Byte.Parse(value);
                case BaseVarType.UInt16:
                    return UInt16.Parse(value);
                case BaseVarType.UInt32:
                    return UInt32.Parse(value);
                case BaseVarType.UInt64:
                    return UInt64.Parse(value);
                case BaseVarType.Int16:
                    return Int16.Parse(value);
                case BaseVarType.Int32:
                    return Int32.Parse(value);
                case BaseVarType.Int64:
                    return Int64.Parse(value);
                case BaseVarType.Real:
                    return Double.Parse(value, NumberFormatInfo.InvariantInfo);
                case BaseVarType.ArrayOf when type.ArrayTypeSpecification.VarType == VarType.Char:
                    return new TextFunArray(JetSerializationHelper.FromJetEscaped(value));
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        const string ms_regexEscapes = @"[\f\n\r\s\t\v\\]";

        /// <summary>
        /// Converts usual text to jet escaped text. Replace pattern: \f\n\r\s\t\v\\. Returns \\e for empty string
        /// </summary>
        public static string ToJetEscaped(string text)
        {
            if (text.Length == 0)
                return "\\e";
            return   Regex.Replace(text, ms_regexEscapes, match);
        }

        /// <summary>
        /// Convert jet escaped string. Replace pattern: \f\n\r\s\t\v\\. Parses \\e as empty string
        /// </summary>
        public static string FromJetEscaped(string rawString)
        {
            var sb = new StringBuilder();
            int lastNonEscaped = 0;

            int i = 0;
            for (; i < rawString.Length; i++)
            {
                if (rawString[i] != '\\')
                    continue;

                if (lastNonEscaped != i)
                {
                    var prev = rawString.Substring(lastNonEscaped, i - lastNonEscaped);
                    sb.Append(prev);
                }

                if (i == rawString.Length - 1)
                    throw new JetParseException("BackslashAtEndOfString");

                var next = rawString[i + 1];
                char symbol;
                switch (next)
                {
                    case '\\': symbol = '\\'; break;
                    case 'n': symbol = '\n'; break;
                    case 'r': symbol = '\r'; break;
                    case 't': symbol = '\t'; break;
                    case 'f': symbol = '\f'; break;
                    case 'v': symbol = '\v'; break;
                    case 's': symbol = ' '; break;
                    //use special escaped for empty string
                    case 'e': return string.Empty;
                    default:
                        throw new JetParseException("UnknownEscapeSequence "+next);
                }
                sb.Append(symbol);
                i++;
                lastNonEscaped = i + 1;
            }

            if (lastNonEscaped == 0)
                return rawString;

            if (lastNonEscaped <= rawString.Length - 1)
            {
                var prev = rawString.Substring(lastNonEscaped, i - lastNonEscaped);
                sb.Append(prev);
            }
            return sb.ToString();
        }
        private static string match(Match m)
        {
            string match = m.ToString();
            switch (match)
            {
                case " ": return @"\s";
                case "\f": return @"\f";
                case "\n": return @"\n";
                case "\r": return @"\r";
                case "\t": return @"\t";
                case "\v": return @"\v";
                case "\\": return @"\\";
            }

            throw new NotSupportedException();
        }
    }
}
