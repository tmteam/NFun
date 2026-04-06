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
        if (stateB is ConstraintsState c && c.NoConstrains)
            return stateA;

        if (stateA is ITypeState typeA && !typeA.IsMutable)
        {
            if (stateB is ITypeState typeB && !typeB.IsMutable)
                return !typeB.Equals(typeA) ? null : typeA;

            if (stateB is ConstraintsState constrainsB)
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
            case StateOptional optA when stateB is StateOptional optB:
                MergeInplace(optA.ElementNode, optB.ElementNode);
                return optA;
            case StateStruct strA when stateB is StateStruct strB:
                return MergeStructs(strA, strB);
            case StateStruct strA2 when stateB is ConstraintsState constrainsB2:
            {
                if (constrainsB2.HasDescendant && constrainsB2.Descendant is StateStruct descStruct)
                    return MergeStructs(strA2, descStruct);
                if (!constrainsB2.HasDescendant && !constrainsB2.IsComparable)
                    return strA2; // unconstrained → becomes the struct
                return null;
            }
            case ConstraintsState constrainsA when stateB is ConstraintsState constrainsB:
                return constrainsB.MergeOrNull(constrainsA);
            case ConstraintsState:
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

    private static StateStruct MergeStructs(StateStruct strA, StateStruct strB) {
        var result = new Dictionary<string, TicNode>();
        foreach (var (key, value) in strA.Fields)
        {
            var bNode = strB.GetFieldOrNull(key);
            if (bNode != null)
            {
                // When merging None field with non-None field → wrap in Optional
                var aRef = value.GetNonReference();
                var bRef = bNode.GetNonReference();
                if (aRef.State == StatePrimitive.None && bRef.State is not StatePrimitive { Name: PrimitiveTypeName.None })
                {
                    // b has content, a is None → result = opt(b's content)
                    var innerNode = TicNode.CreateTypeVariableNode("e" + key + "'", bRef.State);
                    value.State = new StateOptional(innerNode);
                }
                else if (bRef.State == StatePrimitive.None && aRef.State is not StatePrimitive { Name: PrimitiveTypeName.None })
                {
                    // a has content, b is None → result = opt(a's content)
                    var innerNode = TicNode.CreateTypeVariableNode("e" + key + "'", aRef.State);
                    value.State = new StateOptional(innerNode);
                }
                else
                    MergeInplace(value, bNode);
            }
            else if (strA.IsFrozen || strB.IsFrozen)
                return null;
            result.Add(key, value);
        }

        foreach (var (key, value) in strB.Fields)
            result.TryAdd(key, value);

        return new StateStruct(result, isFrozen: false);
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

        foreach (var a in secondary.Ancestors)
            if (a != main) main.AddAncestor(a);
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
        // Materialize to avoid repeated enumeration of lazy IEnumerable
        var cycleSet = new HashSet<TicNode>(cycleRoute);

        TicNode main = null;
        foreach (var c in cycleSet)
            if (c.State is not StateRefTo) { main = c; break; }
        main ??= cycleSet.First();

        foreach (var current in cycleSet)
        {
            if (current == main)
                continue;

            if (current.State is StateRefTo refState)
            {
                if (!cycleSet.Contains(refState.Node))
                    throw new InvalidOperationException();
            }
            else
            {
                //merge main and current
                main.State = GetMergedStateOrNull(main.State, current.State) ??
                             throw TicErrors.CannotMerge(main, current);
            }

            foreach (var a in current.Ancestors)
                if (a != main) main.AddAncestor(a);
            current.ClearAncestors();

            if (!current.IsSolved)
                current.State = new StateRefTo(main);
        }

        // Filter ancestors: remove cycle members, deduplicate
        var newAncestors = new HashSet<TicNode>();
        foreach (var a in main.Ancestors)
            if (!cycleSet.Contains(a))
                newAncestors.Add(a);

        main.ClearAncestors();
        foreach (var a in newAncestors)
            main.AddAncestor(a);
        return main;
    }

    #endregion


    public static void PullConstraints(TicNode[] toposortedNodes) {
        // Phase 1: Pull None nodes first — sets IsOptional flags on ancestor ConstraintsStates.
        // Must run before non-None nodes so that Concretest snapshots in Phase 2
        // see the IsOptional flag and produce opt() wrappers.
        bool hasNoneNodes = false;
        foreach (var node in toposortedNodes)
        {
            if (node.IsMemberOfAnything)
                continue;
            if (node.State == StatePrimitive.None) {
                hasNoneNodes = true;
                PullConstraintsRecursive(node);
            }
        }

        // Phase 2: Pull all non-None nodes. IsOptional flags are already set,
        // so AddDescendant's Concretest produces correct opt() snapshots.
        foreach (var node in toposortedNodes)
        {
            if (node.IsMemberOfAnything)
                continue;
            if (node.State == StatePrimitive.None)
                continue;
            PullConstraintsRecursive(node);
        }
    }

    private static void PullConstraintsRecursive(TicNode node) {
        var ancSize = node.Ancestors.Count;
        if (ancSize == 1)
        {
            PullConstrains(node, node.Ancestors[0]);
        }
        else if (ancSize > 0)
        {
            foreach (var ancestor in node.Ancestors.ToSnapshot())
                PullConstrains(node, ancestor);
        }

        if (node.State is ICompositeState composite)
            for (int mi = 0; mi < composite.MemberCount; mi++)
                PullConstraintsRecursive(composite.GetMember(mi));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PullConstrains(TicNode descendant, TicNode ancestor) {
        if (descendant == ancestor) return;
        var res = PullConstraintsFunctions.Singleton.Invoke(ancestor, descendant);
        if (!res)
            throw TicErrors.IncompatibleTypes(ancestor, descendant);
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
        // None is a literal value — constraints can't be pushed into it.
        if (node.State == StatePrimitive.None)
            return;

        if (node.State is ICompositeState composite)
        {
            var oldMakr = node.VisitMark;
            if (oldMakr == 1567)
                throw TicErrors.RecursiveTypeDefinition(new[]{ node});
            node.VisitMark = 1567;
            for (int mi = 0; mi < composite.MemberCount; mi++)
                PushConstraintsRecursive(composite.GetMember(mi));
            node.VisitMark = oldMakr;
        }

        var ancSize = node.Ancestors.Count;
        if (ancSize == 1)
        {
            PushConstraints(node, node.Ancestors[0]);
        }
        else if (ancSize > 0)
        {
            foreach (var ancestor in node.Ancestors.ToSnapshot())
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

        // Materialize remaining IsOptional ConstraintsState → StateOptional
        // after Destruction, before Finalization.
        MaterializeOptionalFlags(toposorteNodes);

        return notSolvedCount == 0;
    }

    private static void MaterializeOptionalFlags(TicNode[] nodes) {
        var visited = new HashSet<TicNode>(ReferenceEqualityComparer.Instance);
        foreach (var node in nodes)
            MaterializeOptionalNode(node, visited, nodes);
    }

    private static void MaterializeOptionalNode(TicNode node, HashSet<TicNode> visited, TicNode[] allNodes) {
        var n = node.GetNonReference();
        if (!visited.Add(n)) return;

        if (n.State is ConstraintsState cs && cs.IsOptional) {
            // Pure IsOptional with no type constraints: this is standalone None
            // or all-None if-else. Resolve to None directly.
            if (!cs.HasDescendant && !cs.HasAncestor && !cs.IsComparable)
            {
                TraceLog.WriteLine($"  Materialize: {n.Name}: {cs} → None (pure optional, no constraints)");
                n.State = StatePrimitive.None;
            }
            else
            {
                TraceLog.WriteLine($"  Materialize: {n.Name}: {cs} → opt(inner)");
                var innerCs = ConstraintsState.Of(cs.Descendant, cs.Ancestor, cs.IsComparable);
                innerCs.Preferred = cs.Preferred;
                var innerNode = TicNode.CreateTypeVariableNode(
                    "e" + n.Name.ToString().ToLower() + "'", innerCs);
                innerNode.IsOptionalElement = true;
                n.State = new StateOptional(innerNode);

                // Redirect syntax nodes (concrete values like constants) that were merged
                // with the Optional node during Destruction. These should keep their non-Optional
                // type (e.g., constant 1 stays I32, not opt(I32)).
                foreach (var other in allNodes) {
                    if (other.Type == TicNodeType.SyntaxNode
                        && other.State is StateRefTo refTo
                        && ReferenceEquals(refTo.Node, n)) {
                        other.State = new StateRefTo(innerNode);
                    }
                }
            }
        }
        if (n.State is ICompositeState composite) {
            for (int mi = 0; mi < composite.MemberCount; mi++)
                MaterializeOptionalNode(composite.GetMember(mi), visited, allNodes);
        }
    }

    private static void DestructionRecursive(TicNode node) {
        ThrowIfRecursiveTypeDefinition(node);

        if (node.State is ICompositeState composite)
        {
            if (composite.HasAnyReferenceMember) node.State = composite.GetNonReferenced();
            for (int mi = 0; mi < composite.MemberCount; mi++) DestructionRecursive(composite.GetMember(mi));
            // Flatten nested optionals after member destruction — only needed for StateOptional.
            if (node.State is StateOptional)
                node.FlattenNestedOptional();
        }

        var ancSize = node.Ancestors.Count;
        if (ancSize == 1)
        {
            Destruction(node, node.Ancestors[0]);
        }
        else if (ancSize > 0)
        {
            foreach (var ancestor in node.Ancestors.ToSnapshot())
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
    public static StateArray TransformToArrayOrNull(object descNodeName, ConstraintsState descendant) {
        if (descendant.NoConstrains)
        {
            var constrains = ConstraintsState.Empty;
            var eName = "e" + descNodeName.ToString().ToLower() + "'";

            var node = TicNode.CreateTypeVariableNode(eName, constrains);
            return new StateArray(node);
        }
        else if (descendant.HasDescendant && descendant.Descendant is StateArray arrayEDesc)
        {
            if (!arrayEDesc.IsSolved)
                return arrayEDesc;
            var constrains = ConstraintsState.Empty;
            var eName = "e" + descNodeName.ToString().ToLower() + "'";

            constrains.AddDescendant(arrayEDesc.Element);
            var node = TicNode.CreateTypeVariableNode(eName, constrains);
            return new StateArray(node);
        }
        else
            return null;
    }

    /// <summary>
    /// Transform constrains state to optional state
    /// </summary>
    public static StateOptional TransformToOptionalOrNull(object descNodeName, ConstraintsState descendant) {
        if (descendant.NoConstrains)
        {
            var constrains = ConstraintsState.Empty;
            var eName = "e" + descNodeName.ToString().ToLower() + "'";
            var node = TicNode.CreateTypeVariableNode(eName, constrains);
            return new StateOptional(node);
        }

        // None ≤ opt(T): absorb None into opt layer, element stays unconstrained
        if (descendant.HasDescendant && descendant.Descendant == StatePrimitive.None)
        {
            var constrains = ConstraintsState.Empty;
            var eName = "e" + descNodeName.ToString().ToLower() + "'";
            var node = TicNode.CreateTypeVariableNode(eName, constrains);
            return new StateOptional(node);
        }

        if (descendant.HasDescendant && descendant.Descendant is StateOptional optDesc)
        {
            if (!optDesc.IsSolved)
                return optDesc;
            var constrains = ConstraintsState.Empty;
            var eName = "e" + descNodeName.ToString().ToLower() + "'";
            constrains.AddDescendant(optDesc.Element);
            var node = TicNode.CreateTypeVariableNode(eName, constrains);
            return new StateOptional(node);
        }

        // IsOptional flag set with actual constraints — materialize now.
        // This ensures opt-to-opt paths are used instead of implicit lift T ≤ opt(T),
        // which would lose the inner type constraints (e.g., widening I32 to Real).
        // Only materialize when there are constraints to preserve (desc or anc);
        // pure IsOptional-only is handled by MaterializeOptionalFlags later.
        if (descendant.IsOptional && (descendant.HasDescendant || descendant.HasAncestor))
        {
            var innerCs = ConstraintsState.Of(descendant.Descendant, descendant.Ancestor, descendant.IsComparable);
            // Preserve Preferred from the original constraint. If none exists but
            // we have a concrete primitive descendant, use it as Preferred — this
            // matches non-optional SetDef behavior where Preferred comes from the literal.
            innerCs.Preferred = descendant.Preferred
                ?? (descendant.HasDescendant && descendant.Descendant is StatePrimitive dp
                    ? dp : null);
            var eName = "e" + descNodeName.ToString().ToLower() + "'";
            var node = TicNode.CreateTypeVariableNode(eName, innerCs);
            return new StateOptional(node);
        }

        return null;
    }

    /// <summary>
    /// Transform constrains to fun state
    /// </summary>
    public static StateFun TransformToFunOrNull(object descNodeName, ConstraintsState descendant, StateFun ancestor) {
        if (descendant.NoConstrains)
        {
            var argNodes = new TicNode[ancestor.ArgsCount];
            for (int i = 0; i < ancestor.ArgsCount; i++)
            {
                var argNode = TicNode.CreateTypeVariableNode("a'" + descNodeName + "'" + i, ConstraintsState.Empty);
                ancestor.ArgNodes[i].AddAncestor(argNode); // contravariant: desc arg ≥ anc arg
                argNodes[i] = argNode;
            }

            var retNode = TicNode.CreateTypeVariableNode("r'" + descNodeName, ConstraintsState.Empty);
            retNode.AddAncestor(ancestor.RetNode);

            return StateFun.Of(argNodes, retNode);
        }

        if (descendant.Descendant is StateFun funDesc && funDesc.ArgsCount == ancestor.ArgsCount)
        {
            var argNodes = new TicNode[ancestor.ArgsCount];
            for (int i = 0; i < ancestor.ArgsCount; i++)
            {
                var state = funDesc.ArgNodes[i].State;

                var argNode = TicNode.CreateTypeVariableNode("a'" + descNodeName + "'" + i,
                    ConstraintsState.Of(
                        anc: state as StatePrimitive,
                        isComparable: state is ConstraintsState {IsComparable:true}));
                ancestor.ArgNodes[i].AddAncestor(argNode);
                //argNode.AddAncestor(ancestor.ArgNodes[i]);
                argNodes[i] = argNode;
            }

            var retNode = TicNode.CreateTypeVariableNode("r'" + descNodeName,
                    ConstraintsState.Of(desc : funDesc.ReturnType));
            retNode.AddAncestor(ancestor.RetNode);

            return StateFun.Of(argNodes, retNode);
        }

        return null;
    }

    /// <summary>
    /// Transform constrains to struct state
    /// </summary>
    public static StateStruct TransformToStructOrNull(ConstraintsState descendant, StateStruct ancStruct) {
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

        static void ThrowIfStateHasRecursiveTypeDefinitionReq(ITicNodeState state, int bypassNumber) {
            switch (state)
            {
                case StateRefTo refTo:
                    ThrowIfNodeHasRecursiveTypeDefinitionReq(refTo.Node, bypassNumber);
                    break;
                case ICompositeState composite:
                    for (int mi = 0; mi < composite.MemberCount; mi++)
                        ThrowIfNodeHasRecursiveTypeDefinitionReq(composite.GetMember(mi), bypassNumber);
                    break;
                case ConstraintsState constrains:
                    if (constrains.HasDescendant)
                        ThrowIfStateHasRecursiveTypeDefinitionReq(constrains.Descendant, bypassNumber);
                    if (constrains.HasAncestor)
                        ThrowIfStateHasRecursiveTypeDefinitionReq(constrains.Ancestor, bypassNumber);
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
                for (int mi = 0; mi < composite.MemberCount; mi++)
                {
                    if (FindRecursionTypeRoute(composite.GetMember(mi), nodes))
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
        TicNode[] syntaxNodes,
        Dictionary<string, TicNode> namedNodes,
        bool ignorePreferred) {
        var typeVariables = new List<TicNode>();

        int genericNodesCount = 0;
        const int typeVariableVisitedMark = 123;
        for (int i = toposortedNodes.Length - 1; i >= 0; i--)
        {
            var node = toposortedNodes[i];
            FinalizeRecursive(node);

            CollectLeafTypeVariables(node, typeVariables, typeVariableVisitedMark);

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
            {
                node.State = composite.GetNonReferenced();
                composite = (ICompositeState)node.State;
            }

            for (int mi = 0; mi < composite.MemberCount; mi++)
                FinalizeRecursive(composite.GetMember(mi));

            // Safety net: flatten any remaining nested optionals after member finalization
            node.FlattenNestedOptional();
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
                var leafs = new List<TicNode>();
                stateFun.CollectNotSolvedContravariantLeafs(leafs);
                foreach (var leafNode in leafs)
                {
                    if (leafNode.VisitMark == outputTypeMark)
                        continue;
                    leafNode.VisitMark = outputTypeMark;

                    //if contravariant not in output type list then
                    //solve it and add to output types
                    leafNode.State = ((ConstraintsState)leafNode.State).SolveContravariant();
                }
            }
        }

        //Input covariant types that NOT referenced and are not members of any output types
        foreach (var node in toposortedNodes)
        {
            if (node.VisitMark == outputTypeMark)
                continue;
            if (node.State is not ConstraintsState c)
                continue;
            // we have to ignore prefered type as we need last common ancestor
            node.State = c.SolveCovariant(ignorePreferred);
        }
    }

    #endregion


    private static void CollectNotSolvedContravariantLeafs(this StateFun fun, List<TicNode> result) {
        foreach (var argNode in fun.ArgNodes)
            CollectLeafConstraints(argNode, result);
    }

    private static void CollectLeafConstraints(TicNode node, List<TicNode> result) {
        var state = node.State;
        if (state is ICompositeState composite) {
            for (int mi = 0; mi < composite.MemberCount; mi++)
                CollectLeafConstraints(composite.GetMember(mi), result);
            return;
        }
        if (state is StateRefTo)
            node = node.GetNonReference();
        if (node.State is ConstraintsState)
            result.Add(node);
    }


    // Perf note: new[] { node } allocates a 1-element array, but yield return
    // creates a heavier state machine. Benchmarked: yield is ~2-4% slower here.
    public static IEnumerable<TicNode> GetAllLeafTypes(this TicNode node) =>
        node.State switch {
            ICompositeState composite => composite.AllLeafTypes,
            StateRefTo => new[] { node.GetNonReference() },
            _ => new[] { node }
        };

    private static void CollectLeafTypeVariables(TicNode node, List<TicNode> typeVariables, int visitMark) {
        var state = node.State;
        if (state is ICompositeState composite) {
            for (int mi = 0; mi < composite.MemberCount; mi++)
                CollectLeafTypeVariables(composite.GetMember(mi), typeVariables, visitMark);
            return;
        }
        if (state is StateRefTo)
            node = node.GetNonReference();
        if (node.VisitMark == visitMark) return;
        node.VisitMark = visitMark;
        if (node.Type == TicNodeType.TypeVariable && node.State is ConstraintsState)
            typeVariables.Add(node);
    }

    private static IEnumerable<TicNode> GetAllOutputTypes(this TicNode node) =>
        node.State switch {
            StateFun fun => fun.RetNode.GetAllOutputTypes(),
            StateArray array => array.AllLeafTypes,
            StateStruct @struct => @struct.AllLeafTypes,
            StateOptional opt => opt.AllLeafTypes,
            StateRefTo => node.GetNonReference().GetAllOutputTypes(),
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
