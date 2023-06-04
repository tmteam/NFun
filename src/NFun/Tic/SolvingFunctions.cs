using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using NFun.Tic.Errors;
using NFun.Tic.SolvingStates;
using NFun.Tic.Stages;

namespace NFun.Tic;

public static class SolvingFunctions {
    #region Merges

    public static ITicNodeState GetMergedStateOrNull(ITicNodeState stateA, ITicNodeState stateB) {
        if (stateB is ConstrainsState c && c.NoConstrains)
            return stateA;

        if (stateA is ITypeState typeA && !typeA.IsMutable)
        {
            if (stateB is ITypeState typeB && !typeB.IsMutable)
                return !typeB.Equals(typeA) ? null : typeA;

            if (stateB is ConstrainsState constrainsB)
                return !constrainsB.CanBeConvertedTo(typeA) ? null : typeA;
        }

        switch (stateA)
        {
            case StateArray arrayA when stateB is StateArray arrayB:
                MergeInplace(arrayA.ElementNode, arrayB.ElementNode);
                return arrayA;
            case StateFun funA when stateB is StateFun funB:
            {
                if (funA.ArgsCount != funB.ArgsCount)
                    return null;

                for (int i = 0; i < funA.ArgsCount; i++)
                    MergeInplace(funA.ArgNodes[i], funB.ArgNodes[i]);
                MergeInplace(funA.RetNode, funB.RetNode);
                return funA;
            }
            case StateStruct strA when stateB is StateStruct strB:
            {
                var result = new Dictionary<string, TicNode>();
                foreach (var (key, value) in strA.Fields)
                {
                    var bNode = strB.GetFieldOrNull(key);
                    if (bNode != null)
                        MergeInplace(value, bNode);
                    else if (strA.IsFrozen || strB.IsFrozen)
                        return null;
                    result.Add(key, value);
                }

                foreach (var (key, value) in strB.Fields)
                    result.TryAdd(key, value);

                return new StateStruct(result, isFrozen: false);
            }
            case ConstrainsState constrainsA when stateB is ConstrainsState constrainsB:
                return constrainsB.MergeOrNull(constrainsA);
            case ConstrainsState:
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

    /// <summary>
    /// Merge two nodes. Both of them become equiualent.
    ///
    /// In complex situation, 'secondary' node becomes reference to 'main' node, if it is possible
    /// </summary>
    public static void MergeInplace(TicNode main, TicNode secondary) {
        if (main == secondary)
            return;
        if (main.State is StateRefTo)
        {
            var nonreferenceMain = main.GetNonReference();
            var nonreferenceSecondary = secondary.GetNonReference();
            MergeInplace(nonreferenceMain, nonreferenceSecondary);
            return;
        }

        if (secondary.GetNonReference() == main)
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

        main.AddAncestors(secondary.Ancestors.Where(a => a != main));
        secondary.ClearAncestors();
        secondary.State = new StateRefTo(main);
    }

    /// <summary>
    /// Merge all node states. First non ref state (or first state) called 'main'
    /// 'main' state takes all constrains and ancestors
    ///
    /// All other nodes refs to 'main'
    ///
    /// Returns 'main'
    /// </summary>
    public static TicNode MergeGroup(IEnumerable<TicNode> cycleRoute) {
        var main = cycleRoute.FirstOrDefault(c => c.State is not StateRefTo) ?? cycleRoute.First();
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
                main.State = GetMergedStateOrNull(main.State, current.State) ??
                             throw TicErrors.CannotMerge(main, current);
            }

            main.AddAncestors(current.Ancestors.Where(c => c != main));
            current.ClearAncestors();

            if (!current.IsSolved)
                current.State = new StateRefTo(main);
        }

        var newAncestors = main.Ancestors
            .Where(r => !cycleRoute.Contains(r))
            .Distinct()
            .ToList();

        main.ClearAncestors();
        main.AddAncestors(newAncestors);
        return main;
    }

    #endregion


    public static void PullConstraints(TicNode[] toposortedNodes) {
        foreach (var node in toposortedNodes)
        {
            if (node.IsMemberOfAnything)
                continue;
            PullConstraintsRecursive(node);
        }
    }

