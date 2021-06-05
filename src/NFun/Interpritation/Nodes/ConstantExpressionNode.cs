using System;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Nodes
{
    public class ConstantExpressionNode: IExpressionNode
    {
        private readonly object _value;
        public static ConstantExpressionNode CreateConcrete(FunnyType primitive, ulong value, Interval interval)
        {
            switch (primitive.BaseType)
            {
                case BaseFunnyType.Real:   return new ConstantExpressionNode((double)value, FunnyType.Real, interval);
                case BaseFunnyType.Int64:  return new ConstantExpressionNode((long) value, FunnyType.Int64, interval);
                case BaseFunnyType.Int32:  return new ConstantExpressionNode((int)  value, FunnyType.Int32, interval);
                case BaseFunnyType.Int16:  return new ConstantExpressionNode((short)value, FunnyType.Int16, interval);
                case BaseFunnyType.UInt64: return new ConstantExpressionNode((ulong)value, FunnyType.UInt64, interval);
                case BaseFunnyType.UInt32: return new ConstantExpressionNode((uint)value, FunnyType.UInt32, interval);
                case BaseFunnyType.UInt16: return new ConstantExpressionNode((ushort)value, FunnyType.UInt16, interval);
                case BaseFunnyType.UInt8:  return new ConstantExpressionNode((byte)value, FunnyType.UInt8, interval);
                default:
                    throw new ArgumentOutOfRangeException(nameof(primitive), primitive, null);
            }
        }
        public static ConstantExpressionNode CreateConcrete(FunnyType primitive, long value, Interval interval)
        {
            switch (primitive.BaseType)
            {
                case BaseFunnyType.Real:   return new ConstantExpressionNode((double)value, FunnyType.Real, interval);
                case BaseFunnyType.Int64:  return new ConstantExpressionNode((long) value, FunnyType.Int64, interval);
                case BaseFunnyType.Int32:  return new ConstantExpressionNode((int)  value, FunnyType.Int32, interval);
                case BaseFunnyType.Int16:  return new ConstantExpressionNode((short)value, FunnyType.Int16, interval);
                case BaseFunnyType.UInt64: return new ConstantExpressionNode((ulong)value, FunnyType.UInt64, interval);
                case BaseFunnyType.UInt32: return new ConstantExpressionNode((uint)value, FunnyType.UInt32, interval);
                case BaseFunnyType.UInt16: return new ConstantExpressionNode((ushort)value, FunnyType.UInt16, interval);
                case BaseFunnyType.UInt8:  return new ConstantExpressionNode((byte)value, FunnyType.UInt8, interval);
                default:
                    throw new ArgumentOutOfRangeException(nameof(primitive), primitive, null);
            }
        }

        public ConstantExpressionNode(object objVal, FunnyType type, Interval interval)
        {
            _value = objVal;
            Interval = interval;
            Type = type;
        }

        public FunnyType Type { get; }
        public Interval Interval { get; }
        public object Calc() => _value;
    }
}