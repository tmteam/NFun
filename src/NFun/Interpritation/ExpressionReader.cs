using System.Collections.Generic;
using System.Linq;
using NFun.Interpritation.Functions;
using NFun.Interpritation.Nodes;
using NFun.LexAnalyze;
using NFun.ParseErrors;
using NFun.Parsing;
using NFun.Runtime;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation
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
            IEnumerable<GenericFunctionBase> predefinedGenerics)
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
                    new VariableExpressionNode(variable.Id, VarType.Real,Interval.Empty)
                    {
                        IsOutput =  variable.IsOutput
                    });
            }

            foreach (var variableTypeSpecification in vars)
            {
                if (_variables.ContainsKey(variableTypeSpecification.Id)) 
                    _variables[variableTypeSpecification.Id].SetType(variableTypeSpecification.Type);
            }
        }
        
        private void Interpritate()
        {
            foreach (var userFun in _lexTreeUserFuns)
            {
                var prototype = GetFunctionPrototype(userFun);
                if (!_functions.Add(prototype))
                    throw ErrorFactory.FunctionAlreadyExist(userFun);
            }

            foreach (var userFun in _lexTreeUserFuns)
            {
                var prototype = _functions.GetOrNull(userFun.Id, userFun.Args.Select(a=>a.Type).ToArray());
                
                ((FunctionPrototype)prototype).SetActual(GetFunction(userFun), userFun.Head.Interval);
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
                vars.Add(arg.Id, new VariableExpressionNode(arg.Id, arg.Type, new Interval()));
            }
            var reader = new SingleExpressionReader(_functions, vars);
            var expression = reader.ReadNode(lexFunction.Node);
            ExpressionHelper.CheckForUnknownVariables(lexFunction.Args.Select(a=>a.Id).ToArray(), vars);
            return new UserFunction(lexFunction.Id, vars.Values.ToArray(), expression);
        }

        
    }
}