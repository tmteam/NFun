using NFun.Tic.SolvingStates;

namespace NFun.Interpritation.Functions
{
    public struct GenericConstrains
    {
        public readonly Primitive Ancestor;
        public readonly Primitive Descendant;
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
            = new GenericConstrains(Primitive.Real, Primitive.U24, false);
        public static readonly GenericConstrains Integers
            = new GenericConstrains(Primitive.I96, null, false);
        public static readonly GenericConstrains Integers3264
            = new GenericConstrains(Primitive.I96, Primitive.U24, false);
        public static readonly GenericConstrains Integers32
            = new GenericConstrains(Primitive.I48, null, false);
        public static readonly GenericConstrains SignedNumber
            = new GenericConstrains(Primitive.Real, Primitive.I16, false);
        public static readonly GenericConstrains Numbers
            = new GenericConstrains(Primitive.Real, null, false);

        public static GenericConstrains FromTicConstrains(Constrains constrains)
            =>new GenericConstrains(constrains.Ancestor , constrains.Descedant as Primitive, constrains.IsComparable);

        public GenericConstrains(Primitive ancestor = null, Primitive descendant = null, bool isComparable = false)
        {
            Ancestor = ancestor;
            Descendant = descendant;
            IsComparable = isComparable;
        }
    }
}