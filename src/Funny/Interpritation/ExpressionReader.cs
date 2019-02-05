using System;
using System.Collections.Generic;
using System.Linq;
using Funny.Parsing;
using Funny.Tokenization;

namespace Funny.Interpritation
{
    public class ExpressionReader
    {
        private readonly IEnumerable<LexEquatation> _lexEquatations;
        
        private readonly Dictionary<string, VariableExpressionNode> _variables 
            = new Dictionary<string, VariableExpressionNode>();
        
        private readonly Dictionary<string, Equatation> _equatations 
            = new Dictionary<string, Equatation>();
        
        public static Runtime.Runtime Interpritate(List<LexEquatation> lexEquatations)
        {
            var ans = new ExpressionReader(lexEquatations);
            ans.Interpritate();
            var result = OrderEquatationsOrThrow(lexEquatations, ans);
            return new Runtime.Runtime(result,  ans._variables);
        }

        private static Equatation[] OrderEquatationsOrThrow(List<LexEquatation> lexEquatations, ExpressionReader ans)
        {
            //now build dependencies map
            int[][] dependencyGraph = new int[lexEquatations.Count][];

            for (int i = 0; i < lexEquatations.Count; i++)
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

        private ExpressionReader(IEnumerable<LexEquatation> lexEquatations)
        {
            _lexEquatations = lexEquatations;
        }

        private void Interpritate()
        {
            int equatationNum = 0;
            foreach (var equatation in _lexEquatations)
            {
                var expression = InterpritateNode(equatation.Expression, equatationNum);
                _equatations.Add(equatation.Id.ToLower(), new Equatation
                {
                    Expression = expression,
                    Id = equatation.Id,
                });
                equatationNum++;
            }
        }
        private IExpressionNode InterpritateNode(LexNode node, int equatationNum)
        {
            if (node.Op.Is((TokType.Id)))
                return GetOrAddVariableNode(node.Op.Value, equatationNum);

            if (node.Op.Is(TokType.Number))
            {
                var val = node.Op.Value;
                try
                {
                    if (val.Length > 2)
                    {
                        val = val.Replace("_", null);
                    
                        if(val[1]=='b')
                            return new ValueExpressionNode(Convert.ToInt32(val.Substring(2), 2));
                        if(val[1]=='x')                        
                            return new ValueExpressionNode(Convert.ToInt32(val, 16));
                    }

                    if (val.EndsWith('.'))
                        throw new FormatException();
                    return new ValueExpressionNode(double.Parse(val));
                }
                catch (FormatException e)
                {
                    throw new ParseException("Cannot parse number \""+ node.Op.Value+"\"");
                }
                
            }

            Func<double, double, double> op = null;
            switch (node.Op.Type)
            {
                case TokType.Plus:
                    op = (a, b) => a + b;
                    break;
                case TokType.Minus:
                    op = (a, b) => a - b;
                     break;
                case TokType.Div:
                    op = (a, b) => a / b;
                    break;
                case TokType.Mult:
                    op = (a, b) => a * b;
                    break;
                case TokType.Pow:
                    op = Math.Pow; 
                    break;
                case TokType.Rema:
                    op = (a,b)=> a%b; 
                    break;
                case TokType.Equal:
                    op = (a, b) => a == b? 1:0;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var left =node.ChildrenNode.ElementAtOrDefault(0);
            if(left==null)
                throw new ParseException("a node is missing");

            var right = node.ChildrenNode.ElementAtOrDefault(1);
            if(right==null)
                throw new ParseException("b node is missing");

            var leftExpr = InterpritateNode(left,equatationNum);
            var rightExpr = InterpritateNode(right,equatationNum);
            
            return new OpExpressionNode(leftExpr, rightExpr, op);
        }

        private IExpressionNode GetOrAddVariableNode(string varName, int equatationNum)
        {
            var lower = varName.ToLower();
            VariableExpressionNode res;
            
            if (_variables.ContainsKey(lower))
                res= _variables[lower];
            else {
                res = new VariableExpressionNode(lower);
                _variables.Add(lower, res);            
            }
            res.AddEquatationNum(equatationNum);
            return res;
        }
    }
}