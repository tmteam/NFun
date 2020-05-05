using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Tic;
using NFun.Tic.SolvingStates;
using Array = NFun.Tic.SolvingStates.Array;

namespace NFun.TypeInferenceCalculator
{
    public static class DestructionFunctions
    {

        #region Destructiont

        public static void Destruction(SolvingNode[] toposorteNodes)
        {
            void Destruction(SolvingNode node)
            {
                if (node.State is ICompositeType composite)
                {
                    if (composite.Members.Any(m => m.State is RefTo))
                        node.State = composite.GetNonReferenced();

                    foreach (var member in composite.Members)
                        Destruction(member);
                }

                foreach (var ancestor in node.Ancestors.ToArray())
                    TryMergeDestructive(node, ancestor);
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
                Console.WriteLine($"{ancestorNode}=={descendantNode}. Skip");
                return;
            }

            var originAnc = nonRefAncestor.ToString();
            var originDes = nonRefDescendant.ToString();
            Console.WriteLine($"-dm: {originAnc} => {originDes}");


            switch (nonRefAncestor.State)
            {
                case Primitive concreteAnc:
                {
                    if (nonRefDescendant.State is Constrains c && c.Fits(concreteAnc))
                    {
                        Console.Write("p+c");
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
                        Console.Write("a+c");
                        nonRefDescendant.State = new RefTo(nonRefAncestor);
                    }
                    else if (nonRefDescendant.State is Array arrayDesc)
                    {
                        Console.Write("a+a");
                        TryMergeDestructive(arrayDesc.ElementNode, arrayAnc.ElementNode);
                    }
                    break;
                }

                case Fun funAnc:
                {
                    if (nonRefDescendant.State is Constrains constrainsDesc && constrainsDesc.Fits(funAnc))
                    {
                        Console.Write("f+c");
                        nonRefDescendant.State = new RefTo(nonRefAncestor);
                    }
                    else if (nonRefDescendant.State is Fun funDesc)
                    {
                        Console.Write("f+f");
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
                        Console.Write("c+p");
                        descendantNode.State = ancestorNode.State = nonRefAncestor.State = concreteDesc;
                    }

                    break;
                }

                case Constrains constrainsAnc when nonRefDescendant.State is Constrains constrainsDesc:
                {
                    Console.Write("c+c");

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
                    Console.Write("c+a");

                    if (constrainsAnc.Fits(arrayDes))
                        nonRefAncestor.State = ancestorNode.State = new RefTo(nonRefDescendant);

                    break;
                }
                case Constrains constrainsAnc when nonRefDescendant.State is Fun funDes:
                {
                    Console.Write("c+f");

                    if (constrainsAnc.Fits(funDes))
                        nonRefAncestor.State = ancestorNode.State = new RefTo(nonRefDescendant);

                    break;
                }

                default:
                {
                    Console.Write("no");
                    break;
                }
            }

            Console.WriteLine($"    {originAnc} + {originDes} = {nonRefDescendant.State}");
        }

        #endregion

        #region Finalize
        public static FinalizationResults FinalizeUp(SolvingNode[] toposortedNodes, SolvingNode[] outputNodes)
        {
            var typeVariables = new HashSet<SolvingNode>();
            var namedNodes = new List<SolvingNode>();
            var syntaxNodes = new List<SolvingNode>(toposortedNodes.Length);
            void Finalize(SolvingNode node)
            {
                if (node.State is RefTo)
                {
                    var originalOne = node.GetNonReference();

                    if (originalOne != node)
                    {
                        Console.WriteLine($"\t{node.Name}->r");
                        node.State = new RefTo(originalOne);
                    }

                    if (originalOne.State is IType)
                    {
                        node.State = originalOne.State;
                        Console.WriteLine($"\t{node.Name}->s");
                    }
                }
                else if (node.State is ICompositeType composite)
                {
                    if (composite.Members.Any(m => m.State is RefTo))
                    {
                        node.State = composite.GetNonReferenced();
                        Console.WriteLine($"\t{node.Name}->ar");
                    }

                    foreach (var member in composite.Members)
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

            SolveUselessGenerics(toposortedNodes, outputNodes);
            return new FinalizationResults(typeVariables.ToArray(), namedNodes.ToArray(), syntaxNodes.ToArray());
        }

        private static void SolveUselessGenerics(
            SolvingNode[] toposortedNodes,
            SolvingNode[] outputNodes)
        {
            //Если нерешенный тип участвует только во входных переменных
            //то этот тип можно попытаться привести к конкретному

            //находим нерешенные выходные типы
            var outputTypes = outputNodes
                .SelectMany(GetAllOutputTypes)
                .Where(t => t.State is Constrains)
                .Distinct()
                .ToArray(); //для отладки

            //находим входные типы которые НЕ участвуют в выходных типах
            var notSolved = toposortedNodes
                .Where(t => t.State is Constrains)
                .Except(outputTypes)
                .ToArray(); //для отладки

            //пытаемся их решить.
            foreach (var node in notSolved)
                node.State = ((Constrains)node.State).Solve();
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
    }
}