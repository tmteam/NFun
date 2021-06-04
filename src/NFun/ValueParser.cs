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
        public static (object, VarType) ParseValue(this TokFlow flow)
        {

           var syntaxNode = SyntaxNodeReader.ReadNodeOrNull(flow);
            return ParseSyntaxNode(syntaxNode);
        }

        private static (object, VarType) ParseSyntaxNode(ISyntaxNode syntaxNode)
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
                VarType? unifiedType = null;
                foreach (var child in array.Expressions)
                {
                    var (value, childVarType) =  ParseSyntaxNode(child);
                    if (!unifiedType.HasValue)
                        unifiedType = childVarType;
                    else if(unifiedType!= childVarType)
                        unifiedType = VarType.Anything;
                    
                    items.Add(value);
                }

                if (!items.Any())
                    return (new object[0], VarType.ArrayOf(VarType.Anything));
                return (items.ToArray(), VarType.ArrayOf( unifiedType.Value));
            }
            throw new NotSupportedException($"syntax node {syntaxNode.GetType().Name} is not supported");
        }
        
        private static (object, VarType) ParseGenericIntConstant(GenericIntSyntaxNode constant)
        {
            if (constant.IsHexOrBin)
            {
                //0xff, 0xFFFF or 0b1110101010
                if (constant.Value is long l)
                {                    
                    if (l <= int.MaxValue)
                        return ((int) l, VarType.Int32);
                    else
                        return (l, VarType.Int64);
                }
                else if(constant.Value is ulong u)
                    return (u, VarType.UInt64);
                else
                    throw new NotSupportedException();
                
            }
            else
            {
                //1,2,3..
                if (constant.Value is long l)
                    return ((double) l, VarType.Real);
                if (constant.Value is ulong u)
                    return ((double)u, VarType.Real);
                
                throw new NotSupportedException();
            }
        }
        private static (object, VarType) ParseConstant(ConstantSyntaxNode constant) =>
            constant.Value switch
            {
                int i    => (i, VarType.Int32),
                uint ui  => (ui, VarType.UInt32),
                double d => (d, VarType.Real),
                TextFunArray str => (str.ToString(), VarType.Text),
                bool b => (b, VarType.Bool),
                _ => throw new ArgumentOutOfRangeException()
            };
    }
}