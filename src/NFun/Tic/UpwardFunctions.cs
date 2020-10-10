using NFun.Tic.Errors;
using NFun.Tic.SolvingStates;

namespace NFun.Tic
{
    public static class UpwardFunctions
    {
        public static ITicNodeState MergeUpwards(
            ITypeState typeDesc, StatePrimitive ancestor)
        {
            if (!typeDesc.CanBeImplicitlyConvertedTo(ancestor)) return null;
            return ancestor;
        }
        
        public static ITicNodeState MergeUpwards(
            ITypeState typeDesc, ConstrainsState ancestor)
        {
            var result = ancestor.GetCopy();
            result.AddDescedant(typeDesc);
            return result.GetOptimizedOrNull();
        }

        /*public static ITicNodeState MergeUpwards(
            TicNode descendant, TicNode ancestor, 
            StateArray arrayDesc, StateArray arrayAnc)
        {
            descendant.Ancestors.Remove(ancestor);
            arrayDesc.ElementNode.Ancestors.Add(arrayAnc.ElementNode);
            return arrayAnc;
        }*/
        
        /*public static ITicNodeState MergeUpwards(
            TicNode descendant, TicNode ancestor,
            StateFun funDesc, StateFun fun)
        {
            descendant.Ancestors.Remove(ancestor);
            if (funDesc.ArgsCount != fun.ArgsCount)
                throw TicErrors.IncompatibleFunSignatures(ancestor, descendant);

            descendant.Ancestors.Remove(ancestor);

            funDesc.RetNode.Ancestors.Add(fun.RetNode);
            for (int i = 0; i < funDesc.ArgsCount; i++)
                fun.ArgNodes[i].Ancestors.Add(funDesc.ArgNodes[i]);

            return ancestor.State;
        }*/

        
        /*public static ITicNodeState MergeUpwards(
            ConstrainsState descendant, StatePrimitive ancestor)
        {
            if (descendant.HasDescendant &&
                ! descendant.Descedant.CanBeImplicitlyConvertedTo(ancestor))
                return null;
            return ancestor;
        }*/
        /*public static ITicNodeState MergeUpwards(
            ConstrainsState descendant, ConstrainsState ancestor)
        {
            var result = ancestor.GetCopy();
            result.AddDescedant(descendant.Descedant);
            return result.GetOptimizedOrNull();
        }*/
        /*public static ITicNodeState MergeUpwards(
            TicNode descNode, 
            TicNode ancNode,
            ConstrainsState descendant, 
            StateArray ancestor)
        {
            var result = SolvingFunctions.TransformToArrayOrNull(descNode.Name, descendant);
            if (result == null)
                return null;
            result.ElementNode.Ancestors.Add(ancestor.ElementNode);
            descNode.State = result;
            descNode.Ancestors.Remove(ancNode);
            return ancestor;
        }*/

        /*public static ITicNodeState MergeUpwards(
            TicNode descNode,
            TicNode ancNode,
            ConstrainsState descendant,
            StateFun ancestor)
        {
            var result = SolvingFunctions.TransformToFunOrNull(descNode.Name, descendant, ancestor);
                         
            result.RetNode.Ancestors.Add(ancestor.RetNode);
            for (int i = 0; i < result.ArgsCount; i++)
                result.ArgNodes[i].Ancestors.Add(ancestor.ArgNodes[i]);
            descNode.State = result;
            descNode.Ancestors.Remove(ancNode);
            return ancestor;
        }*/

    }
}