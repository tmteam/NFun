using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Tic.Errors;
using NFun.Tic.SolvingStates;

namespace NFun.Tic
{
    public static class FunalizeFunctions
    {
        #region Finalize
        public static ITicResults FinalizeUp(
            TicNode[] toposortedNodes, 
            IEnumerable<TicNode> outputNodes, IEnumerable<TicNode> inputNodes)
        {
            var typeVariables = new HashSet<TicNode>();
            var namedNodes  = new List<TicNode>();
            var syntaxNodes = new List<TicNode>(toposortedNodes.Length);
            void Finalize(TicNode node)
            {
                node.ThrowIfTypeIsRecursive();

                if (node.State is StateRefTo)
                {
                    var originalOne = node.GetNonReference();

                    if (originalOne != node)
                    {
#if DEBUG
                        TraceLog.WriteLine($"\t{node.Name}->fold ref");
#endif
                        node.State = new StateRefTo(originalOne);
                    }

                    if (originalOne.State is ITypeState)
                    {
                        node.State = originalOne.State;
#if DEBUG
                        TraceLog.WriteLine($"\t{node.Name}->concretize ref to {node.State}");
#endif                        
                    }
                }
                
                if (node.State is ICompositeState composite)
                {
                    if (composite.HasAnyReferenceMember)
                    {
#if DEBUG
                        TraceLog.Write($"\t{node.Name}->simplify composite ");
#endif
                        node.State = composite.GetNonReferenced();
#if DEBUG
                        TraceLog.Write($"{composite}->{node.State} \r\n");
#endif
                    }

                    foreach (var member in ((ICompositeState) node.State).Members)
                        Finalize(member);
                }
            }

            int genericNodesCount = 0;
            for (int i = toposortedNodes.Length - 1; i >= 0; i--)
            {
                var node = toposortedNodes[i];
                Finalize(node);

                foreach (var member in node.GetAllLeafTypes())
                {
                    if (member.Type == TicNodeType.TypeVariable && member.State is ConstrainsState)
                    {
                        typeVariables.Add(member);
                    }
                }

                if (node.Type == TicNodeType.Named)
                    namedNodes.Add(node);
                else if (node.Type == TicNodeType.SyntaxNode)
                    syntaxNodes.EnlargeAndSet((int)node.Name, node);

                if (!node.IsSolved)
                    genericNodesCount++;
            }
            if (genericNodesCount == 0)
                return new TicResultsWithoutGenerics(namedNodes, syntaxNodes);
            
            SolveUselessGenerics(toposortedNodes, outputNodes, inputNodes);
            return new TicResultsWithGenerics(typeVariables, namedNodes, syntaxNodes);
        }
        
        private static void SolveUselessGenerics(
            IEnumerable<TicNode> toposortedNodes,
            IEnumerable<TicNode> outputNodes,
            IEnumerable<TicNode> inputNodes)
        {

            //We have to solve all generic types that are not output
            
            var outputTypes = GetAllNotSolvedOutputTypes(outputNodes);

            foreach (var node in GetAllNotSolvedContravariantLeafs(inputNodes))
            {
                //if contravariant not in output type list then
                //solve it and add to output types
                if(outputTypes.Add(node))
                    node.State = ((ConstrainsState) node.State).SolveContravariant();
            }

            //Input covariant types that NOT referenced and are not members of any output types
            foreach (var node in toposortedNodes)
            {
                if(!(node.State is ConstrainsState c))
                    continue;
                if(outputTypes.Contains(node))
                    continue;
                node.State = c.SolveCovariant();
            }
            /*     
     #if DEBUG
     
                 if (TraceLog.IsEnabled && notSolved.Any())
                 {
                     TraceLog.WriteLine($"Finalize. outputs {outputTypes.Count}");
                     foreach (var outputType in outputTypes) outputType.PrintToConsole();
                     TraceLog.WriteLine($"Contravariants: {contravariants.Length}");
                     foreach (var contra in contravariants) contra.PrintToConsole();
                     TraceLog.WriteLine($"\r\nFinalize. solve {notSolved.Length}");
                     foreach (var solving in notSolved) solving.PrintToConsole();
                 }
     #endif
     */
        }
        
        private static HashSet<TicNode> GetAllNotSolvedOutputTypes(IEnumerable<TicNode> outputNodes)
        {
            var answer = new HashSet<TicNode>();
            foreach (var outputType in outputNodes
                .SelectMany(GetAllOutputTypes)
                .Where(t => t.State is ConstrainsState))
            {
                answer.Add(outputType);
            }

            return answer;
            //All not solved output types
            /*var outputTypes = outputNodes
                .SelectMany(GetAllOutputTypes)
                .Where(t => t.State is Constrains)
                .Distinct()
                .ToList();
            return outputTypes;*/
        }

        #endregion

        private static IEnumerable<TicNode> GetAllNotSolvedContravariantLeafs(this IEnumerable<TicNode> nodes) =>
            nodes
                .Where(t => t.State is StateFun)
                .SelectMany(t => ((StateFun) t.State).ArgNodes)
                .SelectMany(n => n.GetAllLeafTypes())
                .Where(t => t.State is ConstrainsState);

        private static IEnumerable<TicNode> GetAllLeafTypes(this TicNode node)
        {
            switch (node.State)
            {
                case ICompositeState composite:
                    return composite.AllLeafTypes;
                case StateRefTo _:
                    return new[] { node.GetNonReference() };
                default:
                    return new[] { node };
            }
        }

        private static IEnumerable<TicNode> GetAllOutputTypes(this TicNode node)
        {
            switch (node.State)
            {
                case StateFun fun:
                    return new[] { fun.RetNode };
                case StateArray array:
                    return array.AllLeafTypes;
                case StateRefTo _:
                    return new[] { node.GetNonReference() };
                default:
                    return new[] { node };
            }
        }

        public static void ThrowIfTypeIsRecursive(this TicNode node)
        {
            switch (node.State)
            {
                case StatePrimitive _:
                case ConstrainsState _:
                    return;
                case StateRefTo _:
                case ICompositeState _:
                    ThrowIfTypeIsRecursive(node, new HashSet<TicNode>());
                    return;
                default:
                    throw new NotSupportedException();
            }
        }
        private static void ThrowIfTypeIsRecursive(this TicNode node, HashSet<TicNode> nodes)
        {
            if (!nodes.Add(node))
                throw TicErrors.RecursiveTypeDefinition(nodes.ToArray());
            if (node.State is StateRefTo r)
                ThrowIfTypeIsRecursive(r.Node, nodes);
            else if (node.State is ICompositeState composite)
            {
                foreach (var compositeMember in composite.Members)
                {
                    ThrowIfTypeIsRecursive(compositeMember, nodes);
                }
            }
            nodes.Remove(node);
        }
    }
}