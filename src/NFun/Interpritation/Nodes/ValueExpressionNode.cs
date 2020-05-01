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