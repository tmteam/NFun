using System;
using System.Collections.Generic;
using System.Linq;
using Funny.Interpritation;
using Funny.Parsing;

namespace Funny.ParserAnylizer
{
    public static class LexTreeAnlyzer
    {

        public static LexTreeAnalyze Analyze(LexEquatation[] lexEquatations)
        {
            var vars = SearchVariables(lexEquatations);
            foreach (var lexEquatation in lexEquatations)
            {
                if (!vars.ContainsKey(lexEquatation.Id))
                    vars[lexEquatation.Id] = new LexVarAnalytics(lexEquatation.Id);
                vars[lexEquatation.Id].IsOutput = true;
            }
            return new LexTreeAnalyze()
            {
                AllVariables = vars.Values,
                OrderedEquatations = OrderEquationsOrThrow(lexEquatations, vars)
            };
        }
        
        public static Dictionary<string, LexVarAnalytics> SearchVariables(LexEquatation[] lexEquatations)
        {
            var vars = new Dictionary<string, LexVarAnalytics>();
            for (var i = 0; i < lexEquatations.Length; i++)
            {
                var treeEquatation = lexEquatations[i];
                var varNodes = Dfs(treeEquatation.Expression, node => node.Is(LexNodeType.Var));
                foreach (var variableNode in  varNodes)
                {
                    if(!vars.ContainsKey(variableNode.Value))
                        vars.Add(variableNode.Value, new LexVarAnalytics(variableNode.Value));

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
                foreach (var lexNode in Dfs(nodeChild, condition))
                    yield return lexNode;
            }
        }
        
        public static  LexEquatationAnalytics[] OrderEquationsOrThrow(LexEquatation[] lexEquatations, Dictionary<string,LexVarAnalytics> vars)
        {
            //now build dependencies map
            var result =  new LexEquatationAnalytics[lexEquatations.Length];
            int[][] dependencyGraph = new int[lexEquatations.Length][];

            for (int i = 0; i < lexEquatations.Length; i++)
            {
                result[i] = new LexEquatationAnalytics();
                if (vars.TryGetValue(lexEquatations[i].Id, out var outvar))
                {
                    outvar.IsOutput = true;
                    result[i].UsedInOtherEquatations = true;
                    dependencyGraph[i] = outvar.UsedInOutputs.ToArray();
                }
                else
                    dependencyGraph[i] = Array.Empty<int>();
            }

            var sortResults = GraphTools.SortTopology(dependencyGraph);
            if (sortResults.HasCycle)
                throw new ParseException("Cycle dependencies: "
                                         + string.Join(',', sortResults.NodeNames));

            //Equatations calculation order
            //applying sort order to equatations
            for (int i = 0; i < sortResults.NodeNames.Length; i++)
            {
                //order is reversed:
                var index =  sortResults.NodeNames[sortResults.NodeNames.Length - i-1];
                var element = lexEquatations.ElementAt(index);
                
                result[i].Equatation =  element;
            }
            return result;
        }
        
    }

    public class LexEquatationAnalytics
    {
        public LexEquatation Equatation;
        public bool UsedInOtherEquatations;
    }
    public class LexVarAnalytics
    {
        public readonly HashSet<int> UsedInOutputs = new HashSet<int>();
        public readonly string Id;
        public bool IsOutput = false;
        public LexVarAnalytics(string id)
        {
            Id = id;
        }
    }
    public class LexTreeAnalyze
    {
        public bool[] AreUsedInOtherEquatations;
        public LexEquatationAnalytics[] OrderedEquatations;
        public IEnumerable<LexVarAnalytics> AllVariables;
    }
}