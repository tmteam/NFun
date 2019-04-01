using System;
using System.Collections.Generic;
using System.Linq;
using NFun.BuiltInFunctions;
using NFun.Interpritation.Functions;
using NFun.Interpritation.Nodes;
using NFun.Parsing;
using NFun.Runtime;
using NFun.Types;

namespace NFun.Interpritation
{
    class SingleExpressionReader
    {
        private readonly FunctionsDictionary _functions;
        private readonly Dictionary<string, VariableExpressionNode> _variables;
        public SingleExpressionReader(
            FunctionsDictionary functions, 
            Dictionary<string, VariableExpressionNode> variables)
        {
            _functions = functions;
            _variables = variables;
        }

        public  IExpressionNode ReadNode(LexNode node)
        {
            if(node.Is(LexNodeType.Var))
                return GetOrAddVariableNode(node);
            if(node.Is(LexNodeType.Fun))
                return GetFunNode(node);
            if(node.Is(LexNodeType.IfThanElse))
                return GetIfThanElseNode(node);
            if(node.Is(LexNodeType.Number))
                return GetValueNode(node);
            if(node.Is(LexNodeType.Text))
                return GetTextValueNode(node);
            if(node.Is(LexNodeType.ArrayInit))
                return GetArrayNode(node);
            if(node.Is(LexNodeType.ProcArrayInit))
                return GetProcedureArrayNode(node);
            if(node.Is(LexNodeType.AnonymFun))
                return GetAnonymFun(node);
            
            throw new FunParseException($"{node} is not an expression");
        }

        private IExpressionNode GetAnonymFun(LexNode node)
        {
            var defenition = node.Children.ElementAtOrDefault(0);
            if(defenition==null)
                throw new FunParseException("Anonymous fun defenition is missing");

            var expression = node.Children.ElementAtOrDefault(1);
            if(expression== null)
                throw new FunParseException("Anonymous fun body is missing");
            
            var variablesDictionary = new Dictionary<string, VariableExpressionNode>();
            
            if (defenition.Type == LexNodeType.ListOfExpressions)
            {
                foreach (var arg in defenition.Children)
                {
                    var varNode =  ConvertToVarNodeOrThrow(arg);
                    variablesDictionary.Add(varNode.Name, varNode);
                }
            }
            else
            {
                var varNode =  ConvertToVarNodeOrThrow(defenition);
                variablesDictionary.Add(varNode.Name, varNode);
            }

            var originVariables = variablesDictionary.Keys.ToArray();
            var scope = new SingleExpressionReader(_functions, variablesDictionary);
            var expr = scope.ReadNode(expression);

            ExpressionHelper.CheckForUnknownVariables(originVariables, variablesDictionary);
     
            var fun = new UserFunction("anonymous", variablesDictionary.Values.ToArray(), expr);
            return new FunVariableExpressionNode(fun);
        }

        private VariableExpressionNode ConvertToVarNodeOrThrow(LexNode defenition)
        {
            if (defenition.Type == LexNodeType.Var)
                return new VariableExpressionNode(defenition.Value, VarType.Real);
            else if(defenition.Type== LexNodeType.TypedVar)
                return new VariableExpressionNode(defenition.Value, (VarType)defenition.AdditionalContent);
            else
                throw new FunParseException(defenition + " is  not valid fun arg");
        }
        
        private IExpressionNode GetOrAddVariableNode(LexNode varName)
        {
            var lower = varName.Value;
            var funVars = _functions.Get(lower);
            
            if(funVars.Count>1)
                throw new FunParseException($"Ambiguous call of function with name: {lower}");
            if(funVars.Count==1)
                return new FunVariableExpressionNode(funVars[0]);   
            
            if (_variables.ContainsKey(lower))
                return _variables[lower];
            else {
                var res = new VariableExpressionNode(lower, VarType.Real);
                _variables.Add(lower, res);
                return res;
            }
        }
        
        private IExpressionNode GetProcedureArrayNode(LexNode node)
        {
            var start = ReadNode(node.Children.ElementAt(0));
            
            var end = ReadNode(node.Children.ElementAt(1));
            
            var stepOrNull = node.Children.ElementAtOrDefault(2);

            if (stepOrNull == null)
                return new RangeIntFunction().CreateWithConvertionOrThrow(new[] {start, end});

            var step = ReadNode(stepOrNull);
            if(step.Type== VarType.Real)
               return new RangeWithStepRealFunction().CreateWithConvertionOrThrow(new[] {start, end, step});
            
            if (step.Type!= VarType.Int)
                throw new FunParseException("Array procedure initializator has to be int types only. Example: [1..5..2]");
            
            return new RangeWithStepIntFunction().CreateWithConvertionOrThrow(new[] {start, end, step});
        }
        private IExpressionNode GetArrayNode(LexNode node)
        {
            var nodes = node.Children.Select(ReadNode).ToArray();
            return new ArrayExpressionNode(nodes);
        }
        private IExpressionNode GetIfThanElseNode(LexNode node)
        {
            var ifNodes = new List<IfCaseExpressionNode>();
            foreach (var ifNode in node.Children.Where(c => c.Is(LexNodeType.IfThen)))
            {
                var condition = ReadNode(ifNode.Children.First());
                var expr = ReadNode(ifNode.Children.Last());
                ifNodes.Add(new IfCaseExpressionNode(condition, expr));
            }

            var elseNode = ReadNode(node.Children.Last());
            return new IfThanElseExpressionNode(ifNodes.ToArray(), elseNode);
        }

        private static IExpressionNode GetTextValueNode(LexNode node) 
            => new ValueExpressionNode(node.Value);

        private static IExpressionNode GetValueNode(LexNode node)
        {
            var val = node.Value;
            try
            {
                if (val.Length > 2)
                {
                    if(val == "true")
                        return new ValueExpressionNode(true);
                    if(val == "false")
                        return new ValueExpressionNode(false);
                    
                    val = val.Replace("_", null);

                    if (val[1] == 'b')
                        return new ValueExpressionNode(Convert.ToInt32(val.Substring(2), 2));
                    if (val[1] == 'x')
                        return new ValueExpressionNode(Convert.ToInt32(val, 16));
                }

                if (val.Contains('.'))
                {
                    if (val.EndsWith("."))
                        throw new FormatException();
                    return new ValueExpressionNode(double.Parse(val));
                }

                return new ValueExpressionNode(int.Parse(val));
            }
            catch (FormatException e)
            {
                throw new FunParseException("Cannot parse number \"" + node.Value + "\"");
            }
        }

        private IExpressionNode GetFunNode(LexNode node)
        {
            var id = node.Value.ToLower();
            
            var children= new List<IExpressionNode>();
            var childrenTypes = new List<VarType>();
            foreach (var argLexNode in node.Children)
            {
                var argNode = ReadNode(argLexNode);
                children.Add(argNode);
                childrenTypes.Add(argNode.Type);
            }

            var function = _functions.GetOrNull(id, childrenTypes.ToArray());
            if (function == null)
                throw new FunParseException($"Function {id}({string.Join(", ", childrenTypes.ToArray())}) is not defined");
            return function.CreateWithConvertionOrThrow(children);
        }

    }
}