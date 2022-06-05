using System;
using System.Collections.Generic;
using System.Linq;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Types;

namespace NFun.SyntaxParsing.Visitors; 

internal class ShortDescritpionVisitor : ISyntaxNodeVisitor<string> {
    private static readonly HashSet<String> UnaryOperators = new() 
        { "+", "-", "*", "/", "//", "%", "**", "<<", ">>", "|", "&", "^", "in", ">", "<", ">=", "<=", "==", "!=", "and", "xor", "or" };
    
    public string Visit(AnonymFunctionSyntaxNode node) 
        => $"rule({string.Join(",",node.ArgumentsDefinition.Select(a=>a.Accept(this)))})=..";
    public string Visit(ArraySyntaxNode node) => "[...]";
    public string Visit(EquationSyntaxNode node) => $"{node.Id} = ... ";
    public string Visit(FunCallSyntaxNode node) {
        string msg;
        if (node.IsOperator && UnaryOperators.Contains(node.Id))
            msg =  $"...{node.Id}...";
        else 
            msg = $"{node.Id}(...)";
        
        return node.BracketsCount > 0 ? $"({msg})" : msg;
    }
    public string Visit(IfThenElseSyntaxNode node) => "if (...) ... else ...";
    public string Visit(IfCaseSyntaxNode node) => "if (...) ...";

    public string Visit(ListOfExpressionsSyntaxNode node) {
        var strings = node.Expressions.Select(e => e.Accept(this));
        return $"{string.Join(",", strings)}";
    }

    public string Visit(ConstantSyntaxNode node) {
        if (node.OutputType.Equals(FunnyType.Text))
        {
            var str = node.Value.ToString();
            return $"'{(str.Length > 20 ? (str[17..] + "...") : str)}'";
        }

        return $"{node.Value}";
    }

    public string Visit(SyntaxTree node) => "Fun equations";

    public string Visit(TypedVarDefSyntaxNode node)
        => node.FunnyType.BaseType == BaseFunnyType.Empty
            ? node.Id
            : $"{node.Id}:{node.FunnyType}";

    public string Visit(UserFunctionDefinitionSyntaxNode node) => $"{node.Id}(...) = ...";
    public string Visit(VarDefinitionSyntaxNode node) => node.FunnyType.BaseType == BaseFunnyType.Empty
        ? node.Id
        : $"{node.Id}:{node.FunnyType}";
    
    public string Visit(NamedIdSyntaxNode node) => node.Id;
    public string Visit(ResultFunCallSyntaxNode node) => $"{node.ResultExpression.Accept(this)}(...)";
    public string Visit(SuperAnonymFunctionSyntaxNode node) => "{" + node.Body.Accept(this) + "}";
    public string Visit(StructFieldAccessSyntaxNode node) => $".{node.FieldName}";

    public string Visit(StructInitSyntaxNode node)
        => $"{{ {string.Join("; ", node.Fields.Select(f => $"{f.Name}={f.Node.Accept(this)}"))}}}";
    public string Visit(DefaultValueSyntaxNode node) => "default";
    public string Visit(GenericIntSyntaxNode node) => node.Value.ToString();
}