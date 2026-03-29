namespace NFun.Functions;

public enum BinOp : byte {
    Add, Subtract, Multiply, DivideReal, DivideInt, Pow, Remainder,
    And, Or, Xor,
    BitAnd, BitOr, BitXor, BitShiftLeft, BitShiftRight,
    Equal, NotEqual, Less, LessOrEqual, More, MoreOrEqual,
}

public enum UnOp : byte {
    Negate, Not, BitInverse,
}

internal static class OperatorEnumHelper {
    public static BinOp? TryParseBinOp(string name) => name switch {
        CoreFunNames.Add => BinOp.Add,
        CoreFunNames.Substract => BinOp.Subtract,
        CoreFunNames.Multiply => BinOp.Multiply,
        CoreFunNames.DivideReal => BinOp.DivideReal,
        CoreFunNames.DivideInt => BinOp.DivideInt,
        CoreFunNames.Pow => BinOp.Pow,
        CoreFunNames.Remainder => BinOp.Remainder,
        CoreFunNames.And => BinOp.And,
        CoreFunNames.Or => BinOp.Or,
        CoreFunNames.Xor => BinOp.Xor,
        CoreFunNames.BitAnd => BinOp.BitAnd,
        CoreFunNames.BitOr => BinOp.BitOr,
        CoreFunNames.BitXor => BinOp.BitXor,
        CoreFunNames.BitShiftLeft => BinOp.BitShiftLeft,
        CoreFunNames.BitShiftRight => BinOp.BitShiftRight,
        CoreFunNames.Equal => BinOp.Equal,
        CoreFunNames.NotEqual => BinOp.NotEqual,
        CoreFunNames.Less => BinOp.Less,
        CoreFunNames.LessOrEqual => BinOp.LessOrEqual,
        CoreFunNames.More => BinOp.More,
        CoreFunNames.MoreOrEqual => BinOp.MoreOrEqual,
        _ => null
    };

    public static UnOp? TryParseUnOp(string name) => name switch {
        CoreFunNames.Negate => UnOp.Negate,
        CoreFunNames.Not => UnOp.Not,
        CoreFunNames.BitInverse => UnOp.BitInverse,
        _ => null
    };

    public static string ToName(BinOp op) => op switch {
        BinOp.Add => CoreFunNames.Add,
        BinOp.Subtract => CoreFunNames.Substract,
        BinOp.Multiply => CoreFunNames.Multiply,
        BinOp.DivideReal => CoreFunNames.DivideReal,
        BinOp.DivideInt => CoreFunNames.DivideInt,
        BinOp.Pow => CoreFunNames.Pow,
        BinOp.Remainder => CoreFunNames.Remainder,
        BinOp.And => CoreFunNames.And,
        BinOp.Or => CoreFunNames.Or,
        BinOp.Xor => CoreFunNames.Xor,
        BinOp.BitAnd => CoreFunNames.BitAnd,
        BinOp.BitOr => CoreFunNames.BitOr,
        BinOp.BitXor => CoreFunNames.BitXor,
        BinOp.BitShiftLeft => CoreFunNames.BitShiftLeft,
        BinOp.BitShiftRight => CoreFunNames.BitShiftRight,
        BinOp.Equal => CoreFunNames.Equal,
        BinOp.NotEqual => CoreFunNames.NotEqual,
        BinOp.Less => CoreFunNames.Less,
        BinOp.LessOrEqual => CoreFunNames.LessOrEqual,
        BinOp.More => CoreFunNames.More,
        BinOp.MoreOrEqual => CoreFunNames.MoreOrEqual,
        _ => op.ToString()
    };

    public static string ToName(UnOp op) => op switch {
        UnOp.Negate => CoreFunNames.Negate,
        UnOp.Not => CoreFunNames.Not,
        UnOp.BitInverse => CoreFunNames.BitInverse,
        _ => op.ToString()
    };
}
