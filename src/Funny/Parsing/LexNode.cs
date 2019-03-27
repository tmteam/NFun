using System;
using System.Collections.Generic;
using System.Linq;
using Funny.Tokenization;
using Funny.Types;

namespace Funny.Parsing
{
    public class LexNode
    {
        public static LexNode IfElse(IEnumerable<LexNode> ifThenNodes, LexNode elseResult) 
            => new LexNode(LexNodeType.IfThanElse,null, ifThenNodes.Append(elseResult).ToArray());
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

        public static LexNode ProcArrayInit(LexNode start, LexNode step, LexNode end)
                =>new LexNode(LexNodeType.ProcArrayInit,null, start,step,end);

        public static LexNode ProcArrayInit(LexNode start, LexNode end)
            =>new LexNode(LexNodeType.ProcArrayInit,null, start, end);

        public static LexNode Argument(string name, VarType type)
        {
            return new LexNode(LexNodeType.Argument, name)
            {
                AdditionalContent = type
            };
        }

        public static LexNode ListOf(LexNode[] elements) 
            => new LexNode(LexNodeType.ListOfExpressions, null, elements);

        public  LexNode(LexNodeType type, string value,params LexNode[] children)
        {
            Type = type;
            Value = value;
            Children = children;
        }

        public object AdditionalContent { get; private set; }
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


        public static LexNode AnonymFun(LexNode defenition, LexNode body) 
            => new LexNode(LexNodeType.AnonymFun, null, defenition, body);
    }

    public enum LexNodeType
    {
        Number,
        Var,
        Fun,
        IfThen,
        IfThanElse,
        Text,
        ArrayInit,
        AnonymFun,
        Argument,
        ListOfExpressions,
        ProcArrayInit
    }
}