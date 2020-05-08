using System;
using System.Collections.Generic;
using NFun.BuiltInFunctions;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;
using NFun.TypeInferenceAdapter;
using NFun.Types;

namespace NFun.SyntaxParsing.SyntaxNodes
{
    public class ResultFunCallSyntaxNode: IFunCallSyntaxNode
    {
        public ResultFunCallSyntaxNode(ISyntaxNode resultExpression, ISyntaxNode[] args, Interval interval)
        {
            ResultExpression = resultExpression;
            Args = args;
            Interval = interval;
        }
        public VarType OutputType { get; set; }
        public int OrderNumber { get; set; }

        public bool IsInBrackets { get; set; }
        public ISyntaxNode ResultExpression { get; }
        public ISyntaxNode[] Args { get; }
        public Interval Interval { get; set; }
        public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
        
        public IEnumerable<ISyntaxNode> Children
        {
            get
            {
                yield return ResultExpression;
                foreach (var node in Args)
                    yield return node;
            }
        }

        /// <summary>
        /// Concrete Function Signature.
        /// Setted after Ti-algorithm applied 
        /// </summary>
        public LangFunctionSignature SignatureOfOverload { get; set; }
    }
}