using NFun.Tic.SolvingStates;

namespace NFun.Tic
{
    public class PushConstraintsFunctions : IStateCombinationFunctions
    {
        public static IStateCombinationFunctions Singletone { get; } = new PushConstraintsFunctions();
        public bool Apply(StatePrimitive ancestor, StatePrimitive descendant, TicNode _, TicNode __)
            => descendant.CanBeImplicitlyConvertedTo(ancestor);

        public bool Apply(StatePrimitive ancestor, ConstrainsState descendant, TicNode _, TicNode descendantNode)
        {
            descendant.AddAncestor(ancestor);
            var result = descendant.GetOptimizedOrNull();
            if (result == null)
                return false;
            descendantNode.State = result;
            return true;
        }

        public bool Apply(StatePrimitive ancestor, ICompositeState descendant, TicNode _, TicNode __)=> true;

        public bool Apply(ConstrainsState ancestor, StatePrimitive descendant, TicNode ancestorNode,
            TicNode descendantNode)
        {
            if (!ancestor.HasAncestor)
                return true;
            return descendant.CanBeImplicitlyConvertedTo(ancestor.Ancestor);
        }

        public bool Apply(ConstrainsState ancestor, ConstrainsState descendant, TicNode ancestorNode,
            TicNode descendantNode)
        {
            if (!ancestor.HasAncestor)
                return true;

            descendant.AddAncestor(ancestor.Ancestor);
            var result = descendant.GetOptimizedOrNull();
            if (result == null)
                return false;
            descendantNode.State = result;
            return true;
        }

        public bool Apply(ConstrainsState ancestor, ICompositeState descendant, TicNode _, TicNode __) =>
            !ancestor.HasAncestor || ancestor.Ancestor.Equals(StatePrimitive.Any);
        public bool Apply(ICompositeState ancestor, StatePrimitive descendant, TicNode _, TicNode __) => false;
        public bool Apply(ICompositeState ancestor, ConstrainsState descendant, TicNode ancestorNode,
            TicNode descendantNode)
        {
            if (ancestor is StateArray ancArray)
            {
                var result = SolvingFunctions.TransformToArrayOrNull(descendantNode.Name, descendant);
                if (result == null)
                    return false;
                result.ElementNode.Ancestors.Add(ancArray.ElementNode);
                descendantNode.State = result;
                descendantNode.Ancestors.Remove(ancestorNode);
                SolvingFunctions.PushConstraints(result.ElementNode, ancArray.ElementNode);
                return true;
            }

            if (ancestor is StateFun ancFun)
            {
                var descFun = SolvingFunctions.TransformToFunOrNull(descendantNode.Name, descendant, ancFun);
                if (descFun == null)
                    return false;
                descendantNode.State = descFun;

                ConstraintDownFunTypeArguments(descFun, ancFun);
                return true;
            }
            return false;
        }

        public bool Apply(ICompositeState ancestor, ICompositeState descendant, TicNode ancestorNode,
            TicNode descendantNode)
        {
            if (ancestor.GetType() != descendant.GetType())
                return false;
            if (ancestor is StateArray ancArray)
            {
                var descArray = (StateArray) descendant;
                SolvingFunctions.PushConstraints(descArray.ElementNode, ancArray.ElementNode);
                return true;
            }

            if (ancestor is StateFun ancFun)
            {
                var descFun = (StateFun) descendant;
                if (descFun.ArgsCount != ancFun.ArgsCount)
                    return false;
                ConstraintDownFunTypeArguments(descFun, ancFun);
                return true;
            }

            return false;
        }

        private static void ConstraintDownFunTypeArguments(StateFun descFun, StateFun ancFun)
        {
            for (int i = 0; i < descFun.ArgsCount; i++)
                SolvingFunctions.PushConstraints(descFun.ArgNodes[i], ancFun.ArgNodes[i]);

            SolvingFunctions.PushConstraints(descFun.RetNode, ancFun.RetNode);
        }
    }
}