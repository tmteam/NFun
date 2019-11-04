using System;
using System.Collections.Generic;
using System.Text;
using NFun.Interpritation.Functions;
using NFun.Interpritation.Nodes;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Types;

namespace NFun.Interpritation
{
    public interface IExpressionNodeVisitor
    {
        void VisitInput(VarInfo variable);
        void Visit(UserFunction function);
        void Visit(Equation node);
        
        
        void Visit(CastExpressionNode node, VarType to, VarType from);
        void Visit(ValueExpressionNode node, object value);
        void Visit(VariableExpressionNode node);
        void Visit(ArrayExpressionNode node, IExpressionNode[] items);
        void Visit(IfElseExpressionNode node, int caseCount);
        void Visit(FunExpressionNode node, string name, VarType[] argTypes);
        void Visit(FunArgumentExpressionNode node);
        void Visit(FunVariableExpressionNode node);
    }
}
