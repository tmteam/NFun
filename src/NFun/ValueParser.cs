using System;
using System.Collections.Generic;
using System.Linq;
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
            var reader = new SyntaxParsing.SyntaxNodeReader(flow);
            var syntaxNode = reader.ReadExpressionOrNull();
            return ParseSyntaxNode(syntaxNode);
        }

        private static (object, VarType) ParseSyntaxNode(ISyntaxNode syntaxNode)
        {
            if (syntaxNode == null)
                throw new ArgumentException();
            if (syntaxNode is ConstantSyntaxNode constant)
                return ParseConstant(constant);
           
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
            throw new ArgumentException();
        }

        private static (object, VarType) ParseConstant(ConstantSyntaxNode constant)
        {
            switch (constant.Value)
            {
                case int i:      return (i, VarType.Int32);
                case uint ui:    return (ui, VarType.UInt32);
                case double d:   return (d, VarType.Real);
                case string str: return (str, VarType.Text);
                case bool b:     return (b, VarType.Bool);
            }
            throw  new ArgumentOutOfRangeException();
        }
    }
}