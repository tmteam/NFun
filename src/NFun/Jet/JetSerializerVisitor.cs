using System;
using System.Globalization;
using System.Linq;
using System.Text;
using NFun.Interpritation;
using NFun.Interpritation.Functions;
using NFun.Interpritation.Nodes;
using NFun.SyntaxParsing;
using NFun.Types;

namespace NFun.Jet
{
    public class JetSerializerVisitor : IExpressionNodeVisitor
    {
        readonly StringBuilder _ac = new StringBuilder();

        public StringBuilder GetResult() => _ac;

        public void VisitInput(VarInfo variable)
        {
            PrintArguments(variable.Attributes);
            _ac.Append(JetSerializationHelper.InputDefenitionId);
            _ac.Append(" ");
            _ac.Append(variable.Name);
            _ac.Append(" ");
            _ac.Append(variable.Type.ToJetTypeText());
            _ac.Append(" ");
        }

        public void Visit(Equation node)
        {
            _ac.Append(JetSerializationHelper.EquationId);
            _ac.Append(" ");
            _ac.Append(node.Id);
            _ac.Append(" ");
        }

        public void Visit(UserFunction function)
        {
            if (function.IsGeneric)
                _ac.Append(JetSerializationHelper.GenericUserFunctionId);
            else
                _ac.Append(JetSerializationHelper.UserFunctionId);

            _ac.Append(" ");
            _ac.Append(function.Name);
            _ac.Append(":");
            _ac.Append(function.ReturnType.ToJetTypeText());
            if (function.Variables.Any())
            {
                _ac.Append(":");
                _ac.Append(string.Join(":", function.Variables.Select(v => v.Name + ":" + v.Type.ToJetTypeText())));
            }
            _ac.Append(" ");
        }

        public void VisitLambda(UserFunction function)
        {
            if (function.IsGeneric)
                _ac.Append(JetSerializationHelper.GenericUserFunctionId);
            else
                _ac.Append(JetSerializationHelper.UserFunctionId);

            _ac.Append(" ");
            _ac.Append(function.Name);
            _ac.Append(":");
            _ac.Append(function.ReturnType.ToJetTypeText());
            if (function.Variables.Any())
            {
                _ac.Append(":");
                _ac.Append(string.Join(":", function.Variables.Select(v => v.Name + ":" + v.Type.ToJetTypeText())));
            }
            _ac.Append(" ");
        }

        public void Visit(CastExpressionNode node, VarType to, VarType @from)
        {
            _ac.Append(JetSerializationHelper.CastId);
            _ac.Append(" ");
            _ac.Append(to.ToJetTypeText());
            _ac.Append(" ");

        }

        public void Visit(VariableExpressionNode node)
        {
            _ac.Append(JetSerializationHelper.VariableId);
            _ac.Append(" ");
            _ac.Append(node.Source.Name);
            _ac.Append(" ");
        }

        public void Visit(ConstantExpressionNode node, object value)
        {
            _ac.Append(JetSerializationHelper.ConstId);
            _ac.Append(" ");
            _ac.Append(node.Type.ToJetTypeText());
            _ac.Append(" ");

            switch (node.Type.BaseType)
            {
                case BaseVarType.Bool:
                case BaseVarType.UInt8:
                case BaseVarType.UInt16:
                case BaseVarType.UInt32:
                case BaseVarType.UInt64:
                case BaseVarType.Int16:
                case BaseVarType.Int32:
                case BaseVarType.Int64:
                    _ac.Append(value);
                    break;
                case BaseVarType.Real:
                    _ac.Append( ((double)value).ToString(CultureInfo.InvariantCulture));
                    break;
                case BaseVarType.ArrayOf when  node.Type.ArrayTypeSpecification.VarType == VarType.Char:
                    _ac.Append(JetSerializationHelper.ToJetEscaped(value.ToString()));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _ac.Append(" ");
        }

        public void Visit(ArrayExpressionNode node, IExpressionNode[] items)
        {
            _ac.Append(JetSerializationHelper.ArrayId);
            _ac.Append(" ");
            _ac.Append(node.Type.ArrayTypeSpecification.VarType.ToJetTypeText());
            _ac.Append(" ");
            _ac.Append(items.Length);
            _ac.Append(" ");
        }

        public void Visit(IfElseExpressionNode node, int ifCount)
        {
            _ac.Append(JetSerializationHelper.IfId);
            _ac.Append(" ");
            _ac.Append(ifCount);
            _ac.Append(" ");
        }
        
        public void Visit(FunExpressionNode node, string name, VarType[] argTypes)
        {
            if (node.FunctionDefenition is GenericFunctionBase.ConcreteGenericFunction)
            {
                _ac.Append(JetSerializationHelper.GenericCallId);
                _ac.Append(" ");
                _ac.Append(name);
                _ac.Append(" ");
                _ac.Append(node.Type.ToJetTypeText());
                _ac.Append(" ");
                _ac.Append(argTypes.Length);
                _ac.Append(" ");
                foreach (var argType in argTypes)
                {
                    _ac.Append(argType.ToJetTypeText());
                    _ac.Append(" ");
                }
            }
            else
            {
                _ac.Append(JetSerializationHelper.FunCallId);
                _ac.Append(" ");
                _ac.Append(name);
                _ac.Append(":");
                _ac.Append(string.Join(":", new[] { node.Type }.Concat(argTypes).Select(JetSerializationHelper.ToJetTypeText)));
                _ac.Append(" ");

            }

        }


        public void Visit(FunArgumentExpressionNode node)
        {

        }

        public void Visit(FunVariableExpressionNode node, string name)
        {
            _ac.Append(JetSerializationHelper.FunVariableId);
            _ac.Append(" ");
            _ac.Append(node.Type.ToJetTypeText());
            _ac.Append(" ");
            _ac.Append(name);
            _ac.Append(" ");
        }

        private void PrintArguments(VarAttribute[] argumentsOrNull)
        {
            if(argumentsOrNull==null)
                return;
            foreach (var varAttribute in argumentsOrNull)
            {
                if (varAttribute.Value != null)
                {
                    _ac.Append(JetSerializationHelper.ParameterlessAttributeId);
                    _ac.Append(" ");
                    _ac.Append(varAttribute.Name);
                }
                else
                {
                    _ac.Append(JetSerializationHelper.AttributeWithParameterId);
                    _ac.Append(" ");
                    _ac.Append(varAttribute.Name);
                    _ac.Append(" ");
                    _ac.Append(varAttribute.Value);
                }
                _ac.Append(" ");
            }
        }
    }
}

