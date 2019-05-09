using System.Collections.Generic;
using System.Linq;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Parsing
{
    public static class SyntaxNodeFactory
    {
           #region factories

           public static ISyntaxNode AnonymFun(ISyntaxNode defenition, ISyntaxNode body)
               => new AnonymCallSyntaxNode(defenition, body, Interval.Unite(defenition.Interval, body.Interval));
        public static ISyntaxNode IfElse(IList<IfThenSyntaxNode> ifThenNodes, ISyntaxNode elseResult, int start, int end) 
            => new IfThenElseSyntaxNode(ifThenNodes, elseResult, Interval.New(start, end));
        
        public static IfThenSyntaxNode IfThen(ISyntaxNode condition, ISyntaxNode expression, int start, int end)
            => new IfThenSyntaxNode(condition,expression,Interval.New(start, end));
        
        public static ISyntaxNode Var(Tok token) => new VariableSyntaxNode(token.Value, token.Interval); 
        
        public static ISyntaxNode Text(Tok token)=> new TextSyntaxNode(token.Value,token.Interval);
        public static ISyntaxNode Num(Tok token) => new TextSyntaxNode(token.Value, token.Interval);
        public static ISyntaxNode Num(string val, int start, int end) => new TextSyntaxNode(val, Interval.New(start,end));
        public static ISyntaxNode ProcArrayInit(ISyntaxNode @from, ISyntaxNode step, ISyntaxNode to, int start, int end)
            =>new ProcArrayInit(from, to, step, Interval.New(start,end));

        public static ISyntaxNode ProcArrayInit(ISyntaxNode @from, ISyntaxNode to, int start, int end)
            =>new ProcArrayInit(from, to, null, Interval.New(start,end));
        
        public static ISyntaxNode Array(ISyntaxNode[] elements, int start, int end)
            => new ArraySyntaxNode(elements, Interval.New(start,end));
        public static ISyntaxNode ListOf(ISyntaxNode[] elements, Interval interval, bool hasBrackets) 
            => new ListOfExpressionsSyntaxNode(elements, hasBrackets, interval);
        
        public static ISyntaxNode TypedVar(string name, VarType type, int start, int end)
            => new TypedVarDefSyntaxNode(name, type, Interval.New(start,end));

        public static ISyntaxNode FunCall(string name, ISyntaxNode[] children, int start, int end) 
            => new FunCallSyntaxNode(name, children, Interval.New(start,end));    
       
        public static ISyntaxNode OperatorFun(string name, ISyntaxNode[] children, int start, int end) 
            => new FunCallSyntaxNode(name, children, Interval.New(start,end), true);    
        
      
        #endregion

    }
    
    public interface ISyntaxNode
    {
        bool IsBracket { get; set; }
         LexNodeType Type { get; }
         Interval Interval { get; set; }
    }
    

    public class VariableSyntaxNode : ISyntaxNode
    {
        public VariableSyntaxNode(string value, Interval interval)
        {
            Value = value;
        }

        public bool IsBracket { get; set; }
        public LexNodeType Type => LexNodeType.Var;
        public string Value { get; }
        public Interval Interval { get; set; }
    }
    public class FunCallSyntaxNode: ISyntaxNode
    {
        public FunCallSyntaxNode(string value, ISyntaxNode[] args, Interval interval, bool isOperator = false)
        {
            Value = value;
            Args = args;
            Interval = interval;
            IsOperator = isOperator;
        }

        public bool IsBracket { get; set; }
        public LexNodeType Type => LexNodeType.Fun;
        public string Value { get; }
        public ISyntaxNode[] Args { get; }
        public Interval Interval { get; set; }
        public bool IsOperator { get; }
    }

    public class IfThenSyntaxNode : ISyntaxNode
    {
        public ISyntaxNode Test { get; }
        public ISyntaxNode Expr { get; }

        public IfThenSyntaxNode(ISyntaxNode test, ISyntaxNode expr, Interval interval)
        {
            Test = test;
            Expr = expr;
            Interval = interval;
        }
        public bool IsBracket { get; set; }
        public LexNodeType Type => LexNodeType.IfThen;
        public Interval Interval { get; set; }
    }

    public class IfThenElseSyntaxNode : ISyntaxNode
    {
        public IfThenSyntaxNode[] Ifs { get; }
        public ISyntaxNode ElseExpr { get; }

        public IfThenElseSyntaxNode(IList<IfThenSyntaxNode> ifs, ISyntaxNode elseExpr, Interval interval)
        {
            Ifs = ifs.ToArray();
            ElseExpr = elseExpr;
            Interval = interval;
        }
        public bool IsBracket { get; set; }
        public LexNodeType Type => LexNodeType.IfThanElse;
        public Interval Interval { get; set; }
    }

    public class NumberSyntaxNode : ISyntaxNode
    {
        public NumberSyntaxNode(string value, Interval interval)
        {
            Value = value;
        }
        public bool IsBracket { get; set; }
        public LexNodeType Type => LexNodeType.Number;
        public string Value { get; }
        public Interval Interval { get; set; }
    }
    
    public class TextSyntaxNode : ISyntaxNode
    {
        public TextSyntaxNode (string value, Interval interval)
        {
            Value = value;
        }
        public bool IsBracket { get; set; }
        public LexNodeType Type => LexNodeType.Number;
        public string Value { get; }
        public Interval Interval { get; set; }
    }

    public class ProcArrayInit : ISyntaxNode
    {
        public ISyntaxNode From { get; }
        public ISyntaxNode To { get; }
        public ISyntaxNode Step { get; }

        public ProcArrayInit(ISyntaxNode from, ISyntaxNode to, ISyntaxNode step, Interval interval)
        {
            From = @from;
            To = to;
            Step = step;
            Interval = interval;
        }

        public bool IsBracket { get; set; }
        public LexNodeType Type => LexNodeType.ProcArrayInit;
        public Interval Interval { get; set; }
    }

    public class ListOfExpressionsSyntaxNode : ISyntaxNode
    {
        public ISyntaxNode[] Expressions { get; }

        public ListOfExpressionsSyntaxNode(ISyntaxNode[] expressions,bool hasBrackets, Interval interval)
        {
            Expressions = expressions;
            IsBracket = hasBrackets;
            Interval = interval;
        }
        public bool IsBracket { get; set; }
        public LexNodeType Type => LexNodeType.ListOfExpressions;
        public Interval Interval { get; set; }
    }

    public class TypedVarDefSyntaxNode : ISyntaxNode
    {
        public string Name { get; }
        public VarType VarType { get; }

        public TypedVarDefSyntaxNode(string name, VarType varType, Interval interval)
        {
            Name = name;
            VarType = varType;
            Interval = interval;
        }
        public bool IsBracket { get; set; }
        public LexNodeType Type => LexNodeType.Var;
        public Interval Interval { get; set; }
    }

    public class ArraySyntaxNode : ISyntaxNode
    {
        public ISyntaxNode[] Expressions { get; }

        public ArraySyntaxNode(ISyntaxNode[] expressions, Interval interval)
        {
            Expressions = expressions;
            Interval = interval;
        }
        public bool IsBracket { get; set; }
        public LexNodeType Type { get; }
        public Interval Interval { get; set; }
    }

    public class AnonymCallSyntaxNode : ISyntaxNode
    {
        public ISyntaxNode Defenition { get; }
        public ISyntaxNode Body { get; }

        public AnonymCallSyntaxNode(ISyntaxNode defenition, ISyntaxNode body, Interval interval)
        {
            Defenition = defenition;
            Body = body;
            Interval = interval;
        }

        public bool IsBracket { get; set; }
        public LexNodeType Type => LexNodeType.AnonymFun;
        public Interval Interval { get; set; }
    }
}