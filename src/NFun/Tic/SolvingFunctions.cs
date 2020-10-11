using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using NFun.Tic.Errors;
using NFun.Tic.SolvingStates;

namespace NFun.Tic
{
    public static class SolvingFunctions
    {
        #region Merges

        public static ITicNodeState GetMergedStateOrNull(ITicNodeState stateA, ITicNodeState stateB)
        {
            if (stateB is ConstrainsState c && c.NoConstrains)
                return stateA;

            if (stateA is ITypeState typeA && typeA.IsSolved)
            {
                if (stateB is ITypeState typeB && typeB.IsSolved)
                    return !typeB.Equals(typeA) ? null : typeA;

                if (stateB is ConstrainsState constrainsB)
                    return !constrainsB.Fits(typeA) ? null : typeA;
            }

            switch (stateA)
            {
                case StateArray arrayA when stateB is StateArray arrayB:
                    Merge(arrayA.ElementNode, arrayB.ElementNode);
                    return arrayA;
                case StateFun funA when stateB is StateFun funB:
                {
                    if (funA.ArgsCount != funB.ArgsCount)
                        return null;

                    for (int i = 0; i < funA.ArgsCount; i++)
                        Merge(funA.ArgNodes[i], funB.ArgNodes[i]);
                    Merge(funA.RetNode, funB.RetNode);
                    return funA;
                }

                case ConstrainsState constrainsA when stateB is ConstrainsState constrainsB:
                    return constrainsB.MergeOrNull(constrainsA);
                case ConstrainsState _: 
                    return GetMergedStateOrNull(stateB, stateA);
                case StateRefTo refA:
                {
                    var state = GetMergedStateOrNull(refA.Node.State, stateB);
                    if (state == null) return null;
                    refA.Node.State = state;
                    return stateA;
                }
            }
            if (stateB is StateRefTo)
                return GetMergedStateOrNull(stateB, stateA);

            return null;
        }


        public static void Merge(TicNode main, TicNode secondary)
        {
            if(main==secondary)
                return;
            
            var res = GetMergedStateOrNull(main.State, secondary.State);
            if (res == null)
                throw TicErrors.CannotMerge(main, secondary);

            main.State = res;
            if (res is ITypeState t && t.IsSolved)
            {
                secondary.State = res;
                return;
            }

            main.Ancestors.AddRange(secondary.Ancestors);
            secondary.Ancestors.Clear();
            secondary.State = new StateRefTo(main);
        }

        public static void MergeGroup(TicNode[] cycleRoute)
        {
            var main = cycleRoute.First();

            foreach (var current in cycleRoute)
            {
                if (current == main)
                    continue;

                if (current.State is StateRefTo refState)
                {
                    if (!cycleRoute.Contains(refState.Node))
                        throw new InvalidOperationException();
                }
                else
                {
                    //merge main and current
                    main.State = GetMergedStateOrNull(main.State, current.State)
                                 ?? throw TicErrors.CannotMergeGroup(cycleRoute, main, current);

                }

                main.Ancestors.AddRange(current.Ancestors);
                current.Ancestors.Clear();

                if (!current.IsSolved)
                    current.State = new StateRefTo(main);
            }

            var newAncestors = main.Ancestors.Distinct()
                .SelectMany(r => r.Ancestors)
                .Where(r => !cycleRoute.Contains(r))
                .Distinct()
                .ToList();

            main.Ancestors.Clear();
            main.Ancestors.AddRange(newAncestors);
        }

        #endregion

        public static void PullConstraints(TicNode[] toposortedNodes)
        {
            foreach (var node in toposortedNodes)
            {
                if(node.IsMemberOfAnything)
                    continue;
                PullConstraintsRecursive(node);
            }
        }

        private static void PullConstraintsRecursive(TicNode descendant)
        {
            // ReSharper disable once ForCanBeConvertedToForeach
            //We have to use for, because collection can be modified
            for (var index = 0; index < descendant.Ancestors.Count; index++)
            {
                var ancestor = descendant.Ancestors[index];
                if (descendant == ancestor) continue;
                var res = ancestor.State.ApplyDescendant(PullConstraintsFunctions.SingleTone, ancestor, descendant);
                if (!res) throw TicErrors.IncompatibleTypes(ancestor, descendant);
            }

            if (descendant.State is ICompositeState composite)
                foreach (var member in composite.Members)
                    PullConstraintsRecursive(member);
        }

        public static void PushConstraints(TicNode[] toposortedNodes)
        {
            for (int i = toposortedNodes.Length - 1; i >= 0; i--)
            {
                var descendant = toposortedNodes[i];
                if (descendant.IsMemberOfAnything)
                    continue;

                PushConstraintsRecursive(descendant);
            }
        }

