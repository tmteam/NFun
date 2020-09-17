using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Tic.Errors;
using NFun.Tic.SolvingStates;
using Array = NFun.Tic.SolvingStates.Array;

namespace NFun.Tic
{
    public static class SolvingFunctions
    {
        #region Merges

        public static IState GetMergedStateOrNull(IState stateA, IState stateB)
        {
            if (stateB is Constrains c && c.NoConstrains)
                return stateA;

            if (stateA is IType typeA && typeA.IsSolved)
            {
                if (stateB is IType typeB && typeB.IsSolved)
                    return !typeB.Equals(typeA) ? null : typeA;

                if (stateB is Constrains constrainsB)
                    return !constrainsB.Fits(typeA) ? null : typeA;
            }

            switch (stateA)
            {
                case Array arrayA when stateB is Array arrayB:
                    Merge(arrayA.ElementNode, arrayB.ElementNode);
                    return arrayA;
                case Fun funA when stateB is Fun funB:
                {
                    if (funA.ArgsCount != funB.ArgsCount)
                        return null;

                    for (int i = 0; i < funA.ArgsCount; i++)
                        Merge(funA.ArgNodes[i], funB.ArgNodes[i]);
                    Merge(funA.RetNode, funB.RetNode);
                    return funA;
                }

                case Constrains constrainsA when stateB is Constrains constrainsB:
                    return constrainsB.MergeOrNull(constrainsA);
                case Constrains _: 
                    return GetMergedStateOrNull(stateB, stateA);
                case RefTo refA:
                {
                    var state = GetMergedStateOrNull(refA.Node.State, stateB);
                    if (state == null) return null;
                    refA.Node.State = state;
                    return stateA;
                }
            }
            if (stateB is RefTo)
                return GetMergedStateOrNull(stateB, stateA);

            return null;
        }


        public static void Merge(SolvingNode main, SolvingNode secondary)
        {
            if(main==secondary)
                return;
            
            var res = GetMergedStateOrNull(main.State, secondary.State);
            if (res == null)
                throw TicErrors.CannotMerge(main, secondary);

            main.State = res;
            if (res is IType t && t.IsSolved)
            {
                secondary.State = res;
                return;
            }

            main.Ancestors.AddRange(secondary.Ancestors);
            secondary.Ancestors.Clear();
            secondary.State = new RefTo(main);
        }

