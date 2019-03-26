using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Funny.Interpritation.Functions;
using Funny.Interpritation.Nodes;
using Funny.LexAnalyze;
using Funny.Parsing;
using Funny.Runtime;
using Funny.Tokenization;
using Funny.Types;

namespace Funny.Interpritation
{
    public class ExpressionReader
    {
        private readonly TreeAnalysis _treeAnalysis;
        private readonly LexFunction[] _lexTreeUserFuns;
        private readonly FunctionsDictionary _functions;

        private readonly Dictionary<string, VariableExpressionNode> _variables 
            = new Dictionary<string, VariableExpressionNode>();
        
        private readonly List<Equation> _equations = new List<Equation>();
        
        public static FunRuntime Interpritate(
            LexTree lexTree,
            IEnumerable<FunctionBase> predefinedFunctions, 
            GenericFunctionBase[] predefinedGenerics)
        {
            var functions = new FunctionsDictionary();
            foreach (var predefinedFunction in predefinedFunctions)
                functions.Add(predefinedFunction);
            foreach (var genericFunctionBase in predefinedGenerics)
                functions.Add(genericFunctionBase);

            var analysis = LexAnalyzer.Analyze(lexTree);
            
            var ans = new ExpressionReader(
                analysis,
                lexTree.UserFuns,
                functions,
                lexTree.VarSpecifications);
            
            ans.Interpritate();
            
            return new FunRuntime(
                equations: ans._equations,  
                variables:   ans._variables);
        }

        private ExpressionReader(
            TreeAnalysis treeAnalysis, 
            LexFunction[] lexTreeUserFuns, 
            FunctionsDictionary functions, 
            VariableInfo[] vars)
        {
            _treeAnalysis = treeAnalysis;
            _lexTreeUserFuns = lexTreeUserFuns;
            _functions = functions;
            foreach (var variable in treeAnalysis.AllVariables)
            {
                _variables.Add(
                    variable.Id,
                    new VariableExpressionNode(variable.Id, VarType.Real)
                    {
                        IsOutput =  variable.IsOutput
                    });
            }

            foreach (var variableTypeSpecification in vars)
            {
                if (_variables.ContainsKey(variableTypeSpecification.Id)) ;
                    _variables[variableTypeSpecification.Id].SetType(variableTypeSpecification.Type);
            }
        }
        
        private void Interpritate()
        {
            foreach (var userFun in _lexTreeUserFuns)
            {
                var prototype = GetFunctionPrototype(userFun);
                if (!_functions.Add(prototype))
                    throw new FunParseException($"Function {prototype} already exist");
            }

            foreach (var userFun in _lexTreeUserFuns)
            {
                var prototype = _functions.GetOrNull(userFun.Id, userFun.Args.Select(a=>a.Type).ToArray());
                
                ((FunctionPrototype)prototype).SetActual(GetFunction(userFun));
            }
            
            foreach (var equation in _treeAnalysis.OrderedEquations)
            {
                var reader = new SingleExpressionReader(_functions, _variables);
                    
                var expression = reader.ReadNode(equation.Equation.Expression);
                //ReplaceInputType
                if (_variables.ContainsKey(equation.Equation.Id))
                    _variables[equation.Equation.Id].SetType(expression.Type);
                _equations.Add(new Equation
                {
                    Expression = expression,
                    Id = equation.Equation.Id,
                    ReusingWithOtherEquations = equation.UsedInOtherEquations
                });
            }
        }

        private FunctionPrototype GetFunctionPrototype(LexFunction lexFunction) 
            => new FunctionPrototype(lexFunction.Id, lexFunction.OutputType,lexFunction.Args.Select(a=>a.Type).ToArray());

        private UserFunction GetFunction(LexFunction lexFunction)
        {
            
            var vars = new Dictionary<string, VariableExpressionNode>();
            foreach (var arg in lexFunction.Args) {
                vars.Add(arg.Id, new VariableExpressionNode(arg.Id, arg.Type));
            }
            var reader = new SingleExpressionReader(_functions, vars);
            var expression = reader.ReadNode(lexFunction.Node);
            ExpressionHelper.CheckForUnknownVariables(lexFunction.Args.Select(a=>a.Id).ToArray(), vars);
            return new UserFunction(lexFunction.Id, vars.Values.ToArray(), expression);
        }

        
    }
}