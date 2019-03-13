using System;
using System.Collections.Generic;
using System.Linq;
using Funny.Tokenization;

namespace Funny.Parsing
{
    public class LexNode
    {
        private static LexNodeType TokToNode(TokType tok)
        {
            switch (tok)
            {
                case TokType.Number:
                    return LexNodeType.Number;
                case TokType.Plus:
                    return LexNodeType.Plus;
                case TokType.Minus:
                    return LexNodeType.Minus;
                case TokType.Div:
                    return LexNodeType.Div;
                case TokType.Rema:
                    return LexNodeType.Rema;
                case TokType.Mult:
                    return LexNodeType.Mult;
                case TokType.Pow:
                    return LexNodeType.Pow;
                case TokType.Equal:
                    return LexNodeType.Equal;
                case TokType.NotEqual:
                    return LexNodeType.NotEqual;
                case TokType.And:
                    return LexNodeType.And;
                case TokType.Or:
                    return LexNodeType.Or;
                case TokType.Xor:
                    return LexNodeType.Xor;
                case TokType.Less:
                    return LexNodeType.Less;
                case TokType.More:
                    return LexNodeType.More;
                case TokType.LessOrEqual:
                    return LexNodeType.LessOrEqual;
                case TokType.MoreOrEqual:
                    return LexNodeType.MoreOrEqual;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tok), tok, null);
            }
        } 
        public static LexNode IfElse(IEnumerable<LexNode> ifThenNodes, LexNode elseResult) 
            => new LexNode(LexNodeType.IfThanElse,null, ifThenNodes.Append(elseResult).ToArray());
        public static LexNode Op(TokType type, LexNode leftChild, LexNode rightChild) 
            => new LexNode(TokToNode(type), "", leftChild, rightChild);

        public static LexNode Op(LexNodeType type, LexNode leftChild, LexNode rightChild) 
            => new LexNode(type, "", leftChild, rightChild);
        public static LexNode IfThen(LexNode condition, LexNode expression)
            => new LexNode(LexNodeType.IfThen, null, condition, expression);
        public static LexNode Var(string name) 
            => new LexNode(LexNodeType.Var, name);
            
        public static LexNode Fun(string name, LexNode[] children) 
            => new LexNode(LexNodeType.Fun, name,children);
        
        public static LexNode Text(string val)
            => new LexNode(LexNodeType.Text, val);
        public static LexNode Num(string val)
            => new LexNode(LexNodeType.Number, val);
        public static LexNode Array(LexNode[] elements)
            => new LexNode(LexNodeType.ArrayInit,null, elements);

        public  LexNode(LexNodeType type, string value,params LexNode[] children)
        {
            Type = type;
            Value = value;
            Children = children;
        }
        public LexNodeType Type { get; }
        public string Value { get; }

        public int Finish { get; set; }
        public  IEnumerable<LexNode> Children { get; }
        private string typename => Type.ToString() + (string.IsNullOrWhiteSpace(Value) ? "" : " " + Value);
        public bool Is(LexNodeType type) => Type == type;
        public override string ToString()
        {
            if(!Children.Any())
                return typename;
            else
                return $"{typename}( {string.Join(',', Children.Select(c => c.ToString()))})";
        }


        
    }

    public enum LexNodeType
    {
        Number,
        Plus,
        Minus,
        Div,
        /// <summary>
        /// Division reminder "%"
        /// </summary>
        Rema,
        Mult,
        /// <summary>
        /// Pow "^"
        /// </summary>
        Pow,
        Var,
        Fun,
        And,
        Or,
        Xor,
        Equal,
        NotEqual,
        Less,
        LessOrEqual,
        More,
        MoreOrEqual,
        IfThen,
        IfThanElse,
        Text,
        ArrayInit,
    }
}