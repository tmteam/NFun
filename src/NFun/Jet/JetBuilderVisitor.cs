using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NFun.Interpritation;
using NFun.Interpritation.Functions;
using NFun.Interpritation.Nodes;
using NFun.SyntaxParsing;
using NFun.Types;

namespace NFun.Jet
{
    public class JetBuilderVisitor : IExpressionNodeVisitor
    {
        private string ToShortType(VarType type)
        {
            switch (type.BaseType)
            {
                case BaseVarType.Empty:
                    return "???";
                case BaseVarType.Char:
                    return "c";
                case BaseVarType.Bool:
                    return "b";
                case BaseVarType.UInt8:
                    return "u8";
                case BaseVarType.UInt16:
                    return "u16";
                case BaseVarType.UInt32:
                    return "u32";
                case BaseVarType.UInt64:
                    return "u64";
                case BaseVarType.Int16:
                    return "i16";
                case BaseVarType.Int32:
                    return "i32";
                case BaseVarType.Int64:
                    return "i64";
                case BaseVarType.Real:
                    return "r";
                case BaseVarType.ArrayOf:
                    return "[" + ToShortType(type.ArrayTypeSpecification.VarType);
                case BaseVarType.Fun:
                    return "(" + string.Join(",", type.FunTypeSpecification.Inputs.Select(ToShortType)) + "):" +
                           ToShortType(type.FunTypeSpecification.Output);
                case BaseVarType.Generic:
                    return type.GenericId.Value.ToString();
                case BaseVarType.Any:
                    return "a";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        readonly StringBuilder _ac = new StringBuilder();

        public StringBuilder GetResult() => _ac;

        public void VisitInput(VarInfo variable)
        {
            PrintArguments(variable.Attributes);
            _ac.AppendLine();
            _ac.Append("i ");
            _ac.Append(variable.Name);
            _ac.Append(" ");
            _ac.Append(ToShortType(variable.Type));
            _ac.Append(" ");
        }

        public void Visit(Equation node)
        {
            _ac.AppendLine();
            _ac.Append("o ");
            _ac.Append(node.Id);
            _ac.Append(" ");
            _ac.Append(ToShortType(node.Expression.Type));
            _ac.Append(" ");
        }

        public void Visit(UserFunction function)
        {
            _ac.AppendLine();
            _ac.Append("u ");
            _ac.Append(function.Name);
            _ac.Append(":");
            _ac.Append(ToShortType(function.ReturnType));
            if (function.Variables.Any())
            {
                _ac.Append(":");
                _ac.Append(string.Join(":", function.Variables.Select(v => v.Name + ":" + ToShortType(v.Type))));
            }
            _ac.Append(" ");
        }

        public void Visit(CastExpressionNode node, VarType to, VarType @from)
        {
            _ac.Append("c ");
            _ac.Append(ToShortType(@from));
            _ac.Append(" ");
            _ac.Append(ToShortType(to));
            _ac.Append(" ");

        }

        public void Visit(VariableExpressionNode node)
        {
            _ac.Append("x ");
            _ac.Append(node.Source.Name);
            _ac.Append(" ");
        }

        public void Visit(ConstantExpressionNode node, object value)
        {
            _ac.Append("n ");
            _ac.Append(ToShortType(node.Type));
            _ac.Append(" ");
            _ac.Append(value);
            _ac.Append(" ");
        }

        public void Visit(ArrayExpressionNode node, IExpressionNode[] items)
        {
            _ac.Append("a ");
            _ac.Append(ToShortType(node.Type));
            _ac.Append(" ");
            _ac.Append(items.Length);
            _ac.Append(" ");
        }

        public void Visit(IfElseExpressionNode node, int ifCount)
        {
            _ac.Append("s ");
            _ac.Append(ifCount);
            _ac.Append(" ");
        }

        public void Visit(FunExpressionNode node, string name, VarType[] argTypes)
        {
            _ac.Append("f ");
            _ac.Append(name);
            _ac.Append(":");
            _ac.Append(string.Join(":", new[] {node.Type}.Concat(argTypes).Select(ToShortType)));
            _ac.Append(" ");
        }


        public void Visit(FunArgumentExpressionNode node)
        {

        }

        public void Visit(FunVariableExpressionNode node)
        {
            throw new NotImplementedException();
        }

        private void PrintArguments(VarAttribute[] argumentsOrNull)
        {
            if(argumentsOrNull==null)
                return;
            foreach (var varAttribute in argumentsOrNull)
            {
                if (varAttribute.Value != null)
                {
                    _ac.Append("q ");
                    _ac.Append(varAttribute.Name);
                }
                else
                {
                    _ac.Append("w ");
                    _ac.Append(varAttribute.Name);
                    _ac.Append(" ");
                    _ac.Append(varAttribute.Value);
                }
            }
        }
    }
}

