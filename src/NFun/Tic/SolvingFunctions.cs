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

        public static void MergeGroup(IEnumerable<TicNode> cycleRoute)
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
                                 ?? throw TicErrors.CannotMergeGroup(cycleRoute.ToArray(), main, current);

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

                var markBefore = node.VisitMark;
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
                node.VisitMark = markBefore;
            
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
          #region Finalize
        public static ITicResults Finalize(
            TicNode[] toposortedNodes,
            IReadOnlyList<TicNode> outputNodes,
            IEnumerable<TicNode> inputNodes,
            List<TicNode> syntaxNodes, 
            Dictionary<string, TicNode> namedNodes)
        {
            var typeVariables = new List<TicNode>();

            int genericNodesCount = 0;
            const int typeVariableVisitedMark = 123;
            for (int i = toposortedNodes.Length - 1; i >= 0; i--)
            {
                var node = toposortedNodes[i];
                FinalizeRecursive(node);

                foreach (var member in node.GetAllLeafTypes())
                {
                    if(member.VisitMark== typeVariableVisitedMark)
                        continue;
                    member.VisitMark = typeVariableVisitedMark;
                    if (member.Type == TicNodeType.TypeVariable && member.State is ConstrainsState)
                    {
                        typeVariables.Add(member);
                    }
                }

                if (!node.IsSolved)
                    genericNodesCount++;
            }
            
            if (genericNodesCount == 0)
                return new TicResultsWithoutGenerics(namedNodes, syntaxNodes);
            
            SolveUselessGenerics(toposortedNodes, outputNodes, inputNodes);
            return new TicResultsWithGenerics(typeVariables, namedNodes, syntaxNodes);
        }

        private static void FinalizeRecursive(TicNode node)
        {
            if (node.State is StateRefTo refTo)
            {
                SolvingFunctions.ThrowIfTypeIsRecursive(refTo.Node);

                var originalOne = refTo.Node.GetNonReference();

                if (originalOne.State is ITypeState) 
                    node.State = originalOne.State;
                else if (originalOne != refTo.Node) 
                    node.State = new StateRefTo(originalOne);
            }

            if (node.State is ICompositeState composite)
            {
                SolvingFunctions.ThrowIfTypeIsRecursive(node);
                
                if (composite.HasAnyReferenceMember) 
                    node.State = composite.GetNonReferenced();
                
                foreach (var member in ((ICompositeState) node.State).Members) 
                    FinalizeRecursive(member);
            }
        }

        private static void SolveUselessGenerics(
            IEnumerable<TicNode> toposortedNodes,
            IReadOnlyList<TicNode> outputNodes,
            IEnumerable<TicNode> inputNodes)
        {
            
            //We have to solve all generic types that are not output

            const int outputTypeMark = 77;
            // Firstly - get all outputs
            var outputTypes = new List<TicNode>();
            foreach (var outputNode in outputNodes)
            {
                foreach (var outputType in outputNode.GetAllOutputTypes())
                {
                    if(outputType.VisitMark== outputTypeMark)
                        continue;
                    outputType.VisitMark = outputTypeMark;
                    if(outputType.State is ConstrainsState)
                        outputTypes.Add(outputType);
                }
            }

            //All not solved output types
            foreach (var inputNode in inputNodes)
            {
                if (inputNode.State is StateFun stateFun)
                {
                    foreach (var leafNode in stateFun.GetAllNotSolvedContravariantLeafs())
                    {
                        if(leafNode.VisitMark== outputTypeMark)
                            continue;
                        leafNode.VisitMark = outputTypeMark;

                        //if contravariant not in output type list then
                        //solve it and add to output types
                        leafNode.State = ((ConstrainsState) leafNode.State).SolveContravariant();
                        outputTypes.Add(leafNode);
                    }
                }
            }
            
            //Input covariant types that NOT referenced and are not members of any output types
            foreach (var node in toposortedNodes)
            {
                if(node.VisitMark== outputTypeMark)
                    continue;
                if(!(node.State is ConstrainsState c))
                    continue;
                node.State = c.SolveCovariant();
            }
        }

        #endregion

        private static IEnumerable<TicNode> GetAllNotSolvedContravariantLeafs(this StateFun fun) =>
            fun.ArgNodes
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PrintTrace(IEnumerable<TicNode> nodes)
        {
#if  DEBUG
            if(!TraceLog.IsEnabled)
                return;
            
            var alreadyPrinted = new HashSet<TicNode>();

            void ReqPrintNode(TicNode node)
            {
                if(node==null)
                    return;
                if(alreadyPrinted.Contains(node))
                    return;
                if(node.State is StateArray arr)
                    ReqPrintNode(arr.ElementNode);
                node.PrintToConsole();
                alreadyPrinted.Add(node);
            }

            foreach (var node in nodes)
                ReqPrintNode(node);
#endif

        }
    }
}