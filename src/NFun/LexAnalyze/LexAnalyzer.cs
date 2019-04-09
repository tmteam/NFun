using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Interpritation;
using NFun.ParseErrors;
using NFun.Parsing;
using NFun.Runtime;

namespace NFun.LexAnalyze
{
    public static class LexAnalyzer
    {
        public static TreeAnalysis Analyze(LexTree tree)
        {
            var vars = SearchVariables(tree.Equations);
            return new TreeAnalysis
            {
                AllVariables = vars.Values,
                OrderedEquations = OrderEquationsOrThrow(tree.Equations, vars)
            };
        }
        
        public static Dictionary<string, VarAnalysis> SearchVariables(LexEquation[] lexEquations)
        {
            var vars = new Dictionary<string, VarAnalysis>();
            foreach (var lexEquation in lexEquations)
            {
                vars.Add( lexEquation.Id, new VarAnalysis(lexEquation.Id, true));
            }
            for (var i = 0; i < lexEquations.Length; i++)
            {
                var treeEquation = lexEquations[i];
                
                var varNodes = Dfs(treeEquation.Expression, node => node.Is(LexNodeType.Var));
                foreach (var variableNode in  varNodes)
                {
                    if(!vars.ContainsKey(variableNode.Value))
                        vars.Add(variableNode.Value, new VarAnalysis(variableNode.Value));

                    vars[variableNode.Value].UsedInOutputs.Add(i);
                }
            }

            return vars;
        }
        public static  IEnumerable<LexNode> Dfs(LexNode node, Predicate<LexNode> condition)
        {
            if (condition(node))
                yield return node;
            foreach (var nodeChild in node.Children)
            {
                //todo Refactor it Remove LEx analizer at all.
                //dirtiest hack
                if (node.Is(LexNodeType.AnonymFun))
                {
                    var head = node.Children.FirstOrDefault();

                    List<string> deniedNames = null;
                
                    if (head.Is(LexNodeType.ListOfExpressions))
                        deniedNames = head.Children.Select(c => c.Value).ToList();
                    else 
                        deniedNames = new List<string>() {head.Value};
                    
                    foreach (var lexNode in Dfs(nodeChild, condition))
                    {
                        if (lexNode.Is(LexNodeType.Var) && deniedNames.Contains(lexNode.Value))
                            continue;
                        yield return lexNode;
                    }
                }
                else
                {
                    foreach (var lexNode in Dfs(nodeChild, condition))
                        yield return lexNode;
                }
            }
        }
        
        public static  EquationAnalysis[] OrderEquationsOrThrow(LexEquation[] lexEquations, Dictionary<string,VarAnalysis> vars)
        {
            //now build dependencies map
            var result =  new EquationAnalysis[lexEquations.Length];
            int[][] dependencyGraph = new int[lexEquations.Length][];

            for (int i = 0; i < lexEquations.Length; i++)
            {
                result[i] = new EquationAnalysis();
                if (vars.TryGetValue(lexEquations[i].Id, out var outvar))
                {
                    outvar.IsOutput = true;
                    result[i].UsedInOtherEquations = true;
                    dependencyGraph[i] = outvar.UsedInOutputs.ToArray();
                }
                else
                    dependencyGraph[i] = Array.Empty<int>();
            }

            var sortResults = GraphTools.SortTopology(dependencyGraph);

            if (sortResults.HasCycle)
            {
                var errorCycle = new List<LexEquation>();
                //Equations calculation order
                //applying sort order to Equations
                for (int i = 0; i < sortResults.NodeNames.Length; i++)
                {
                    //order is reversed:
                    var index =  sortResults.NodeNames[sortResults.NodeNames.Length - i-1];
                    var element = lexEquations.ElementAt(index);
                    errorCycle.Add(element);
                }
                throw ErrorFactory.CycleEquatationDependencies(errorCycle.ToArray());
            }
            
            //Equations calculation order
            //applying sort order to Equations
            for (int i = 0; i < sortResults.NodeNames.Length; i++)
            {
                //order is reversed:
                var index =  sortResults.NodeNames[sortResults.NodeNames.Length - i-1];
                var element = lexEquations.ElementAt(index);
                
                result[i].Equation =  element;
            }
            return result;
        }
        
    }
}