    private static void PullConstraintsRecursive(TicNode node) {
        // micro optimization. node.Ancestors.ToArray() is very expensive operation
        // but cases of 0 or 1 ancestors are most common
        var ancSize = node.Ancestors.Count;
        if (ancSize == 1)
        {
            PullConstrains(node, node.Ancestors[0]);
        }
        else if (ancSize > 0)
        {
            // We have to use ToArray() option, since some node ancestors can be removed
            // during the operation
            foreach (var ancestor in node.Ancestors.ToArray())
            {
                PullConstrains(node, ancestor);
            }
        }

        if (node.State is ICompositeState composite)
            foreach (var member in composite.Members)
                PullConstraintsRecursive(member);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PullConstrains(TicNode descendant, TicNode ancestor) {
        if (descendant == ancestor) return;
        var res = PullConstraintsFunctions.Singleton.Invoke(ancestor, descendant);
        if (!res) throw TicErrors.IncompatibleTypes(ancestor, descendant);
    }

    public static void PushConstraints(TicNode[] toposortedNodes) {
        for (int i = toposortedNodes.Length - 1; i >= 0; i--)
        {
            var descendant = toposortedNodes[i];
            if (descendant.IsMemberOfAnything)
                continue;

            PushConstraintsRecursive(descendant);
        }
    }

    private static void PushConstraintsRecursive(TicNode node) {

        if (node.State is ICompositeState composite)
        {
            var oldMakr = node.VisitMark;
            if (oldMakr == 1567)
                throw TicErrors.RecursiveTypeDefinition(new[]{ node});
            node.VisitMark = 1567;
            foreach (var member in composite.Members)
                PushConstraintsRecursive(member);
            node.VisitMark = oldMakr;
        }

        // micro optimization. node.Ancestors.ToArray() is very expensive operation
        // but cases of 0 or 1 ancestors are most common
        var ancSize = node.Ancestors.Count;
        if (ancSize == 1)
        {
            PushConstraints(node, node.Ancestors[0]);
        }
        else if (ancSize > 0)
        {
            // We have to use ToArray() option, since some node ancestors can be removed
            // during the operation
            foreach (var ancestor in node.Ancestors.ToArray())
                PushConstraints(node, ancestor);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PushConstraints(TicNode descendant, TicNode ancestor) {
        if (descendant == ancestor)
            return;

        if (!PushConstraintsFunctions.Singleton.Invoke(ancestor, descendant))
            throw TicErrors.IncompatibleNodes(ancestor, descendant);
    }

    public static bool Destruction(TicNode[] toposorteNodes) {
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

    private static void DestructionRecursive(TicNode node) {
        ThrowIfRecursiveTypeDefinition(node);

        if (node.State is ICompositeState composite)
        {
            if (composite.HasAnyReferenceMember) node.State = composite.GetNonReferenced();
            foreach (var member in composite.Members) DestructionRecursive(member);
        }

        // micro optimization. node.Ancestors.ToArray() is very expensive operation
        // but cases of 0 or 1 ancestors are most common
        var ancSize = node.Ancestors.Count;
        if (ancSize == 1)
        {
            Destruction(node, node.Ancestors[0]);
        }
        else if (ancSize > 0)
        {
            // We have to use ToArray() option, since some node ancestors can be removed
            // during the operation
            foreach (var ancestor in node.Ancestors.ToArray())
                Destruction(node, ancestor);
        }
    }

    public static bool Destruction(TicNode descendantNode, TicNode ancestorNode) {
        var nonRefAncestor = ancestorNode.GetNonReference();
        var nonRefDescendant = descendantNode.GetNonReference();
        if (nonRefDescendant == nonRefAncestor)
            return true;
        return DestructionFunctions.Singleton.Invoke(nonRefAncestor, nonRefDescendant);
    }


    public static void BecomeReferenceFor(this TicNode referencedNode, TicNode original) {
        referencedNode = referencedNode.GetNonReference();
        original = original.GetNonReference();
        if (referencedNode.Type == TicNodeType.SyntaxNode)
            MergeInplace(original, referencedNode);
        else
            MergeInplace(referencedNode, original);
    }

    /// <summary>
    /// Transform constrains state to array state
    /// </summary>
    public static StateArray TransformToArrayOrNull(object descNodeName, ConstrainsState descendant) {
        //todo - we can put constrains of calling side here, or on a calling site
        if (descendant.NoConstrains)
        {
            var constrains = ConstrainsState.Empty;
            var eName = "e" + descNodeName.ToString().ToLower() + "'";

            var node = TicNode.CreateTypeVariableNode(eName, constrains);
            return new StateArray(node);
        }
        else if (descendant.HasDescendant && descendant.Descendant is StateArray arrayEDesc)
        {
            if (!arrayEDesc.IsSolved)
                return arrayEDesc; // todo we have to remove ancestor on constrains here
            var constrains = ConstrainsState.Empty;
            var eName = "e" + descNodeName.ToString().ToLower() + "'";

            constrains.AddDescendant(arrayEDesc.Element);
            var node = TicNode.CreateTypeVariableNode(eName, constrains);
            return new StateArray(node);
        }
        else
            return null;
    }

    /// <summary>
    /// Transform constrains to fun state
    /// </summary>
    public static StateFun TransformToFunOrNull(object descNodeName, ConstrainsState descendant, StateFun ancestor) {
        if (descendant.NoConstrains)
        {
            var argNodes = new TicNode[ancestor.ArgsCount];
            for (int i = 0; i < ancestor.ArgsCount; i++)
            {
                var argNode = TicNode.CreateTypeVariableNode("a'" + descNodeName + "'" + i, ConstrainsState.Empty);
                argNode.AddAncestor(ancestor.ArgNodes[i]);
                argNodes[i] = argNode;
            }

            var retNode = TicNode.CreateTypeVariableNode("r'" + descNodeName, ConstrainsState.Empty);
            retNode.AddAncestor(ancestor.RetNode);

            return StateFun.Of(argNodes, retNode);
        }

        if (descendant.Descendant is StateFun funDesc && funDesc.ArgsCount == ancestor.ArgsCount)
        {
            return funDesc;
            /*
            var nrArgNodes = new TicNode[funDesc.ArgNodes.Length];
            for (int i = 0; i < funDesc.ArgNodes.Length; i++)
                nrArgNodes[i] = funDesc.ArgNodes[i];

            var nrRetNode = funDesc.RetNode.GetNonReference();
            if (allArgsAreSolved && nrRetNode.IsSolved)
                return StateFun.Of(nrArgNodes, nrRetNode);*/
        }

        return null;
    }

    /// <summary>
    /// Transform constrains to struct state
    /// </summary>
    public static StateStruct TransformToStructOrNull(ConstrainsState descendant, StateStruct ancStruct) {
        if (descendant.NoConstrains)
            return ancStruct;

        if (descendant.HasDescendant && descendant.Descendant is StateStruct structDesc)
        {
            //descendant is struct.
            if (structDesc.IsSolved)
                return structDesc; //if it is solved - return it

            // For perfomance
            bool allFieldsAreSolved = true;

            var newFields = new Dictionary<string, TicNode>(structDesc.MembersCount);
            foreach (var (key, value) in structDesc.Fields)
            {
                var nrField = value.GetNonReference();
                allFieldsAreSolved = allFieldsAreSolved && nrField.IsSolved;
                newFields.Add(key, nrField);
            }

            return new StateStruct(newFields, isFrozen: false);
        }

        return null;
    }

    private static void ThrowIfRecursiveTypeDefinition(TicNode node) {
        ThrowIfStateHasRecursiveTypeDefinitionReq(node.State, 1);

        // ReSharper disable once UnusedParameter.Local
        static void ThrowIfStateHasRecursiveTypeDefinitionReq(ITicNodeState state, int bypassNumber) {
            switch (state)
            {
                case StateRefTo refTo:
                    ThrowIfNodeHasRecursiveTypeDefinitionReq(refTo.Node, 1);
                    break;
                case ICompositeState composite:
                    foreach (var member in composite.Members)
                        ThrowIfNodeHasRecursiveTypeDefinitionReq(member, 1);
                    break;
                case ConstrainsState constrains:
                    if (constrains.HasDescendant)
                        ThrowIfStateHasRecursiveTypeDefinitionReq(constrains.Descendant, 1);
                    if (constrains.HasAncestor)
                        ThrowIfStateHasRecursiveTypeDefinitionReq(constrains.Ancestor, 1);
                    break;
            }
        }

        static void ThrowIfNodeHasRecursiveTypeDefinitionReq(TicNode node, int bypassNumber) {
            if (node.VisitMark == bypassNumber)
            {
                var route = new HashSet<TicNode>();
                FindRecursionTypeRoute(node, route);
                throw TicErrors.RecursiveTypeDefinition(route.ToArray());
            }

            var markBefore = node.VisitMark;
            node.VisitMark = bypassNumber;
            ThrowIfStateHasRecursiveTypeDefinitionReq(node.State, bypassNumber);
            node.VisitMark = markBefore;
        }

        static bool FindRecursionTypeRoute(TicNode node, ISet<TicNode> nodes) {
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
        IReadOnlyList<TicNode> inputNodes,
        IReadOnlyList<TicNode> syntaxNodes,
        Dictionary<string, TicNode> namedNodes,
        bool ignorePreferred) {
        var typeVariables = new List<TicNode>();

        int genericNodesCount = 0;
        const int typeVariableVisitedMark = 123;
        for (int i = toposortedNodes.Length - 1; i >= 0; i--)
        {
            var node = toposortedNodes[i];
            FinalizeRecursive(node);

            foreach (var member in node.GetAllLeafTypes())
            {
                if (member.VisitMark == typeVariableVisitedMark)
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

        SolveUselessGenerics(toposortedNodes, outputNodes, inputNodes, ignorePreferred);
        return new TicResultsWithGenerics(typeVariables, namedNodes, syntaxNodes);
    }

    private static void FinalizeRecursive(TicNode node) {
        if (node.State is StateRefTo refTo)
        {
            ThrowIfRecursiveTypeDefinition(refTo.Node);

            var originalOne = refTo.Node.GetNonReference();

            if (originalOne.State is ITypeState)
                node.State = originalOne.State;
            else if (originalOne != refTo.Node)
                node.State = new StateRefTo(originalOne);
        }

        if (node.State is ICompositeState composite)
        {
            ThrowIfRecursiveTypeDefinition(node);

            if (composite.HasAnyReferenceMember)
                node.State = composite.GetNonReferenced();

            foreach (var member in ((ICompositeState)node.State).Members)
                FinalizeRecursive(member);
        }
    }

    private static void SolveUselessGenerics(
        IEnumerable<TicNode> toposortedNodes,
        IReadOnlyList<TicNode> outputNodes,
        IEnumerable<TicNode> inputNodes,
        bool ignorePreferred) {
        //We have to solve all generic types that are not output

        const int outputTypeMark = 77;
        // Firstly - get all outputs and mark them with output mark
        foreach (var outputNode in outputNodes)
            foreach (var outputType in outputNode.GetAllOutputTypes())
                outputType.VisitMark = outputTypeMark;

        //All not solved output types
        foreach (var inputNode in inputNodes)
        {
            if (inputNode.State is StateFun stateFun)
            {
                foreach (var leafNode in stateFun.GetAllNotSolvedContravariantLeafs())
                {
                    if (leafNode.VisitMark == outputTypeMark)
                        continue;
                    leafNode.VisitMark = outputTypeMark;

                    //if contravariant not in output type list then
                    //solve it and add to output types
                    leafNode.State = ((ConstrainsState)leafNode.State).SolveContravariant();
                }
            }
        }

        //Input covariant types that NOT referenced and are not members of any output types
        foreach (var node in toposortedNodes)
        {
            if (node.VisitMark == outputTypeMark)
                continue;
            if (node.State is not ConstrainsState c)
                continue;
            // we have to ignore prefered type as we need last common ancestor
            node.State = c.SolveCovariant(ignorePreferred);
        }
    }

    #endregion


    private static IEnumerable<TicNode> GetAllNotSolvedContravariantLeafs(this StateFun fun) =>
        fun.ArgNodes
            .SelectMany(n => n.GetAllLeafTypes())
            .Where(t => t.State is ConstrainsState);


    public static IEnumerable<TicNode> GetAllLeafTypes(this TicNode node) =>
        node.State switch {
            ICompositeState composite => composite.AllLeafTypes,
            StateRefTo => new[] { node.GetNonReference() },
            _ => new[] { node }
        };

    private static IEnumerable<TicNode> GetAllOutputTypes(this TicNode node) =>
        //Todo Method is not tested. What about composite reference+ fun + reference cases?
        node.State switch {
            StateFun fun => fun.RetNode.GetAllOutputTypes(),
            StateArray array => array.AllLeafTypes,
            StateStruct @struct => @struct.AllLeafTypes,
            StateRefTo => new[] { node.GetNonReference() }, //mb AllLeafType?
            _ => new[] { node }
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PrintTrace(IEnumerable<TicNode> nodes) {
#if DEBUG
        if (!TraceLog.IsEnabled)
            return;

        var alreadyPrinted = new HashSet<TicNode>();

        void ReqPrintNode(TicNode node) {
            if (node == null)
                return;
            if (alreadyPrinted.Contains(node))
                return;
            if (node.State is StateArray arr)
                ReqPrintNode(arr.ElementNode);
            node.PrintToConsole();
            alreadyPrinted.Add(node);
        }

        foreach (var node in nodes)
            ReqPrintNode(node);
#endif
    }
}
