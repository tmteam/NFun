using System;
using System.Linq;
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

        #region Upward 

        public static void SetUpwardsLimits(TicNode[] toposortedNodes)
        {
            void HandleUpwardLimits(TicNode node)
            {
                
                // ReSharper disable once ForCanBeConvertedToForeach
                //We has to use for, because collection can be modified
                for (var index = 0; index < node.Ancestors.Count; index++)
                {
                    var ancestor = node.Ancestors[index];
                    ancestor.State = SetUpwardsLimits(node, ancestor);
                }

                if(node.State is ICompositeTypeState composite)
                    foreach (var member in composite.Members)
                        HandleUpwardLimits(member);
            }
            foreach (var node in toposortedNodes.Where(n => !n.IsMemberOfAnything))
                HandleUpwardLimits(node);
        }

        private static ITicNodeState SetUpwardsLimits(TicNode descendant, TicNode ancestor)
        {
            #region handle refto cases. 

            if (ancestor == descendant)
                return ancestor.State;
            
/*
            if (ancestor.State is StateRefTo referenceAnc)
            {
                if (descendant.Ancestors.Contains(ancestor))
                {
                    descendant.Ancestors.Remove(ancestor);
                    if (descendant != referenceAnc.Node)
                        descendant.Ancestors.Add(referenceAnc.Node);
                }

                referenceAnc.Node.State = SetUpwardsLimits(descendant, referenceAnc.Node);
                return referenceAnc;
            }

            if (descendant.State is StateRefTo referenceDesc)
            {
                if (descendant.Ancestors.Contains(ancestor))
                {
                    descendant.Ancestors.Remove(ancestor);
                    if (referenceDesc.Node != ancestor)
                        referenceDesc.Node.Ancestors.Add(ancestor);
                }

                ancestor.State = SetUpwardsLimits(referenceDesc.Node, ancestor);
                return ancestor.State;
            }*/

            #endregion

            if (descendant.State is ITypeState typeDesc)
            {
                switch (ancestor.State)
                {
                    case StatePrimitive concreteAnc:
                    {
                        if (!typeDesc.CanBeImplicitlyConvertedTo(concreteAnc))
                            throw TicErrors.IncompatibleTypes(ancestor, descendant);
                        return ancestor.State;
                    }

                    case ConstrainsState constrainsAnc:
                    {
                        var result = constrainsAnc.GetCopy();
                        result.AddDescedant(typeDesc);
                        return result.GetOptimizedOrNull()??
                               throw TicErrors.IncompatibleTypes(ancestor, descendant);
                    }

                    case StateArray arrayAnc:
                    {
                        if (!(typeDesc is StateArray arrayDesc))
                            throw TicErrors.IncompatibleTypes(ancestor, descendant);


                        descendant.Ancestors.Remove(ancestor);
                        arrayDesc.ElementNode.Ancestors.Add(arrayAnc.ElementNode);
                        return ancestor.State;
                    }

                    case StateFun fun:
                    {
                        if (!(typeDesc is StateFun funDesc))
                            throw TicErrors.IncompatibleTypes(ancestor, descendant);
                        if (funDesc.ArgsCount != fun.ArgsCount)
                            throw TicErrors.IncompatibleFunSignatures(ancestor, descendant);

                        descendant.Ancestors.Remove(ancestor);

                        funDesc.RetNode.Ancestors.Add(fun.RetNode);
                        for (int i = 0; i < funDesc.ArgsCount; i++)
                            fun.ArgNodes[i].Ancestors.Add(funDesc.ArgNodes[i]);

                        return ancestor.State;
                    }

                    default:
                        throw new NotSupportedException();
                }
            }

            if (descendant.State is ConstrainsState constrainsDesc)
            {
                switch (ancestor.State)
                {
                    case StatePrimitive concreteAnc:
                    {
                        if (constrainsDesc.HasDescendant &&
                            constrainsDesc.Descedant?.CanBeImplicitlyConvertedTo(concreteAnc) != true)
                            throw TicErrors.IncompatibleNodes(ancestor, descendant);
                        return ancestor.State;
                    }

                    case ConstrainsState constrainsAnc:
                    {
                        var result = constrainsAnc.GetCopy();
                        result.AddDescedant(constrainsDesc.Descedant);
                        return result.GetOptimizedOrNull()
                               ?? throw TicErrors.IncompatibleNodes(ancestor, descendant);
                    }
                    case StateArray arrayAnc:
                    {
                        var result = TransformToArrayOrNull(descendant.Name, constrainsDesc)
                                ?? throw TicErrors.IncompatibleNodes(ancestor, descendant);

                        result.ElementNode.Ancestors.Add(arrayAnc.ElementNode);
                        descendant.State = result;
                        descendant.Ancestors.Remove(ancestor);
                        return ancestor.State;
                    }

                    case StateFun funAnc:
                    {
                        var result = TransformToFunOrNull(descendant.Name, constrainsDesc, funAnc)
                                     ?? throw TicErrors.IncompatibleNodes(ancestor, descendant);

                        result.RetNode.Ancestors.Add(funAnc.RetNode);
                        for (int i = 0; i < result.ArgsCount; i++)
                            result.ArgNodes[i].Ancestors.Add(funAnc.ArgNodes[i]);
                        descendant.State = result;
                        descendant.Ancestors.Remove(ancestor);
                        return ancestor.State;
                    }

                    default:
                        throw new NotSupportedException(
                            $"Ancestor type {ancestor.State.GetType().Name} is not supported");
                }
            }

            throw new NotSupportedException($"Descendant type {descendant.State.GetType().Name} is not supported");
        }

        #endregion

        #region Downward

        public static void SetDownwardsLimits(TicNode[] toposortedNodes)
        {
            void Downwards(TicNode descendant)
            {
                if(descendant.State is ICompositeTypeState composite)
                    foreach (var member in composite.Members)
                        Downwards(member);
                
                foreach (var ancestor in descendant.Ancestors.ToArray())
                    descendant.State = SetDownwardsLimits(descendant, ancestor);
            }

            for (int i = toposortedNodes.Length - 1; i >= 0; i--)
            {
                var descendant = toposortedNodes[i];
                if (descendant.IsMemberOfAnything)
                    continue;

                Downwards(descendant);
            }
        }

        private static ITicNodeState SetDownwardsLimits(TicNode descendant, TicNode ancestor)
        {
            #region handle refTo case

            if (descendant == ancestor)
                return descendant.State;

            if (descendant.State is StateRefTo referenceDesc)
            {
                referenceDesc.Node.State = SetDownwardsLimits(descendant, referenceDesc.Node);
                return referenceDesc;
            }

            if (ancestor.State is StateRefTo referenceAnc)
            {
                return SetDownwardsLimits(referenceAnc.Node, descendant);
            }

            #endregion

            if (ancestor.State is StateArray ancArray)
            {
                if (descendant.State is ConstrainsState constr)
                {
                    var result = TransformToArrayOrNull(descendant.Name, constr)
                            ?? throw TicErrors.CannotBecomeFunction(ancestor, descendant);
                    result.ElementNode.Ancestors.Add(ancArray.ElementNode);
                    descendant.State = result;
                    descendant.Ancestors.Remove(ancestor);
                }

                if (descendant.State is StateArray desArray)
                {
                    desArray.ElementNode.State = SetDownwardsLimits(desArray.ElementNode, ancArray.ElementNode);
                    return descendant.State;
                }
                throw TicErrors.CannotBecomeArray(ancestor, descendant);
            }

            if (ancestor.State is StateFun ancFun)
            {
                if (descendant.State is ConstrainsState constr)
                {
                    var result = TransformToFunOrNull(descendant.Name, constr, ancFun);
                    if (result == null)
                        throw TicErrors.CannotBecomeFunction(ancestor, descendant);
                    descendant.State = result;
                }

                if (descendant.State is StateFun desArray)
                {
                    if (desArray.ArgsCount != ancFun.ArgsCount)
                        throw TicErrors.IncompatibleFunSignatures(ancestor, descendant);

                    for (int i = 0; i < desArray.ArgsCount; i++)
                        desArray.ArgNodes[i].State = SetDownwardsLimits(desArray.ArgNodes[i], ancFun.ArgNodes[i]);

                    desArray.RetNode.State = SetDownwardsLimits(desArray.RetNode, ancFun.RetNode);
                    return descendant.State;
                }

                throw TicErrors.CannotBecomeFunction(ancestor, descendant);
            }

            StatePrimitive up = null;
            if (ancestor.State is StatePrimitive concreteAnc) up = concreteAnc;
            else if (ancestor.State is ConstrainsState constrainsAnc)
            {
                if (constrainsAnc.HasAncestor) up = constrainsAnc.Ancestor;
                else return descendant.State;
            }
            else if (ancestor.State is StateArray) return descendant.State;
            else if (ancestor.State is StateFun) return descendant.State;

            if (up == null)
                throw TicErrors.IncompatibleNodes(ancestor, descendant);

            if (descendant.State is ITypeState concreteDesc)
            {
                if (!concreteDesc.CanBeImplicitlyConvertedTo(up))
                    throw TicErrors.IncompatibleTypes(ancestor, descendant);
                return descendant.State;
            }

            if (descendant.State is ConstrainsState constrainsDesc)
            {
                constrainsDesc.AddAncestor(up);
                return constrainsDesc.GetOptimizedOrNull()
                    ?? throw TicErrors.IncompatibleNodes(ancestor, descendant);

            }

            throw TicErrors.IncompatibleNodes(ancestor, descendant);
        }

        #endregion

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
        private static StateArray TransformToArrayOrNull(object descNodeName, ConstrainsState descendant)
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
        private static StateFun TransformToFunOrNull(object descNodeName, ConstrainsState descendant, StateFun ancestor)
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

       
    }
}