        public static void MergeGroup(SolvingNode[] cycleRoute)
        {
            var main = cycleRoute.First();

            foreach (var current in cycleRoute)
            {
                if (current == main)
                    continue;

                if (current.State is RefTo refState)
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
                    current.State = new RefTo(main);
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

        public static void SetUpwardsLimits(SolvingNode[] toposortedNodes)
        {
            void HandleUpwardLimits(SolvingNode node)
            {
                for (var index = 0; index < node.Ancestors.Count; index++)
                {
                    var ancestor = node.Ancestors[index];
                    ancestor.State = SetUpwardsLimits(node, ancestor);
                }

                if(node.State is ICompositeType composite)
                    foreach (var member in composite.Members)
                        HandleUpwardLimits(member);
            }
            //todo perfomance hotspot (memberOf.Any())
            foreach (var node in toposortedNodes.Where(n => !n.MemberOf.Any()))
                HandleUpwardLimits(node);
        }

        private static IState SetUpwardsLimits(SolvingNode descendant, SolvingNode ancestor)
        {
            #region handle refto cases. 

            if (ancestor == descendant)
                return ancestor.State;

            if (ancestor.State is RefTo referenceAnc)
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

            if (descendant.State is RefTo referenceDesc)
            {
                if (descendant.Ancestors.Contains(ancestor))
                {
                    descendant.Ancestors.Remove(ancestor);
                    if (referenceDesc.Node != ancestor)
                        referenceDesc.Node.Ancestors.Add(ancestor);
                }

                ancestor.State = SetUpwardsLimits(referenceDesc.Node, ancestor);
                return ancestor.State;
            }

            #endregion

            if (descendant.State is IType typeDesc)
            {
                switch (ancestor.State)
                {
                    case Primitive concreteAnc:
                    {
                        if (!typeDesc.CanBeImplicitlyConvertedTo(concreteAnc))
                            throw TicErrors.IncompatibleTypes(ancestor, descendant);
                        return ancestor.State;
                    }

                    case Constrains constrainsAnc:
                    {
                        var result = constrainsAnc.GetCopy();
                        result.AddDescedant(typeDesc);
                        return result.GetOptimizedOrNull()??
                               throw TicErrors.IncompatibleTypes(ancestor, descendant);
                    }

                    case Array arrayAnc:
                    {
                        if (!(typeDesc is Array arrayDesc))
                            throw TicErrors.IncompatibleTypes(ancestor, descendant);


                        descendant.Ancestors.Remove(ancestor);
                        arrayDesc.ElementNode.Ancestors.Add(arrayAnc.ElementNode);
                        return ancestor.State;
                    }

                    case Fun fun:
                    {
                        if (!(typeDesc is Fun funDesc))
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

            if (descendant.State is Constrains constrainsDesc)
            {
                switch (ancestor.State)
                {
                    case Primitive concreteAnc:
                    {
                        if (constrainsDesc.HasDescendant &&
                            constrainsDesc.Descedant?.CanBeImplicitlyConvertedTo(concreteAnc) != true)
                            throw TicErrors.IncompatibleNodes(ancestor, descendant);
                        return ancestor.State;
                    }

                    case Constrains constrainsAnc:
                    {
                        var result = constrainsAnc.GetCopy();
                        result.AddDescedant(constrainsDesc.Descedant);
                        return result.GetOptimizedOrNull()
                               ?? throw TicErrors.IncompatibleNodes(ancestor, descendant);
                    }
                    case Array arrayAnc:
                    {
                        var result = TransformToArrayOrNull(descendant.Name, constrainsDesc)
                                ?? throw TicErrors.IncompatibleNodes(ancestor, descendant);

                        result.ElementNode.Ancestors.Add(arrayAnc.ElementNode);
                        descendant.State = result;
                        descendant.Ancestors.Remove(ancestor);
                        return ancestor.State;
                    }

                    case Fun funAnc:
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

        public static void SetDownwardsLimits(SolvingNode[] toposortedNodes)
        {
            void Downwards(SolvingNode descendant)
            {
                if(descendant.State is ICompositeType composite)
                    foreach (var member in composite.Members)
                        Downwards(member);
                
                foreach (var ancestor in descendant.Ancestors.ToArray())
                    descendant.State = SetDownwardsLimits(descendant, ancestor);
            }

            for (int i = toposortedNodes.Length - 1; i >= 0; i--)
            {
                var descendant = toposortedNodes[i];
                if (descendant.MemberOf.Any())
                    continue;

                Downwards(descendant);
            }
        }

        private static IState SetDownwardsLimits(SolvingNode descendant, SolvingNode ancestor)
        {
            #region handle refTo case

            if (descendant == ancestor)
                return descendant.State;

            if (descendant.State is RefTo referenceDesc)
            {
                referenceDesc.Node.State = SetDownwardsLimits(descendant, referenceDesc.Node);
                return referenceDesc;
            }

            if (ancestor.State is RefTo referenceAnc)
            {
                return SetDownwardsLimits(referenceAnc.Node, descendant);
            }

            #endregion

            if (ancestor.State is Array ancArray)
            {
                if (descendant.State is Constrains constr)
                {
                    var result = TransformToArrayOrNull(descendant.Name, constr)
                            ?? throw TicErrors.CanntoBecomeFunction(ancestor, descendant);
                    result.ElementNode.Ancestors.Add(ancArray.ElementNode);
                    descendant.State = result;
                    descendant.Ancestors.Remove(ancestor);
                }

                if (descendant.State is Array desArray)
                {
                    desArray.ElementNode.State = SetDownwardsLimits(desArray.ElementNode, ancArray.ElementNode);
                    return descendant.State;
                }
                throw TicErrors.CanntoBecomeArray(ancestor, descendant);
            }

            if (ancestor.State is Fun ancFun)
            {
                if (descendant.State is Constrains constr)
                {
                    var result = TransformToFunOrNull(descendant.Name, constr, ancFun);
                    if (result == null)
                        throw TicErrors.CanntoBecomeFunction(ancestor, descendant);
                    descendant.State = result;
                }

                if (descendant.State is Fun desArray)
                {
                    if (desArray.ArgsCount != ancFun.ArgsCount)
                        throw TicErrors.IncompatibleFunSignatures(ancestor, descendant);

                    for (int i = 0; i < desArray.ArgsCount; i++)
                        desArray.ArgNodes[i].State = SetDownwardsLimits(desArray.ArgNodes[i], ancFun.ArgNodes[i]);

                    desArray.RetNode.State = SetDownwardsLimits(desArray.RetNode, ancFun.RetNode);
                    return descendant.State;
                }

                throw TicErrors.CanntoBecomeFunction(ancestor, descendant);
            }

            Primitive up = null;
            if (ancestor.State is Primitive concreteAnc) up = concreteAnc;
            else if (ancestor.State is Constrains constrainsAnc)
            {
                if (constrainsAnc.HasAncestor) up = constrainsAnc.Ancestor;
                else return descendant.State;
            }
            else if (ancestor.State is Array) return descendant.State;
            else if (ancestor.State is Fun) return descendant.State;

            if (up == null)
                throw TicErrors.IncompatibleNodes(ancestor, descendant);

            if (descendant.State is IType concreteDesc)
            {
                if (!concreteDesc.CanBeImplicitlyConvertedTo(up))
                    throw TicErrors.IncompatibleTypes(ancestor, descendant);
                return descendant.State;
            }

            if (descendant.State is Constrains constrainsDesc)
            {
                constrainsDesc.AddAncestor(up);
                return constrainsDesc.GetOptimizedOrNull()
                    ?? throw TicErrors.IncompatibleNodes(ancestor, descendant);

            }

            throw TicErrors.IncompatibleNodes(ancestor, descendant);
        }

        #endregion

        public static void BecomeReferenceFor(this SolvingNode referencedNode, SolvingNode original)
        {
            referencedNode = referencedNode.GetNonReference();
            original = original.GetNonReference();
            if (referencedNode.Type == SolvingNodeType.SyntaxNode)
                Merge(original, referencedNode);
            else
                Merge(referencedNode, original);
        }

        /// <summary>
        /// Transform constrains state to array state
        /// </summary>
        private static Array TransformToArrayOrNull(string descNodeName, Constrains descendant)
        {
            if (descendant.NoConstrains)
            {
                var constrains = new Constrains();
                string eName;

                if (descNodeName.StartsWith("T") && descNodeName.Length > 1)
                    eName = "e" + descNodeName.Substring(1).ToLower() + "'";
                else
                    eName = "e" + descNodeName.ToLower() + "'";

                var node = new SolvingNode(eName, constrains, SolvingNodeType.TypeVariable);
                return new Array(node);
            }
            else if (descendant.HasDescendant && descendant.Descedant is Array arrayEDesc)
            {
                if (arrayEDesc.Element is RefTo)
                {
                    var origin = arrayEDesc.ElementNode.GetNonReference();
                    if (origin.IsSolved)
                        return new Array(origin);
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
        private static Fun TransformToFunOrNull(string descNodeName, Constrains descendant, Fun ancestor)
        {
            if (descendant.NoConstrains)
            {
                var argNodes = new SolvingNode[ancestor.ArgsCount];
                for (int i = 0; i < ancestor.ArgsCount; i++)
                {
                    var argNode = new SolvingNode("a'"+ descNodeName +"'"+i, new Constrains(), SolvingNodeType.TypeVariable);
                    argNode.Ancestors.Add(ancestor.ArgNodes[i]);
                    argNodes[i] = argNode;
                }

                var retNode = new SolvingNode("r'"+ descNodeName, new Constrains(), SolvingNodeType.TypeVariable);
                retNode.Ancestors.Add(ancestor.RetNode);

                return Fun.Of(argNodes, retNode);
            }

            if (descendant.Descedant is Fun arrayEDesc
                && arrayEDesc.ArgsCount == ancestor.ArgsCount)
            {
                if (arrayEDesc.IsSolved)
                    return arrayEDesc;

                //For - perfomance
                bool allArgsAreSolved = true;
                var nrArgNodes = new SolvingNode[arrayEDesc.ArgNodes.Length];
                for (int i = 0; i < arrayEDesc.ArgNodes.Length; i++)
                {
                    nrArgNodes[i] = arrayEDesc.ArgNodes[i].GetNonReference();
                    allArgsAreSolved = allArgsAreSolved && nrArgNodes[i].IsSolved;
                }
                
                var nrRetNode = arrayEDesc.RetNode.GetNonReference();
                if (allArgsAreSolved && nrRetNode.IsSolved)
                    return Fun.Of(nrArgNodes, nrRetNode);
            }
            return null;
        }

       
    }
}