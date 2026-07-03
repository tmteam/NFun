using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun.SyntaxParsing.SyntaxNodes;

/// <summary>
/// Multiline try-catch-anyway block (statement form).
/// Unlike TryCatchSyntaxNode (expression form), this has block bodies and optional 'anyway' (finally).
/// </summary>
public class TryBlockSyntaxNode : ISyntaxNode {
    public ISyntaxNode TryBody { get; }
    /// <summary>null if no catch clause</summary>
    public ISyntaxNode CatchBody { get; }
    /// <summary>null for simple catch, non-null for catch(e)</summary>
    public string ErrorVariableName { get; }
    /// <summary>null if no anyway clause</summary>
    public ISyntaxNode AnywayBody { get; }

    public TryBlockSyntaxNode(ISyntaxNode tryBody, ISyntaxNode catchBody,
        string errorVariableName, ISyntaxNode anywayBody, Interval interval) {
        TryBody = tryBody;
        CatchBody = catchBody;
        ErrorVariableName = errorVariableName;
        AnywayBody = anywayBody;
        Interval = interval;
    }

    public FunnyType OutputType { get; set; }
    public int OrderNumber { get; set; }
    public int ParenthesesCount { get; set; }
    public Interval Interval { get; set; }
    public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
    public IEnumerable<ISyntaxNode> Children {
        get {
            yield return TryBody;
            if (CatchBody != null) yield return CatchBody;
            if (AnywayBody != null) yield return AnywayBody;
        }
    }
}
