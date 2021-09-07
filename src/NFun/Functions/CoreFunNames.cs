namespace NFun.Functions {

internal static class CoreFunNames {
    public const string BitAnd = "bitand";
    public const string Add = "+";
    public const string GetElementName = "[]";
    public const string SliceName = "slice";
    public const string RangeName = "range";
    public const string Substract = "-";
    public const string BitOr = "|";
    public const string BitXor = "^";
    public const string BitInverse = "~";
    public const string Multiply = "*";
    public const string DivideReal = "/";
    public const string DivideInt = "//";
    public const string Pow = "**";
    public const string Remainder = "%";
    public const string And = "and";
    public const string Or = "or";
    public const string Xor = "xor";
    public const string Equal = "==";
    public const string NotEqual = "<>";
    public const string Less = "<";
    public const string LessOrEqual = "<=";
    public const string More = ">";
    public const string MoreOrEqual = ">=";
    public const string BitShiftLeft = "<<";
    public const string In = "in";
    public const string BitShiftRight = ">>";
    public const string Not = "!";
    public const string Negate = "-negate";
    public const string ToText = "toText";

    //UsedInInterpolation:

    /// <summary>
    /// General concat text function for array of texts
    /// </summary>
    public const string ConcatArrayOfTexts = "@concatTexts";

    /// <summary>
    /// Concat two texts function. Optimized vertion for 2-arg case
    /// </summary>
    public const string Concat2Texts = "@concat2Texts";

    /// <summary>
    /// Concat three texts function. Optimized vertion for 3-arg case
    /// </summary>
    public const string Concat3Texts = "@concat3Texts";
}

}