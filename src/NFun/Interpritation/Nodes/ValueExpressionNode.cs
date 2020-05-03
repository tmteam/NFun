using System;
using NFun.Tic.SolvingStates;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Nodes
{
    public class GenericNumber
    {
        public static ValueExpressionNode CreateConcrete(Interval interval, long value,
            VarType primitiveTypeName)
            => new GenericNumber(0, interval, value).CreateConcrete(primitiveTypeName);

        private readonly long _value;

        public GenericNumber(int genericId, Interval interval, long value)
        {
            _value = value;
            GenericId = genericId;
            Interval = interval;
        }
        public int GenericId { get; }
        public Interval Interval { get; }

        public ValueExpressionNode CreateConcrete(VarType primitive)
        {
            switch (primitive.BaseType)
            {
                case BaseVarType.Real:   return new ValueExpressionNode((double)_value, VarType.Real,   Interval);
                case BaseVarType.Int64:  return new ValueExpressionNode(        _value, VarType.Int64,  Interval);
                case BaseVarType.Int32:  return new ValueExpressionNode((int)   _value, VarType.Int32,  Interval);
                case BaseVarType.Int16:  return new ValueExpressionNode((short) _value, VarType.Int16,  Interval);
                case BaseVarType.UInt64: return new ValueExpressionNode((ulong) _value, VarType.UInt64, Interval);
                case BaseVarType.UInt32: return new ValueExpressionNode((uint)  _value, VarType.UInt32, Interval);
                case BaseVarType.UInt16: return new ValueExpressionNode((ushort)_value, VarType.UInt16, Interval);
                case BaseVarType.UInt8:  return new ValueExpressionNode((byte)  _value, VarType.UInt8,  Interval);
                default:
                    throw new ArgumentOutOfRangeException(nameof(primitive), primitive, null);
            }
        }
    }
    public class ValueExpressionNode: IExpressionNode
    {
        private readonly object _value;

        public string ClrTypeName => _value?.GetType().Name;

        public static ValueExpressionNode CreateConcrete(VarType primitive, ulong value, Interval interval)
        {
            switch (primitive.BaseType)
            {
                case BaseVarType.Real: return new ValueExpressionNode((double)value, VarType.Real, interval);
                case BaseVarType.Int64: return new ValueExpressionNode(value, VarType.Int64, interval);
                case BaseVarType.Int32: return new ValueExpressionNode((int)value, VarType.Int32, interval);
                case BaseVarType.Int16: return new ValueExpressionNode((short)value, VarType.Int16, interval);
                case BaseVarType.UInt64: return new ValueExpressionNode((ulong)value, VarType.UInt64, interval);
                case BaseVarType.UInt32: return new ValueExpressionNode((uint)value, VarType.UInt32, interval);
                case BaseVarType.UInt16: return new ValueExpressionNode((ushort)value, VarType.UInt16, interval);
                case BaseVarType.UInt8: return new ValueExpressionNode((byte)value, VarType.UInt8, interval);
                default:
                    throw new ArgumentOutOfRangeException(nameof(primitive), primitive, null);
            }
        }
        public static ValueExpressionNode CreateConcrete(VarType primitive, long value, Interval interval)
        {
            switch (primitive.BaseType)
            {
                case BaseVarType.Real: return new ValueExpressionNode((double)value, VarType.Real, interval);
                case BaseVarType.Int64: return new ValueExpressionNode(value, VarType.Int64, interval);
                case BaseVarType.Int32: return new ValueExpressionNode((int)value, VarType.Int32, interval);
                case BaseVarType.Int16: return new ValueExpressionNode((short)value, VarType.Int16, interval);
                case BaseVarType.UInt64: return new ValueExpressionNode((ulong)value, VarType.UInt64, interval);
                case BaseVarType.UInt32: return new ValueExpressionNode((uint)value, VarType.UInt32, interval);
                case BaseVarType.UInt16: return new ValueExpressionNode((ushort)value, VarType.UInt16, interval);
                case BaseVarType.UInt8: return new ValueExpressionNode((byte)value, VarType.UInt8, interval);
                default:
                    throw new ArgumentOutOfRangeException(nameof(primitive), primitive, null);
            }
        }

        public ValueExpressionNode(object objVal, VarType type, Interval interval)
        {
            _value = objVal;
            Interval = interval;
            Type = type;
        }

        public VarType Type { get; }
        public Interval Interval { get; }
        public object Calc() => _value;
    }
}