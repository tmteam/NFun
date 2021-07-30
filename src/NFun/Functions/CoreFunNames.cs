namespace NFun.Functions
{
    public static class CoreFunNames
    {
        public const string BitAnd = "bitand";
        public const string Add = "add";
        public const string GetElementName = "get";
        public const string SliceName = "slice";
        public const string RangeName = "range";
        public const string Substract = "substract";
        public const string BitOr = "bitor";
        public const string BitXor = "bitxor";
        public const string BitInverse = "bitinverse";
        public const string Multiply = "multiply";
        public const string Divide = "divide";
        public const string Pow = "pow";
        public const string Remainder = "rema";
        public const string And = "and";
        public const string Or = "or";
        public const string Xor = "xor";
        public const string Equal = "equal";
        public const string NotEqual = "notequal";
        public const string Less = "less";
        public const string LessOrEqual = "lessorequal";
        public const string More = "more";
        public const string MoreOrEqual = "moreorequal";
        public const string BitShiftLeft = "bitshiftleft";
        public const string In = "in";
        public const string BitShiftRight = "bitshiftright";
        public const string ArrConcat = "@";
        public const string Not = "invert";
        public const string Negate = "-_invert_num";
        public static string Format = "@Format";
        public static string ToText = "toText";
        //UsedInInterpolation
        
        /// <summary>
        /// General concat text function for array of texts
        /// </summary>
        public static string ConcatArrayOfTexts = "@concatTexts";
        /// <summary>
        /// Concat two texts function. Optimized vertion for 2-arg case
        /// </summary>
        public static string Concat2Texts = "@concat2Texts";
        /// <summary>
        /// Concat three texts function. Optimized vertion for 3-arg case
        /// </summary>
        public static string Concat3Texts = "@concat3Texts";

    }
}