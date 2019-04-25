using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Interpritation.Functions;
using NFun.ParseErrors;
using NFun.Parsing;
using NFun.Runtime;
using NFun.Types;

namespace NFun.Interpritation
{
    public class ExpressionReader
    {
        private readonly LexFunction[] _lexTreeUserFuns;
        private readonly LexTree _tree;
        private readonly FunctionsDictionary _functions;

        private readonly VariableDictionary _variables;
        
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
            
            var ans = new ExpressionReader(lexTree,functions);
            
            ans.Interpritate();
            
            return new FunRuntime(
                equations: ans._equations,  
                variables:   ans._variables);
        }

        private ExpressionReader(
            LexTree tree,
            FunctionsDictionary functions 
            )
        {
            _lexTreeUserFuns = tree.UserFuns;
            _tree = tree;
            _functions =  functions;
            _variables = new VariableDictionary(); 
           // _variables = new VariableDictionary(
           //     tree.VarSpecifications.Select());
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
            foreach (var lexRoot in _tree.Roots)
            {
                if (lexRoot is LexEquation equation) {
                    InterpriteEquation(equation);
                }
                else if (lexRoot is LexVarDefenition varDef)
                {
                    var variableSource = new VariableSource(varDef.Id, varDef.Type, varDef.Attributes);
                    if (!_variables.TryAdd(variableSource))
                    {
                        var allUsages = _variables.GetUsages(variableSource.Name);
                        
                        throw ErrorFactory.VariableIsDeclaredAfterUsing(allUsages);
                    }
                }
                else throw  new InvalidOperationException("Type "+ lexRoot+" is not supported as tree root");
            }
        }

        private void InterpriteEquation(LexEquation equation)
        {
            var reader = new SingleExpressionReader(_functions, _variables);
            var expression = reader.ReadNode(equation.Expression);

            var newSource = new VariableSource(equation.Id, VarType.Real, equation.Attributes)
            {
                IsOutput = true
            };
            if (!_variables.TryAdd(newSource))
            {
                //some equation referenced the source before
                var usages = _variables.GetUsages(equation.Id);
                if (usages.Source.IsOutput)
                    throw ErrorFactory.OutputNameWithDifferentCase(equation.Id, equation.Expression.Interval);
                else
                    throw ErrorFactory.CannotUseOutputValueBeforeItIsDeclared(usages, equation.Id);
            }

            //ReplaceInputType
            newSource.Type = expression.Type;
            _equations.Add(new Equation(equation.Id, expression));
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