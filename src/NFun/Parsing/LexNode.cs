using System.Collections.Generic;
using System.Linq;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Parsing
{
    public class LexNode
    {
        #region factories
        public static LexNode IfElse(IEnumerable<LexNode> ifThenNodes, LexNode elseResult, int start, int end) 
            => new LexNode(LexNodeType.IfThanElse,null, start, end, ifThenNodes.Append(elseResult).ToArray());
        public static LexNode IfThen(LexNode condition, LexNode expression, int start, int end)
            => new LexNode(LexNodeType.IfThen, null, start, end, condition, expression);
        public static LexNode Var(Tok token) 
            => new LexNode(LexNodeType.Var, token.Value, token.Interval);
        public static LexNode Text(Tok token)
            => new LexNode(LexNodeType.Text, token.Value, token.Interval);
        public static LexNode Num(Tok token)
            => new LexNode(LexNodeType.Number, token.Value, token.Interval);
        public static LexNode Num(string val, int start, int end)
            => new LexNode(LexNodeType.Number, val, start, end);
        public static LexNode ProcArrayInit(LexNode @from, LexNode step, LexNode to, int start, int end)
            =>new LexNode(LexNodeType.ProcArrayInit,null, start, end, @from,step,to);

        public static LexNode ProcArrayInit(LexNode @from, LexNode to, int start, int end)
            =>new LexNode(LexNodeType.ProcArrayInit,null, start, end, @from, to);
        
        public static LexNode Array(LexNode[] elements, int start, int end)
            => new LexNode(LexNodeType.ArrayInit,null,start, end,  elements);

        public static LexNode ListOf(LexNode[] elements, Interval interval, bool hasBrackets) 
            => new LexNode(LexNodeType.ListOfExpressions, null, interval,  hasBrackets, elements);
        public static LexNode TypedVar(string name, VarType type, int start, int end)
        {
            return new LexNode(LexNodeType.TypedVar, name, start, end) {
                AdditionalContent = type
            };
        }
        public static LexNode FunCall(string name, LexNode[] children, int start, int end) 
            => new LexNode(LexNodeType.Fun, name, start, end, children);
       
        public static LexNode OperatorFun(string name, LexNode[] children, int start, int end) 
            => new LexNode(LexNodeType.Fun,  name, start, end, children){AdditionalContent = true};
        
        public static LexNode Bracket(LexNode node)
        {
            node.IsBracket = true;
            return node;
        }
      
        #endregion

        public LexNode(LexNodeType type, string value, Interval interval, bool brackets, params LexNode[] children)
        {
            Type = type;
            Value = value;
            Interval = interval;
            Children = children;
            IsBracket = brackets;
        }
        public LexNode(LexNodeType type, string value, Interval interval, params LexNode[] children)
        {
            Type = type;
            Value = value;
            Interval = interval;
            Children = children;
        }
        private LexNode(LexNodeType type, string value, int start, int finish, params LexNode[] children)
        {
            Type = type;
            Value = value;
            Interval = new Interval(start,finish);
            Children = children;
        }
        public bool IsBracket { get; set; }
        public object AdditionalContent { get; private set; }
        public LexNodeType Type { get; }
        public string Value { get; }
        public Interval Interval { get; set; }
        public int Start => Interval.Start;
        public int Finish => Interval.Finish;
        public  IEnumerable<LexNode> Children { get; }
        private string Typename => Type + (string.IsNullOrWhiteSpace(Value) ? "" : " " + Value);
        public bool Is(LexNodeType type) => Type == type;
        public override string ToString()
        {
            if(!Children.Any())
                return Typename;
            else
                return $"{Typename}( {string.Join<string>(",", Children.Select(c => c.ToString()))})";
        }

        public static LexNode AnonymFun(LexNode defenition, LexNode body) 
            => new LexNode(
                LexNodeType.AnonymFun, 
                null, 
                defenition.Start, 
                body.Finish,
                defenition, 
                body);
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
        TypedVar,
        ListOfExpressions,
        ProcArrayInit
    }
}