using NFun.Tic.SolvingStates;

namespace NFun.Interpritation.Functions
{
    public struct GenericConstrains
    {
        public readonly StatePrimitive Ancestor;
        public readonly StatePrimitive Descendant;
        public bool IsComparable;
        public override string ToString()
        {
            if (Ancestor == null && Descendant == null && IsComparable)
                return "<>";
            return $"[{Descendant}..{Ancestor}]" + (IsComparable ? "<>" : "");
        }

        public static readonly GenericConstrains Comparable =new GenericConstrains(null,null,true);
        public static readonly GenericConstrains Any 
            = new GenericConstrains(null, null, false);
        public static readonly GenericConstrains Arithmetical
            = new GenericConstrains(StatePrimitive.Real, StatePrimitive.U24, false);
        public static readonly GenericConstrains Integers
            = new GenericConstrains(StatePrimitive.I96, null, false);
        public static readonly GenericConstrains Integers3264
            = new GenericConstrains(StatePrimitive.I96, StatePrimitive.U24, false);
        public static readonly GenericConstrains Integers32
            = new GenericConstrains(StatePrimitive.I48, null, false);
        public static readonly GenericConstrains SignedNumber
            = new GenericConstrains(StatePrimitive.Real, StatePrimitive.I16, false);
        public static readonly GenericConstrains Numbers
            = new GenericConstrains(StatePrimitive.Real, null, false);

        public static GenericConstrains FromTicConstrains(ConstrainsState constrainsState)
            =>new GenericConstrains(constrainsState.Ancestor , constrainsState.Descedant as StatePrimitive, constrainsState.IsComparable);

        public GenericConstrains(StatePrimitive ancestor = null, StatePrimitive descendant = null, bool isComparable = false)
        {
            Ancestor = ancestor;
            Descendant = descendant;
            IsComparable = isComparable;
        }
    }
}