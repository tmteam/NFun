using System;
using System.Collections.Generic;
using System.Linq;
using Funny.Parsing;
using Funny.Runtime;
using Funny.Tokenization;

namespace Funny.Interpritation
{
    public class ExpressionReader
    {
        private readonly LexTree _lexTree;
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
            var ans = new ExpressionReader(lexTree, funDic);
            ans.Interpritate();

            var result = OrderEquatationsOrThrow(lexTree.Equatations, ans);
            return new FunRuntime(result,  ans._variables);
        }

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

        private ExpressionReader(LexTree lexTree,
            Dictionary<string, FunctionBase> functions)
        {
            _lexTree = lexTree;
            _functions = functions;
        }
        
        private void Interpritate()
        {
            foreach (var userFun in _lexTree.UserFuns)
                _functions.Add(userFun.Id, GetFunctionPrototype(userFun));

            foreach (var userFun in _lexTree.UserFuns)
            {
                var prototype = _functions[userFun.Id];
                ((FunctionPrototype)prototype).SetActual(GetFunction(userFun));
            }

            int equatationNum = 0;
            foreach (var equatation in _lexTree.Equatations)
            {
                var reader = new SingleExpressionReader(_functions, _variables);
                    
                var expression = reader.ReadNode(equatation.Expression, equatationNum);
                _equatations.Add(equatation.Id.ToLower(), new Equatation
                {
                    Expression = expression,
                    Id = equatation.Id,
                });
                equatationNum++;
            }
        }

        private FunctionPrototype GetFunctionPrototype(UserFunDef lexFunction) 
            => new FunctionPrototype(lexFunction.Id, lexFunction.Args.Length);

        private UserFunction GetFunction(UserFunDef lexFunction)
        {
            var vars = new Dictionary<string, VariableExpressionNode>();
            var reader = new SingleExpressionReader(_functions, vars);
            var expression = reader.ReadNode(lexFunction.Node, 0);
            //todo: compare input names and variables
            return new UserFunction(lexFunction.Id, vars.Values.ToArray(), expression);
        }
    }
}