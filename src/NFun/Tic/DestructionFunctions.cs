using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Tic.Errors;
using NFun.Tic.SolvingStates;
using NFun.TypeInferenceCalculator;
using Array = NFun.Tic.SolvingStates.Array;

namespace NFun.Tic
{
    public static class DestructionFunctions
    {
        #region Destructiont

        public static void Destruction(SolvingNode[] toposorteNodes)
        {
            void Destruction(SolvingNode node)
            {
                node.ThrowIfTypeIsRecursive();

                if (node.State is ICompositeType composite)
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

        public static void TryMergeDestructive(SolvingNode descendantNode, SolvingNode ancestorNode)
        {
            var nonRefAncestor = ancestorNode.GetNonReference();
            var nonRefDescendant = descendantNode.GetNonReference();
            if (nonRefDescendant == nonRefAncestor)
            {
                TraceLog.WriteLine(()=>$"{ancestorNode.Name}=={descendantNode.Name}. Skip");
                return;
            }

#if DEBUG
            var originAnc = nonRefAncestor.ToString();
            var originDes = nonRefDescendant.ToString();
            TraceLog.WriteLine(()=>$"-dm: {originAnc} => {originDes}");
#endif


            switch (nonRefAncestor.State)
            {
                case Primitive concreteAnc:
                {
                    if (nonRefDescendant.State is Constrains c && c.Fits(concreteAnc))
                    {
                        TraceLog.Write("p+c: ");
                        if (c.Prefered != null && c.Fits(c.Prefered))
                            nonRefDescendant.State = c.Prefered;
                        else
                            nonRefDescendant.State = concreteAnc;
                    }

                    break;
                }

                case Array arrayAnc:
                {
                    if (nonRefDescendant.State is Constrains constrainsDesc && constrainsDesc.Fits(arrayAnc))
                    {
                        TraceLog.Write("a+c: ");
                        nonRefDescendant.State = new RefTo(nonRefAncestor);
                    }
                    else if (nonRefDescendant.State is Array arrayDesc)
                    {
                        TraceLog.Write("a+a: ");
                        TryMergeDestructive(arrayDesc.ElementNode, arrayAnc.ElementNode);
                    }
                    break;
                }

                case Fun funAnc:
                {
                    if (nonRefDescendant.State is Constrains constrainsDesc && constrainsDesc.Fits(funAnc))
                    {
                        TraceLog.Write("f+c: ");
                        nonRefDescendant.State = new RefTo(nonRefAncestor);
                    }
                    else if (nonRefDescendant.State is Fun funDesc)
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

                case Constrains constrainsAnc when nonRefDescendant.State is Primitive concreteDesc:
                {
                    if (constrainsAnc.Fits(concreteDesc))
                    {
                        TraceLog.Write("c+p: ");
                        descendantNode.State = ancestorNode.State = nonRefAncestor.State = concreteDesc;
                    }

                    break;
                }

                case Constrains constrainsAnc when nonRefDescendant.State is Constrains constrainsDesc:
                {
                    TraceLog.Write("c+c: ");

                    var result = constrainsAnc.MergeOrNull(constrainsDesc);
                    if (result == null)
                        return;

                    if (result is Primitive)
                    {
                        nonRefAncestor.State = nonRefDescendant.State = descendantNode.State = result;
                        return;
                    }

                    if (nonRefAncestor.Type == SolvingNodeType.TypeVariable ||
                        nonRefDescendant.Type != SolvingNodeType.TypeVariable)
                    {
                        nonRefAncestor.State = result;
                        nonRefDescendant.State = descendantNode.State = new RefTo(nonRefAncestor);
                    }
                    else
                    {
                        nonRefDescendant.State = result;
                        nonRefAncestor.State = ancestorNode.State = new RefTo(nonRefDescendant);
                    }

                    nonRefDescendant.Ancestors.Remove(nonRefAncestor);
                    descendantNode.Ancestors.Remove(nonRefAncestor);
                    break;
                }
                case Constrains constrainsAnc when nonRefDescendant.State is Array arrayDes:
                {
                    TraceLog.Write("c+a: ");

                    if (constrainsAnc.Fits(arrayDes))
                        nonRefAncestor.State = ancestorNode.State = new RefTo(nonRefDescendant);

                    break;
                }
                case Constrains constrainsAnc when nonRefDescendant.State is Fun funDes:
                {
                    TraceLog.Write("c+f: ");

                    if (constrainsAnc.Fits(funDes))
                        nonRefAncestor.State = ancestorNode.State = new RefTo(nonRefDescendant);

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
        public static FinalizationResults FinalizeUp(SolvingNode[] toposortedNodes, 
            IEnumerable<SolvingNode> outputNodes, IEnumerable<SolvingNode> inputNodes)
        {
            var typeVariables = new HashSet<SolvingNode>();
            var namedNodes  = new List<SolvingNode>();
            var syntaxNodes = new List<SolvingNode>(toposortedNodes.Length);
            void Finalize(SolvingNode node)
            {
                node.ThrowIfTypeIsRecursive();

                if (node.State is RefTo)
                {
                    var originalOne = node.GetNonReference();

                    if (originalOne != node)
                    {
#if DEBUG
                        TraceLog.WriteLine($"\t{node.Name}->fold ref");
#endif
                        node.State = new RefTo(originalOne);
                    }

                    if (originalOne.State is IType)
                    {
                        node.State = originalOne.State;
#if DEBUG
                        TraceLog.WriteLine($"\t{node.Name}->concretize ref to {node.State}");
#endif                        
                    }
                }
                
                if (node.State is ICompositeType composite)
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

                    foreach (var member in ((ICompositeType) node.State).Members)
                        Finalize(member);
                }
            }

            for (int i = toposortedNodes.Length - 1; i >= 0; i--)
            {
                var node = toposortedNodes[i];
                Finalize(node);

                foreach (var member in node.GetAllLeafTypes())
                {
                    if (member.Type == SolvingNodeType.TypeVariable && member.State is Constrains)
                    {
                        if (!typeVariables.Contains(member))
                            typeVariables.Add(member);
                    }
                }

                if (node.Type == SolvingNodeType.Named)
                    namedNodes.Add(node);
                else if (node.Type == SolvingNodeType.SyntaxNode)
                {
                    var nodeId = int.Parse(node.Name);
                    syntaxNodes.EnlargeAndSet(nodeId, node);
                }
            }

            SolveUselessGenerics(toposortedNodes, outputNodes, inputNodes);
            return new FinalizationResults(typeVariables, namedNodes, syntaxNodes);
        }
        
        private static void SolveUselessGenerics(
            IEnumerable<SolvingNode> toposortedNodes,
            IEnumerable<SolvingNode> outputNodes,
            IEnumerable<SolvingNode> inputNodes)
        {

            //We have to solve all generic types that are not output
            
            var outputTypes = GetAllNotSolvedOutputTypes(outputNodes);

            foreach (var node in GetAllNotSolvedContravariantLeafs(inputNodes))
            {
                //if contravariant not in output type list then
                //solve it and add to output types
                if(outputTypes.Add(node))
                    node.State = ((Constrains) node.State).SolveContravariant();
            }

            //Input covariant types that NOT referenced and are not members of any output types
            foreach (var node in toposortedNodes)
            {
                if(!(node.State is Constrains c))
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
        
        private static HashSet<SolvingNode> GetAllNotSolvedOutputTypes(IEnumerable<SolvingNode> outputNodes)
        {
            var answer = new HashSet<SolvingNode>();
            foreach (var outputType in outputNodes
                .SelectMany(GetAllOutputTypes)
                .Where(t => t.State is Constrains))
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

        private static IEnumerable<SolvingNode> GetAllNotSolvedContravariantLeafs(this IEnumerable<SolvingNode> nodes) =>
            nodes
                .Where(t => t.State is Fun)
                .SelectMany(t => ((Fun) t.State).ArgNodes)
                .SelectMany(n => n.GetAllLeafTypes())
                .Where(t => t.State is Constrains);

        private static IEnumerable<SolvingNode> GetAllLeafTypes(this SolvingNode node)
        {
            switch (node.State)
            {
                case ICompositeType composite:
                    return composite.AllLeafTypes;
                case RefTo _:
                    return new[] { node.GetNonReference() };
                default:
                    return new[] { node };
            }
        }

        private static IEnumerable<SolvingNode> GetAllOutputTypes(this SolvingNode node)
        {
            switch (node.State)
            {
                case Fun fun:
                    return new[] { fun.RetNode };
                case Array array:
                    return array.AllLeafTypes;
                case RefTo _:
                    return new[] { node.GetNonReference() };
                default:
                    return new[] { node };
            }
        }

        public static void ThrowIfTypeIsRecursive(this SolvingNode node)
        {
            switch (node.State)
            {
                case Primitive _:
                case Constrains _:
                    return;
                case RefTo _:
                case ICompositeType _:
                    ThrowIfTypeIsRecursive(node, new HashSet<SolvingNode>());
                    return;
                default:
                    throw new NotSupportedException();
            }
        }
        private static void ThrowIfTypeIsRecursive(this SolvingNode node, HashSet<SolvingNode> nodes)
        {
            if (!nodes.Add(node))
                throw TicErrors.RecursiveTypeDefenition(nodes.ToArray());
            if (node.State is RefTo r)
                ThrowIfTypeIsRecursive(r.Node, nodes);
            else if (node.State is ICompositeType composite)
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