        private static void PushConstraintsRecursive(TicNode descendant)
        {
            if (descendant.State is ICompositeState composite)
                foreach (var member in composite.Members)
                    PushConstraintsRecursive(member);

            foreach (var ancestor in descendant.Ancestors.ToArray()) 
                PushConstraints(descendant, ancestor);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PushConstraints(TicNode descendant, TicNode ancestor)
        {
            if (descendant == ancestor)
                return;

            if (!ancestor.State.ApplyDescendant(PushConstraintsFunctions.Singletone, ancestor, descendant))
                throw TicErrors.IncompatibleNodes(ancestor, descendant);
        }
        
        public static bool Destruction(TicNode[] toposorteNodes)
        {
            int notSolvedCount = 0;
            for (int i = toposorteNodes.Length - 1; i >= 0; i--)
            {
                var descendant = toposorteNodes[i];
                if (descendant.IsMemberOfAnything)
                    continue;
                DestructionRecursive(descendant);
                if (!descendant.IsSolved)
                    notSolvedCount++;
            }
            return notSolvedCount == 0;
        }

        private static void DestructionRecursive(TicNode node)
        {
            ThrowIfTypeIsRecursive(node);

            if (node.State is ICompositeState composite)
            {
                if (composite.HasAnyReferenceMember) node.State = composite.GetNonReferenced();

                foreach (var member in composite.Members) DestructionRecursive(member);
            }

            foreach (var ancestor in node.Ancestors.ToArray())
            {
                Destruction(node, ancestor);
            }
        }

        public static void Destruction(TicNode descendantNode, TicNode ancestorNode)
        {
            var nonRefAncestor = ancestorNode.GetNonReference();
            var nonRefDescendant = descendantNode.GetNonReference();
            if (nonRefDescendant == nonRefAncestor)
                return;

            nonRefAncestor.State.ApplyDescendant(
                DestructionFunctions.Singletone, 
                nonRefAncestor,
                nonRefDescendant);
        }
        

        public static void BecomeReferenceFor(this TicNode referencedNode, TicNode original)
        {
            referencedNode = referencedNode.GetNonReference();
            original = original.GetNonReference();
            if (referencedNode.Type == TicNodeType.SyntaxNode)
                Merge(original, referencedNode);
            else
                Merge(referencedNode, original);
        }

        /// <summary>
        /// Transform constrains state to array state
        /// </summary>
        public static StateArray TransformToArrayOrNull(object descNodeName, ConstrainsState descendant)
        {
            if (descendant.NoConstrains)
            {
                var constrains = new ConstrainsState();
                var eName = "e" + descNodeName.ToString().ToLower() + "'";

                var node = TicNode.CreateTypeVariableNode(eName, constrains);
                return new StateArray(node);
            }
            else if (descendant.HasDescendant && descendant.Descedant is StateArray arrayEDesc)
            {
                if (arrayEDesc.Element is StateRefTo)
                {
                    var origin = arrayEDesc.ElementNode.GetNonReference();
                    if (origin.IsSolved)
                        return new StateArray(origin);
                }
                else if (arrayEDesc.ElementNode.IsSolved)
                {
                    return arrayEDesc;
                }
            }

            return null;
        }

        /// <summary>
        /// Transform constrains to fun state
        /// </summary>
        public static StateFun TransformToFunOrNull(object descNodeName, ConstrainsState descendant, StateFun ancestor)
        {
            if (descendant.NoConstrains)
            {
                var argNodes = new TicNode[ancestor.ArgsCount];
                for (int i = 0; i < ancestor.ArgsCount; i++)
                {
                    var argNode = TicNode.CreateTypeVariableNode("a'"+ descNodeName +"'"+i, new ConstrainsState());
                    argNode.Ancestors.Add(ancestor.ArgNodes[i]);
                    argNodes[i] = argNode;
                }

                var retNode = TicNode.CreateTypeVariableNode("r'"+ descNodeName, new ConstrainsState());
                retNode.Ancestors.Add(ancestor.RetNode);

                return StateFun.Of(argNodes, retNode);
            }

            if (descendant.Descedant is StateFun arrayEDesc
                && arrayEDesc.ArgsCount == ancestor.ArgsCount)
            {
                if (arrayEDesc.IsSolved)
                    return arrayEDesc;

                //For perfomance
                bool allArgsAreSolved = true;
                var nrArgNodes = new TicNode[arrayEDesc.ArgNodes.Length];
                for (int i = 0; i < arrayEDesc.ArgNodes.Length; i++)
                {
                    nrArgNodes[i] = arrayEDesc.ArgNodes[i].GetNonReference();
                    allArgsAreSolved = allArgsAreSolved && nrArgNodes[i].IsSolved;
                }
                
                var nrRetNode = arrayEDesc.RetNode.GetNonReference();
                if (allArgsAreSolved && nrRetNode.IsSolved)
                    return StateFun.Of(nrArgNodes, nrRetNode);
            }
            return null;
        }


        public static void ThrowIfTypeIsRecursive(TicNode node)
        {
            switch (node.State)
            {
                case StateRefTo refTo:
                    ThrowIfTypeIsRecursive(refTo.Node, 1);
                    break;
                case ICompositeState composite:
                    foreach (var member in composite.Members) 
                        ThrowIfTypeIsRecursive(member, 1);
                    break;
                default:
                    return;
            }
            
            static void ThrowIfTypeIsRecursive(TicNode node, int bypassNumber)
            {
                if (node.VisitMark == bypassNumber)
                {
                    var route = new HashSet<TicNode>();
                    FindRecursionTypeRoute(node, route);
                    throw TicErrors.RecursiveTypeDefinition(route.ToArray());
                }
                node.VisitMark = bypassNumber;
                switch (node.State)
                {
                    case StateRefTo refTo:
                        ThrowIfTypeIsRecursive(refTo.Node, bypassNumber);
                        break;
                    case ICompositeState composite:
                        foreach (var member in composite.Members) 
                            ThrowIfTypeIsRecursive(member, bypassNumber);
                        break;
                }
                node.VisitMark = 0;
            
            }
            
            static bool FindRecursionTypeRoute(TicNode node, HashSet<TicNode> nodes)
            {
                if (!nodes.Add(node))
                    return true;
            
                if (node.State is StateRefTo r)
                    return FindRecursionTypeRoute(r.Node, nodes);
            
                if (node.State is ICompositeState composite)
                {
                    foreach (var compositeMember in composite.Members)
                    {
                        if (FindRecursionTypeRoute(compositeMember, nodes))
                            return true;
                    }
                }
                nodes.Remove(node);
                return false;
            }
        }
    }
}