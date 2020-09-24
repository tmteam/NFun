using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Tic.Errors;
using NFun.Tic.SolvingStates;
using NFun.TypeInferenceCalculator;

namespace NFun.Tic
{
    public static class DestructionFunctions
    {
        #region Destructiont

        public static void Destruction(TicNode[] toposorteNodes)
        {
            void Destruction(TicNode node)
            {
                node.ThrowIfTypeIsRecursive();

                if (node.State is ICompositeTypeState composite)
                {
                    if (composite.HasAnyReferenceMember)
                        node.State = composite.GetNonReferenced();

                    foreach (var member in composite.Members)
                        Destruction(member);
                }

                foreach (var ancestor in node.Ancestors.ToArray())
                {
                    TryMergeDestructive(node, ancestor);
                }
            }

            for (int i = toposorteNodes.Length - 1; i >= 0; i--)
            {
                var descendant = toposorteNodes[i];
                if (descendant.IsMemberOfAnything)
                    continue;
                Destruction(descendant);
            }
        }

        public static void TryMergeDestructive(TicNode descendantNode, TicNode ancestorNode)
        {
            var nonRefAncestor = ancestorNode.GetNonReference();
            var nonRefDescendant = descendantNode.GetNonReference();
            if (nonRefDescendant == nonRefAncestor)
            {
#if DEBUG
                TraceLog.WriteLine(()=>$"{ancestorNode.Name}=={descendantNode.Name}. Skip");
#endif
                return;
            }

#if DEBUG
            var originAnc = nonRefAncestor.ToString();
            var originDes = nonRefDescendant.ToString();
            TraceLog.WriteLine(()=>$"-dm: {originAnc} => {originDes}");
#endif


            switch (nonRefAncestor.State)
            {
                case StatePrimitive concreteAnc:
                {
                    if (nonRefDescendant.State is ConstrainsState c && c.Fits(concreteAnc))
                    {
                        TraceLog.Write("p+c: ");
                        if (c.Prefered != null && c.Fits(c.Prefered))
                            nonRefDescendant.State = c.Prefered;
                        else
                            nonRefDescendant.State = concreteAnc;
                    }

                    break;
                }

                case StateArray arrayAnc:
                {
                    if (nonRefDescendant.State is ConstrainsState constrainsDesc && constrainsDesc.Fits(arrayAnc))
                    {
                        TraceLog.Write("a+c: ");
                        nonRefDescendant.State = new StateRefTo(nonRefAncestor);
                    }
                    else if (nonRefDescendant.State is StateArray arrayDesc)
                    {
                        TraceLog.Write("a+a: ");
                        TryMergeDestructive(arrayDesc.ElementNode, arrayAnc.ElementNode);
                    }
                    break;
                }

                case StateFun funAnc:
                {
                    if (nonRefDescendant.State is ConstrainsState constrainsDesc && constrainsDesc.Fits(funAnc))
                    {
                        TraceLog.Write("f+c: ");
                        nonRefDescendant.State = new StateRefTo(nonRefAncestor);
                    }
                    else if (nonRefDescendant.State is StateFun funDesc)
                    {
                        TraceLog.Write("f+f: ");
                        if (funAnc.ArgsCount != funDesc.ArgsCount)
                            break;
                        for (int i = 0; i < funAnc.ArgsCount; i++)
                            TryMergeDestructive(funDesc.ArgNodes[i], funAnc.ArgNodes[i]);
                        TryMergeDestructive(funAnc.RetNode, funDesc.RetNode);
                    }
                    break;
                }

                case ConstrainsState constrainsAnc when nonRefDescendant.State is StatePrimitive concreteDesc:
                {
                    if (constrainsAnc.Fits(concreteDesc))
                    {
                        TraceLog.Write("c+p: ");
                        descendantNode.State = ancestorNode.State = nonRefAncestor.State = concreteDesc;
                    }

                    break;
                }

                case ConstrainsState constrainsAnc when nonRefDescendant.State is ConstrainsState constrainsDesc:
                {
                    TraceLog.Write("c+c: ");

                    var result = constrainsAnc.MergeOrNull(constrainsDesc);
                    if (result == null)
                        return;

                    if (result is StatePrimitive)
                    {
                        nonRefAncestor.State = nonRefDescendant.State = descendantNode.State = result;
                        return;
                    }

                    if (nonRefAncestor.Type == TicNodeType.TypeVariable ||
                        nonRefDescendant.Type != TicNodeType.TypeVariable)
                    {
                        nonRefAncestor.State = result;
                        nonRefDescendant.State = descendantNode.State = new StateRefTo(nonRefAncestor);
                    }
                    else
                    {
                        nonRefDescendant.State = result;
                        nonRefAncestor.State = ancestorNode.State = new StateRefTo(nonRefDescendant);
                    }

                    nonRefDescendant.Ancestors.Remove(nonRefAncestor);
                    descendantNode.Ancestors.Remove(nonRefAncestor);
                    break;
                }
                case ConstrainsState constrainsAnc when nonRefDescendant.State is StateArray arrayDes:
                {
                    TraceLog.Write("c+a: ");

                    if (constrainsAnc.Fits(arrayDes))
                        nonRefAncestor.State = ancestorNode.State = new StateRefTo(nonRefDescendant);

                    break;
                }
                case ConstrainsState constrainsAnc when nonRefDescendant.State is StateFun funDes:
                {
                    TraceLog.Write("c+f: ");

                    if (constrainsAnc.Fits(funDes))
                        nonRefAncestor.State = ancestorNode.State = new StateRefTo(nonRefDescendant);

                    break;
                }

                default:
                {
                    TraceLog.Write("no");
                    break;
                }
            }
#if DEBUG
            TraceLog.WriteLine(()=>$"    {originAnc} + {originDes} = {nonRefDescendant.State}");
#endif
        }

        #endregion

        #region Finalize
        public static FinalizationResults FinalizeUp(
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
                
                if (node.State is ICompositeTypeState composite)
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

                    foreach (var member in ((ICompositeTypeState) node.State).Members)
                        Finalize(member);
                }
            }

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
                {
                    var nodeId = (int)node.Name;
                    syntaxNodes.EnlargeAndSet(nodeId, node);
                }
            }

            SolveUselessGenerics(toposortedNodes, outputNodes, inputNodes);
            return new FinalizationResults(typeVariables, namedNodes, syntaxNodes);
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
                case ICompositeTypeState composite:
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
                case ICompositeTypeState _:
                    ThrowIfTypeIsRecursive(node, new HashSet<TicNode>());
                    return;
                default:
                    throw new NotSupportedException();
            }
        }
        private static void ThrowIfTypeIsRecursive(this TicNode node, HashSet<TicNode> nodes)
        {
            if (!nodes.Add(node))
                throw TicErrors.RecursiveTypeDefenition(nodes.ToArray());
            if (node.State is StateRefTo r)
                ThrowIfTypeIsRecursive(r.Node, nodes);
            else if (node.State is ICompositeTypeState composite)
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