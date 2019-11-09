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
        void AddInput(VarInfo variable);
        void AddUserFunction(UserFunction function);
        void AddEquation(Equation node);
        void AddLambda(UserFunction function);


        void Visit(CastExpressionNode node, VarType to, VarType from);
        void Visit(ConstantExpressionNode node, object value);
        void Visit(VariableExpressionNode node);
        void Visit(ArrayExpressionNode node, IExpressionNode[] items);
        void Visit(IfElseExpressionNode node, int caseCount);
        void Visit(CallExpressionNode node, string name, VarType[] argTypes);
        void Visit(FunVariableExpressionNode node, string name);
    }
}
