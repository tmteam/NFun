using System.Collections.Generic;
using NFun.Interpritation.Functions;
using NFun.SyntaxParsing.Visitors;
using NFun.Tic.SolvingStates;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.SyntaxParsing.SyntaxNodes
{
    public enum NamedIdNodeType
    {
        Unknown,
        Variable,
        GenericFunction,
        ConcreteFunction,
        UnknownFunction,
        Constant
    }

    public class FunctionalVariableCallInfo
    {
        public FunctionalVariableCallInfo(IFunctionSignature signature, StateRefTo[] referenceToGenericArguments)
        {
            Signature = signature;
            ReferenceToGenericArguments = referenceToGenericArguments;
        }
        public readonly IFunctionSignature Signature;
        public StateRefTo[] ReferenceToGenericArguments;
    }
    /// <summary>
    /// Variable or constant or function
    /// </summary>
    public class NamedIdSyntaxNode : ISyntaxNode
    {
        /// <summary>
        /// type of id. Constant, variable or function
        /// </summary>
        public NamedIdNodeType IdType { get; set; } = NamedIdNodeType.Unknown;
        /// <summary>
        /// Content of id. Variable source, Function signature, or Constant value
        /// </summary>
        public object IdContent { get; set; }

        public FunnyType OutputType { get; set; }
        public int OrderNumber { get; set; }

        public NamedIdSyntaxNode(string id, Interval interval)
        {
            Id = id;
            Interval = interval;
        }

        public bool IsInBrackets { get; set; }
        public string Id { get; }
        public Interval Interval { get; set; }
        public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);    
        public IEnumerable<ISyntaxNode> Children => new ISyntaxNode[0];

        public override string ToString() => $"({OrderNumber}) {Id}:{OutputType}";
    }
}