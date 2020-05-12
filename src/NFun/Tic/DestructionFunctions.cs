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
                    if (composite.Members.Any(m => m.State is RefTo))
                        node.State = composite.GetNonReferenced();

                    foreach (var member in composite.Members)
                        Destruction(member);
                }

                foreach (var ancestor in node.Ancestors.ToArray())
                {
                    //if(ancestor==node)
                    //    throw new InvalidOperationException("Ancestor cannot be equal to node itself");
                    TryMergeDestructive(node, ancestor);
                }
            }

            for (int i = toposorteNodes.Length - 1; i >= 0; i--)
            {
                var descendant = toposorteNodes[i];
                if (descendant.MemberOf.Any())
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

            var originAnc = nonRefAncestor.ToString();
            var originDes = nonRefDescendant.ToString();
            TraceLog.WriteLine(()=>$"-dm: {originAnc} => {originDes}");


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

            TraceLog.WriteLine(()=>$"    {originAnc} + {originDes} = {nonRefDescendant.State}");
        }

        #endregion

        #region Finalize
        public static FinalizationResults FinalizeUp(SolvingNode[] toposortedNodes, 
            SolvingNode[] outputNodes, SolvingNode[] inputNodes)
        {
            var typeVariables = new HashSet<SolvingNode>();
            var namedNodes = new List<SolvingNode>();
            var syntaxNodes = new List<SolvingNode>(toposortedNodes.Length);
            void Finalize(SolvingNode node)
            {
                node.ThrowIfTypeIsRecursive();

                if (node.State is RefTo)
                {
                    var originalOne = node.GetNonReference();

                    if (originalOne != node)
                    {
                        TraceLog.WriteLine($"\t{node.Name}->reduce ref");
                        node.State = new RefTo(originalOne);
                    }

                    if (originalOne.State is IType)
                    {
                        node.State = originalOne.State;
                        TraceLog.WriteLine($"\t{node.Name}->concretize ref to {node.State}");
                    }
                }
                
                if (node.State is ICompositeType composite)
                {
                    if (composite.Members.Any(m => m.State is RefTo))
                    {
                        TraceLog.Write($"\t{node.Name}->simplify composite ");
                        node.State = composite.GetNonReferenced();
                        TraceLog.Write($"{composite}->{node.State} \r\n");
                    }

                    foreach (var member in ((ICompositeType) node.State).Members)
                        Finalize(member);
                }
            }

            foreach (var node in toposortedNodes.Reverse())
            {
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
                    while (syntaxNodes.Count <= nodeId)
                        syntaxNodes.Add(null);
                    syntaxNodes[nodeId] = node;
                }
            }

            SolveUselessGenerics(toposortedNodes, outputNodes, inputNodes);
            return new FinalizationResults(typeVariables.ToArray(), namedNodes.ToArray(), syntaxNodes.ToArray());
        }

        private static void SolveUselessGenerics(
            SolvingNode[] toposortedNodes,
            SolvingNode[] outputNodes,
            SolvingNode[] inputNodes)
        {
            //Если нерешенный тип участвует только во входных переменных
            //то этот тип можно попытаться привести к конкретному

            //находим нерешенные выходные типы
            var outputTypes = outputNodes
                .SelectMany(GetAllOutputTypes)
                .Where(t => t.State is Constrains)
                .Distinct()
                .ToList(); //для отладки

            //Контрвариативные типы
            var contravariants = inputNodes
                .Where(t => t.State is Fun)
                .SelectMany(t => ((Fun) t.State).ArgNodes)
                .SelectMany(n=>n.GetAllLeafTypes())
                .Where(t => t.State is Constrains)
                .Except(outputTypes)
                .Distinct()
                .ToArray();

            foreach (var node in contravariants)
            {
                outputTypes.Add(node);
                node.State = ((Constrains) node.State).SolveContravariant();
            }


            //находим входные ковариативные типы которые НЕ участвуют в выходных типах
            var notSolved = toposortedNodes
                .Where(t => t.State is Constrains)
                .Except(outputTypes)
                .ToArray(); //для отладки
            
            if (TraceLog.IsEnabled && notSolved.Any())
            {
                TraceLog.WriteLine($"Finalize. outputs {outputTypes.Count}");
                foreach (var outputType in outputTypes) outputType.PrintToConsole();
                TraceLog.WriteLine($"Contravariants: {contravariants.Length}");
                foreach (var contra in contravariants) contra.PrintToConsole();
                TraceLog.WriteLine($"\r\nFinalize. solve {notSolved.Length}");
                foreach (var solving in notSolved) solving.PrintToConsole();
            }
            //пытаемся их решить.
            foreach (var node in notSolved)
                node.State = ((Constrains)node.State).SolveCovariant();
        }

        #endregion

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