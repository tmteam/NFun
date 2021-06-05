using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Runtime.Arrays;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tokenization;
using NFun.Types;

namespace NFun
{
    public static class ValueParser
    {
        public static object  ParseValue(string text)
        {
            var flow = Tokenizer.ToFlow(text);
            return ParseValue(flow).Item1;
        }
        public static (object, FunnyType) ParseValue(this TokFlow flow)
        {

           var syntaxNode = SyntaxNodeReader.ReadNodeOrNull(flow);
            return ParseSyntaxNode(syntaxNode);
        }

        private static (object, FunnyType) ParseSyntaxNode(ISyntaxNode syntaxNode)
        {
            if (syntaxNode == null)
                throw new ArgumentException();
            if (syntaxNode is ConstantSyntaxNode constant)
                return ParseConstant(constant);
            if (syntaxNode is GenericIntSyntaxNode intGeneric)
                return ParseGenericIntConstant(intGeneric);
            if (syntaxNode is ArraySyntaxNode array)
            {
                var items = new List<object>(array.Expressions.Count);
                FunnyType? unifiedType = null;
                foreach (var child in array.Expressions)
                {
                    var (value, childVarType) =  ParseSyntaxNode(child);
                    if (!unifiedType.HasValue)
                        unifiedType = childVarType;
                    else if(unifiedType!= childVarType)
                        unifiedType = FunnyType.Anything;
                    
                    items.Add(value);
                }

                if (!items.Any())
                    return (new object[0], FunnyType.ArrayOf(FunnyType.Anything));
                return (items.ToArray(), FunnyType.ArrayOf( unifiedType.Value));
            }
            throw new NotSupportedException($"syntax node {syntaxNode.GetType().Name} is not supported");
        }
        
        private static (object, FunnyType) ParseGenericIntConstant(GenericIntSyntaxNode constant)
        {
            if (constant.IsHexOrBin)
            {
                //0xff, 0xFFFF or 0b1110101010
                if (constant.Value is long l)
                {                    
                    if (l <= int.MaxValue)
                        return ((int) l, FunnyType.Int32);
                    else
                        return (l, FunnyType.Int64);
                }
                else if(constant.Value is ulong u)
                    return (u, FunnyType.UInt64);
                else
                    throw new NotSupportedException();
                
            }
            else
            {
                //1,2,3..
                if (constant.Value is long l)
                    return ((double) l, FunnyType.Real);
                if (constant.Value is ulong u)
                    return ((double)u, FunnyType.Real);
                
                throw new NotSupportedException();
            }
        }
        private static (object, FunnyType) ParseConstant(ConstantSyntaxNode constant) =>
            constant.Value switch
            {
                int i    => (i, FunnyType.Int32),
                uint ui  => (ui, FunnyType.UInt32),
                double d => (d, FunnyType.Real),
                TextFunArray str => (str.ToString(), FunnyType.Text),
                bool b => (b, FunnyType.Bool),
                _ => throw new ArgumentOutOfRangeException()
            };
    }
}