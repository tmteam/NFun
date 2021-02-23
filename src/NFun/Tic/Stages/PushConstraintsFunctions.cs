using NFun.Tic.SolvingStates;

namespace NFun.Tic.Stages
{
    public class PushConstraintsFunctions : IStateCombination2dimensionalVisitor
    {
        public static IStateCombination2dimensionalVisitor Singletone { get; } = new PushConstraintsFunctions();
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
        public bool Apply(
            ICompositeState ancestor, 
            ConstrainsState descendant,
            TicNode ancestorNode,
            TicNode descendantNode)
        {
            // if ancestor is composite type then descendant HAS to have same composite type
            // y:int[] = a:[..]  # 'a' has to be an array
            if (ancestor is StateArray ancArray)
            {
                var result = SolvingFunctions.TransformToArrayOrNull(descendantNode.Name, descendant);
                if (result == null)
                    return false;
                if (result.ElementNode == ancArray.ElementNode)
                {
                    descendantNode.RemoveAncestor(ancestorNode);
                    return true;
                }
                
                result.ElementNode.AddAncestor(ancArray.ElementNode);
                descendantNode.State = result;
                descendantNode.RemoveAncestor(ancestorNode);
                SolvingFunctions.PushConstraints(result.ElementNode, ancArray.ElementNode);
                return true;
            }
            // y:f(x) = a:[..]  # 'a' has to be a functional variable
            if (ancestor is StateFun ancFun)
            {
                var descFun = SolvingFunctions.TransformToFunOrNull(descendantNode.Name, descendant, ancFun);
                if (descFun == null)
                    return false;
                if (!descendantNode.State.Equals(descFun))
                {
                    descendantNode.State = descFun;
                    PushFunTypeArgumentsConstraints(descFun, ancFun);
                }

                return true;
            }
            // y:user = a:[..]  # 'a' has to be a struct, that converts to type of 'user'
            if (ancestor is StateStruct ancStruct)
            {
                var descStruct = SolvingFunctions.TransformToStructOrNull(descendant, ancStruct);
                if (descStruct == null)
                    return false;
                if (!descendantNode.State.Equals(descStruct))
                {
                    descendantNode.RemoveAncestor(ancestorNode);
                    return true;
                }
                descendantNode.State = descStruct;
                if (TryMergeStructFields(ancStruct, descStruct))
                {
                    descendantNode.RemoveAncestor(ancestorNode);
                    return true;
                }
            }
            return false;
        }

        private static bool TryMergeStructFields(StateStruct ancStruct, StateStruct descStruct)
        {
            foreach (var ancField in ancStruct.Fields)
            {
                TicNode descFieldNode = descStruct.GetFieldOrNull(ancField.Key);
                if (descFieldNode == null)
                    return false;
                //  i m not sure why - but it is very important to set descFieldNode as main merge node... 
                SolvingFunctions.Merge(descFieldNode, ancField.Value);
            }

            return true;
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
                PushFunTypeArgumentsConstraints(descFun, ancFun);
                return true;
            }
            if (ancestor is StateStruct ancStruct)
            {
                return TryMergeStructFields(ancStruct, (StateStruct) descendant);
            }
            return false;
        }

        private static void PushFunTypeArgumentsConstraints(StateFun descFun, StateFun ancFun)
        {
            for (int i = 0; i < descFun.ArgsCount; i++)
                SolvingFunctions.PushConstraints(descFun.ArgNodes[i], ancFun.ArgNodes[i]);

            SolvingFunctions.PushConstraints(descFun.RetNode, ancFun.RetNode);
        }
    }
}