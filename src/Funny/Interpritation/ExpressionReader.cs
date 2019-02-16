using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Funny.Interpritation.Functions;
using Funny.ParserAnylizer;
using Funny.Parsing;
using Funny.Runtime;
using Funny.Tokenization;

namespace Funny.Interpritation
{
    public class ExpressionReader
    {
        private readonly LexTreeAnalyze _analytics;
        private readonly LexFunction[] _lexTreeUserFuns;
        private readonly Dictionary<string, FunctionBase> _functions;

        private readonly Dictionary<string, VariableExpressionNode> _variables 
            = new Dictionary<string, VariableExpressionNode>();
        
        private readonly Dictionary<string, Equatation> _equatations 
            = new Dictionary<string, Equatation>();
        
        public static FunRuntime Interpritate(
            LexTree lexTree, 
            IEnumerable<FunctionBase> predefinedFunctions)
        {
            var funDic = predefinedFunctions.ToDictionary((f) => f.Name.ToLower());
            var analyze = LexTreeAnlyzer.Analyze(lexTree.Equations);
            var ans = new ExpressionReader(
                analyze,
                lexTree.UserFuns,
                funDic);
            ans.Interpritate();
            
            return new FunRuntime(
                equatations: ans._equatations.Values.ToArray(),  
                variables:   ans._variables);
        }
/*
        private static Equatation[] OrderEquatationsOrThrow(LexEquatation[] lexEquatations, ExpressionReader ans)
        {
            //now build dependencies map
            int[][] dependencyGraph = new int[lexEquatations.Length][];

            for (int i = 0; i < lexEquatations.Length; i++)
            {
                if (ans._variables.TryGetValue(lexEquatations[i].Id.ToLower(), out var outvar))
                {
                    outvar.IsOutput = true;
                    ans._equatations.Values.ElementAt(i).ReusingWithOtherEquatations = true;
                    
                    dependencyGraph[i] = outvar.usedInOutputs.ToArray();
                }
                else
                    dependencyGraph[i] = Array.Empty<int>();
            }

            var sortResults = GraphTools.SortTopology(dependencyGraph);
            if (sortResults.HasCycle)
                throw new ParseException("Cycle dependencies: "
                                         + string.Join(',', sortResults.NodeNames));

            //Equatations calculation order
            var result = new List<Equatation>(dependencyGraph.Length);
            //applying sort order to equatations
            for (int i = 0; i < sortResults.NodeNames.Length; i++)
            {
                //order is reversed:
                var index =  sortResults.NodeNames[sortResults.NodeNames.Length - i-1];
                var element = ans._equatations.Values.ElementAt(index);
                result.Add(element);
            }
            return result.ToArray();
        }
*/

        private ExpressionReader(
            LexTreeAnalyze analytics, 
            LexFunction[] lexTreeUserFuns, 
            Dictionary<string, FunctionBase> functions)
        {
            _analytics = analytics;
            _lexTreeUserFuns = lexTreeUserFuns;
            _functions = functions;
            foreach (var variablesWithProperty in analytics.AllVariables)
            {
                _variables.Add(variablesWithProperty.Id,new VariableExpressionNode(variablesWithProperty.Id)
                {
                    IsOutput =  variablesWithProperty.IsOutput
                });
            }
        }

        private void Interpritate()
        {
            foreach (var userFun in _lexTreeUserFuns)
                _functions.Add(userFun.Id, GetFunctionPrototype(userFun));

            foreach (var userFun in _lexTreeUserFuns)
            {
                var prototype = _functions[userFun.Id];
                ((FunctionPrototype)prototype).SetActual(GetFunction(userFun));
            }

            int equatationNum = 0;
            foreach (var equatation in _analytics.OrderedEquatations)
            {
                var reader = new SingleExpressionReader(_functions, _variables);
                    
                var expression = reader.ReadNode(equatation.Equatation.Expression, equatationNum);
                _equatations.Add(equatation.Equatation.Id.ToLower(), new Equatation
                {
                    Expression = expression,
                    Id = equatation.Equatation.Id,
                    ReusingWithOtherEquatations = equatation.UsedInOtherEquatations
                });
                equatationNum++;
            }
        }

        private FunctionPrototype GetFunctionPrototype(LexFunction lexFunction) 
            => new FunctionPrototype(lexFunction.Id, lexFunction.Args.Length);

        private UserFunction GetFunction(LexFunction lexFunction)
        {
            var vars = new Dictionary<string, VariableExpressionNode>();
            var reader = new SingleExpressionReader(_functions, vars);
            var expression = reader.ReadNode(lexFunction.Node, 0);
            CheckForUnknownVariables(lexFunction.Args, vars);
            return new UserFunction(lexFunction.Id, vars.Values.ToArray(), expression);
        }

        private static void CheckForUnknownVariables(string[] args, Dictionary<string, VariableExpressionNode> vars)
        {
            var unknownVariables = vars.Values.Select(v => v.Name).Except(args);
            if (unknownVariables.Any())
            {
                if (unknownVariables.Count() == 1)
                    throw new ParseException($"Unknown variable \"{unknownVariables.First()}\"");
                else
                    throw new ParseException($"Unknown variables \"{string.Join(", ", unknownVariables)}\"");
            }
        }
    }
}