using System;
using System.Collections.Generic;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpretation.Nodes;

internal class ConstantExpressionNode : IExpressionNode {
    internal static ConstantExpressionNode CreateConcrete(FunnyType primitive, ulong value, TypeBehaviour typeBehaviour, Interval interval) =>
        primitive.BaseType switch {
            BaseFunnyType.Real   => new ConstantExpressionNode(typeBehaviour.GetRealConstantValue(value), FunnyType.Real, interval),
            BaseFunnyType.Int64  => new ConstantExpressionNode((long)value,   FunnyType.Int64, interval),
            BaseFunnyType.Int32  => new ConstantExpressionNode((int)value,    FunnyType.Int32, interval),
            BaseFunnyType.Int16  => new ConstantExpressionNode((short)value,  FunnyType.Int16, interval),
            BaseFunnyType.UInt64 => new ConstantExpressionNode((ulong)value,  FunnyType.UInt64, interval),
            BaseFunnyType.UInt32 => new ConstantExpressionNode((uint)value,   FunnyType.UInt32, interval),
            BaseFunnyType.UInt16 => new ConstantExpressionNode((ushort)value, FunnyType.UInt16, interval),
            BaseFunnyType.UInt8  => new ConstantExpressionNode((byte)value,   FunnyType.UInt8, interval),
            _                    => throw new ArgumentOutOfRangeException(nameof(primitive), primitive, null)
        };

    internal static ConstantExpressionNode CreateConcrete(FunnyType primitive, long value, TypeBehaviour typeBehaviour, Interval interval) =>
        primitive.BaseType switch {
            BaseFunnyType.Real   => new ConstantExpressionNode(typeBehaviour.GetRealConstantValue(value), FunnyType.Real, interval),
            BaseFunnyType.Int64  => new ConstantExpressionNode((long)value,   FunnyType.Int64, interval),
            BaseFunnyType.Int32  => new ConstantExpressionNode((int)value,    FunnyType.Int32, interval),
            BaseFunnyType.Int16  => new ConstantExpressionNode((short)value,  FunnyType.Int16, interval),
            BaseFunnyType.UInt64 => new ConstantExpressionNode((ulong)value,  FunnyType.UInt64, interval),
            BaseFunnyType.UInt32 => new ConstantExpressionNode((uint)value,   FunnyType.UInt32, interval),
            BaseFunnyType.UInt16 => new ConstantExpressionNode((ushort)value, FunnyType.UInt16, interval),
            BaseFunnyType.UInt8  => new ConstantExpressionNode((byte)value,   FunnyType.UInt8, interval),
            _                    => throw new ArgumentOutOfRangeException(nameof(primitive), primitive, null)
        };

    public ConstantExpressionNode(object objVal, FunnyType type, Interval interval) {
        _value = objVal;
        Interval = interval;
        Type = type;
    }

    private readonly object _value;

    public FunnyType Type { get; }
    public Interval Interval { get; }
    public IEnumerable<IRuntimeNode> Children => Array.Empty<IExpressionNode>();

    public object Calc() => _value;
    public IExpressionNode Clone(ICloneContext context) => this;
}
