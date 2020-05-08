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
        public static (object, VarType) ParseValue(string text)
        {
            var flow = Tokenizer.ToFlow(text);
            return ParseValue(flow);
        }
        public static (object, VarType) ParseValue(this Tokenization.TokFlow flow)
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
                List<object> items = new List<object>();
                VarType? unifiedType = null;
                foreach (var child in array.Children)
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
                else if (constant.Value is ulong u)
                    return ((double)u, VarType.Real);
                else
                    throw new NotSupportedException();
            }
        }
        private static (object, VarType) ParseConstant(ConstantSyntaxNode constant)
        {

            switch (constant.Value)
            {
                case int i:      return (i, VarType.Int32);
                case uint ui:    return (ui, VarType.UInt32);
                case double d:   return (d, VarType.Real);
                case TextFunArray str: return (str.ToString(), VarType.Text);
                case bool b:     return (b, VarType.Bool);
                
            }
            throw  new ArgumentOutOfRangeException();
        }
    }
}