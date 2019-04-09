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

        private readonly VariableDictionary _variables = new VariableDictionary();
        
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
            
            _variables = new VariableDictionary(vars.Select(v=> new VariableSource(v.Id,v.Type)));
            
            foreach (var variable in treeAnalysis.AllVariables)
            {
                _variables.TryAdd( new VariableSource(variable.Id, VarType.Real)
                    {
                        IsOutput =  variable.IsOutput
                    });
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
                _variables.GetSource(equation.Equation.Id).Type = expression.Type;
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
            var vars = new VariableDictionary();
            foreach (var lexFunctionArg in lexFunction.Args)
            {
                if (!vars.TryAdd(new VariableSource(lexFunctionArg)))
                    throw ErrorFactory.FunctionArgumentDuplicates(lexFunction, lexFunctionArg);
            }
            var reader = new SingleExpressionReader(_functions, vars);
            var expression = reader.ReadNode(lexFunction.Node);
            
            ExpressionHelper.CheckForUnknownVariables(
                lexFunction.Args.Select(a=>a.Id).ToArray(), vars);
            
            return new UserFunction(lexFunction.Id, vars.GetAllSources(), expression);
        }

        
    }
}