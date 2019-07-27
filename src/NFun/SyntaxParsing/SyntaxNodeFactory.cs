using System;
using System.Collections.Generic;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.SyntaxParsing
{
    public static class SyntaxNodeFactory
    {
        #region factories

        public static ISyntaxNode AnonymFun(ISyntaxNode defenition, ISyntaxNode body)
            => new AnonymCallSyntaxNode(defenition, body, Interval.Unite(defenition.Interval, body.Interval));
        public static ISyntaxNode IfElse(IList<IfCaseSyntaxNode> ifThenNodes, ISyntaxNode elseResult, int start, int end) 
            => new IfThenElseSyntaxNode(ifThenNodes, elseResult, Interval.New(start, end));
        
        public static IfCaseSyntaxNode IfThen(ISyntaxNode condition, ISyntaxNode expression, int start, int end)
            => new IfCaseSyntaxNode(condition,expression,Interval.New(start, end));
        
        public static ISyntaxNode Var(Tok token) => new VariableSyntaxNode(token.Value, token.Interval); 
        
        public static ISyntaxNode Constant(object value, VarType type, Interval interval) 
            => new ConstantSyntaxNode(value, type, interval);
        
        public static ISyntaxNode ProcArrayInit(ISyntaxNode @from, ISyntaxNode to, ISyntaxNode step, int start, int end)
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
}