using System;
using System.Collections.Generic;
using System.Linq;
using NFun.BuiltInFunctions;
using NFun.Interpritation.Functions;
using NFun.Interpritation.Nodes;
using NFun.ParseErrors;
using NFun.Parsing;
using NFun.Runtime;
using NFun.Types;

namespace NFun.Interpritation
{
    class SingleExpressionReader
    {
        private readonly FunctionsDictionary _functions;
        private readonly VariableDictionary _variables;
        public SingleExpressionReader(
            FunctionsDictionary functions, 
            VariableDictionary variables)
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

            throw ErrorFactory.NotAnExpression(node);
        }

        private IExpressionNode GetAnonymFun(LexNode node)
        {
            var defenition = node.Children.ElementAtOrDefault(0);
            if (defenition == null)
                throw ErrorFactory.AnonymousFunDefenitionIsMissing(node);

            var expression = node.Children.ElementAtOrDefault(1);
            if(expression== null)
                throw ErrorFactory.AnonymousFunBodyIsMissing(node);
            
            var anonymVariables = new VariableDictionary(); 

            if (defenition.Type == LexNodeType.ListOfExpressions)
            {
                foreach (var arg in defenition.Children)
                {
                    var varNode =  ConvertToArgumentNodeOrThrow(arg);
                    if (!anonymVariables.TryAdd(new VariableSource(varNode.Name, varNode.Type)))
                        throw ErrorFactory.AnonymousFunctionArgumentDuplicates(varNode, defenition);
                }
            }
            else
            {
                var varNode =  ConvertToArgumentNodeOrThrow(defenition);
                anonymVariables.TryAdd(new VariableSource(varNode.Name, varNode.Type));
            }

            var originVariables = anonymVariables.GetAllSources().Select(s=>s.Name).ToArray();
            var scope = new SingleExpressionReader(_functions, anonymVariables);
            var expr = scope.ReadNode(expression);

            ExpressionHelper.CheckForUnknownVariables(originVariables, anonymVariables);
     
            var fun = new UserFunction("anonymous", anonymVariables.GetAllSources(), expr);
            return new FunVariableExpressionNode(fun, node.Interval);
        }

        private FunArgumentExpressionNode ConvertToArgumentNodeOrThrow(LexNode node)
        {
            if (node.Type == LexNodeType.Var)
                return new FunArgumentExpressionNode(node.Value, VarType.Real, node.Interval);
            if (node.Type == LexNodeType.TypedVar)
                return new FunArgumentExpressionNode(node.Value, (VarType) node.AdditionalContent, node.Interval);
            
            throw ErrorFactory.InvalidArgTypeDefenition(node);
        }
        
        private IExpressionNode GetOrAddVariableNode(LexNode varName)
        {
            var lower = varName.Value;
            var funVars = _functions.Get(lower);
            
            if (funVars.Count > 1)
                throw ErrorFactory.AmbiguousCallOfFunction(funVars, varName);
            
            if(funVars.Count==1)
                return new FunVariableExpressionNode(funVars[0], varName.Interval);

            return _variables.CreateVarNode(varName);
            
        }
        
        private IExpressionNode GetProcedureArrayNode(LexNode node)
        {
            var start = ReadNode(node.Children.ElementAt(0));
            
            var end = ReadNode(node.Children.ElementAt(1));
            
            var stepOrNull = node.Children.ElementAtOrDefault(2);

            if (stepOrNull == null)
                return new RangeIntFunction().CreateWithConvertionOrThrow(new[] {start, end}, node.Interval);

            var step = ReadNode(stepOrNull);
            if(step.Type== VarType.Real)
               return new RangeWithStepRealFunction().CreateWithConvertionOrThrow(new[] {start, end, step},node.Interval);
            
            if (step.Type!= VarType.Int)
                throw ErrorFactory.ArrayInitializerTypeMismatch(step.Type, node);

            
            return new RangeWithStepIntFunction().CreateWithConvertionOrThrow(new[] {start, end, step},node.Interval);
        }
        private IExpressionNode GetArrayNode(LexNode node)
        {
            var nodes = node.Children.Select(ReadNode).ToArray();
            return new ArrayExpressionNode(nodes,node.Interval);
        }
        private IExpressionNode GetIfThanElseNode(LexNode node)
        {
            var ifNodes = new List<IfCaseExpressionNode>();
            foreach (var ifNode in node.Children.Where(c => c.Is(LexNodeType.IfThen)))
            {
                var condition = ReadNode(ifNode.Children.First());
                var expr = ReadNode(ifNode.Children.Last());
                ifNodes.Add(new IfCaseExpressionNode(condition, expr,node.Interval));
            }

            var elseNode = ReadNode(node.Children.Last());
            return new IfThanElseExpressionNode(ifNodes.ToArray(), elseNode,elseNode.Interval);
        }

        private static IExpressionNode GetTextValueNode(LexNode node) 
            => new ValueExpressionNode(node.Value, node.Interval);

        private static IExpressionNode GetValueNode(LexNode node)
        {
            var val = node.Value;
            try
            {
                if (val.Length > 2)
                {
                    if(val == "true")
                        return new ValueExpressionNode(true, node.Interval);
                    if(val == "false")
                        return new ValueExpressionNode(false,node.Interval);
                    
                    val = val.Replace("_", null);

                    if (val[1] == 'b')
                        return new ValueExpressionNode(Convert.ToInt32(val.Substring(2), 2),node.Interval);
                    if (val[1] == 'x')
                        return new ValueExpressionNode(Convert.ToInt32(val, 16),node.Interval);
                }

                if (val.Contains('.'))
                {
                    if (val.EndsWith("."))
                        throw new FormatException();
                    return new ValueExpressionNode(double.Parse(val),node.Interval);
                }

                return new ValueExpressionNode(int.Parse(val),node.Interval);
            }
            catch (FormatException)
            {
                throw ErrorFactory.CannotParseNumber(node);
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
                throw ErrorFactory.FunctionNotFound(node, children, _functions);
            return function.CreateWithConvertionOrThrow(children, node.Interval);
        }

    }
}