using System.Runtime.CompilerServices;
using NFun.Tic.SolvingStates;

namespace NFun.Tic
{
    public class PullConstraintsFunctions: IStateCombinationFunctions
    {
        public static IStateCombinationFunctions SingleTone { get; } = new PullConstraintsFunctions();
        
        public bool Apply(StatePrimitive ancestor, StatePrimitive descendant, TicNode _, TicNode __) =>
            descendant.CanBeImplicitlyConvertedTo(ancestor);
        public bool Apply(StatePrimitive ancestor, ConstrainsState descendant, TicNode _, TicNode __) 
            => !descendant.HasDescendant || descendant.Descedant.CanBeImplicitlyConvertedTo(ancestor);
        public bool Apply(StatePrimitive ancestor, ICompositeState descendant, TicNode _, TicNode __) 
            => descendant.CanBeImplicitlyConvertedTo(ancestor);
        public bool Apply(ConstrainsState ancestor, StatePrimitive descendant, TicNode ancestorNode, TicNode descendantNode)
            => ApplyAncestorConstrains(ancestorNode, ancestor, descendant);
        public bool Apply(ConstrainsState ancestor, ConstrainsState descendant, TicNode ancestorNode, TicNode descendantNode)
        {
            var ancestorCopy = ancestor.GetCopy();
            ancestorCopy.AddDescedant(descendant.Descedant);
            var result = ancestorCopy.GetOptimizedOrNull();
            if (result == null)
                return false;
            ancestorNode.State = result;
            return true;
        }
        public bool Apply(ConstrainsState ancestor, ICompositeState descendant, TicNode ancestorNode, TicNode descendantNode)
            => ApplyAncestorConstrains(ancestorNode, ancestor, descendant);
        public bool Apply(ICompositeState ancestor, StatePrimitive descendant, TicNode _, TicNode __) => false;
        public bool Apply(ICompositeState ancestor, ConstrainsState descendant, TicNode ancestorNode, TicNode descendantNode)
        {
            if (ancestor is StateArray ancArray)
            {
                var result = SolvingFunctions.TransformToArrayOrNull(descendantNode.Name, descendant);
                if (result == null)
                    return false;
                result.ElementNode.Ancestors.Add(ancArray.ElementNode);
                descendantNode.State = result;
                descendantNode.Ancestors.Remove(ancestorNode);
            }
            else if (ancestor is StateFun ancFun)
            {
                var result = SolvingFunctions.TransformToFunOrNull(
                    descendantNode.Name, descendant, ancFun);
                if (result == null)
                    return false;
                result.RetNode.Ancestors.Add(ancFun.RetNode);
                for (int i = 0; i < result.ArgsCount; i++)
                    result.ArgNodes[i].Ancestors.Add(ancFun.ArgNodes[i]);
                descendantNode.State = result;
                descendantNode.Ancestors.Remove(ancestorNode);
            }
            return true;
        }

        public bool Apply(ICompositeState ancestor, ICompositeState descendant, TicNode ancestorNode, TicNode descendantNode)
        {
            if (ancestor.GetType() != descendant.GetType())
                return false;
            if (ancestor is StateArray ancArray)
            {
                var descArray = (StateArray) descendant;
                descendantNode.Ancestors.Remove(ancestorNode);
                descArray.ElementNode.Ancestors.Add(ancArray.ElementNode);
            }
            else if (ancestor is StateFun ancFun)
            {
                var descFun = (StateFun) descendant;

                if (descFun.ArgsCount != ancFun.ArgsCount)
                    return false;
                descendantNode.Ancestors.Remove(ancestorNode);

                descFun.RetNode.Ancestors.Add(ancFun.RetNode);
                for (int i = 0; i < descFun.ArgsCount; i++)
                    ancFun.ArgNodes[i].Ancestors.Add(descFun.ArgNodes[i]);
            }

            return true;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ApplyAncestorConstrains(TicNode ancestorNode, ConstrainsState ancestor, ITypeState typeDesc)
        {
            var ancestorCopy = ancestor.GetCopy();
            ancestorCopy.AddDescedant(typeDesc);
            var result = ancestorCopy.GetOptimizedOrNull();
            if (result == null)
                return false;
            ancestorNode.State = result;
            return true;
        }
    }
}