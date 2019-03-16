using System;
using System.Collections.Generic;
using System.Linq;
using Funny.Interpritation.Nodes;
using Funny.Parsing;
using Funny.Runtime;

namespace Funny.Interpritation
{
    public static class StandartOperations
    {
        public static IExpressionNode GetOp(LexNodeType type, IExpressionNode left, IExpressionNode right)
        {
            switch (type)
            {
                case LexNodeType.Plus:
                    switch (left.Type.BaseType)
                    {
                        case PrimitiveVarType.RealType when right.Type.BaseType == PrimitiveVarType.RealType:
                            return new OpExpressionNodeOfT<double,double,double>(left,right, (l, r) => l + r);
                        case PrimitiveVarType.RealType when right.Type.BaseType == PrimitiveVarType.IntType:
                            return new OpExpressionNodeOfT<double,int,double>(left,right, (l, r) => l + r);
                        case PrimitiveVarType.IntType when right.Type.BaseType == PrimitiveVarType.RealType:
                            return new OpExpressionNodeOfT<int,double,double>(left,right, (l, r) => l + r);
                        case PrimitiveVarType.IntType when right.Type.BaseType == PrimitiveVarType.IntType:
                            return new OpExpressionNodeOfT<int,int,int>(left,right, (l, r) => l + r);
                        case PrimitiveVarType.TextType:
                            return new OpExpressionNodeOfT<string, object,string>(left,right, (l,r)=> l+r);
                        default:
                            throw new OutpuCastParseException($"\"{type}\" cast error. Left operand is {left.Type} and right is {right.Type}");
                    }
                case LexNodeType.Minus:
                    switch (left.Type.BaseType)
                    {
                        case PrimitiveVarType.RealType when right.Type.BaseType == PrimitiveVarType.RealType:
                            return new OpExpressionNodeOfT<double,double,double>(left,right, (l, r) => l - r);
                        case PrimitiveVarType.RealType when right.Type.BaseType == PrimitiveVarType.IntType:
                            return new OpExpressionNodeOfT<double,int,double>(left,right, (l, r) => l - r);
                        case PrimitiveVarType.IntType when right.Type.BaseType == PrimitiveVarType.RealType:
                            return new OpExpressionNodeOfT<int,double,double>(left,right, (l, r) => l - r);
                        case PrimitiveVarType.IntType when right.Type.BaseType == PrimitiveVarType.IntType:
                            return new OpExpressionNodeOfT<int,int,int>(left,right, (l, r) => l - r);
                        default:
                            throw new OutpuCastParseException($"\"{type}\" cast error. Left operand is {left.Type} and right is {right.Type}");
                    }
                case LexNodeType.Div:
                    switch (left.Type.BaseType)
                    {
                        case PrimitiveVarType.RealType when right.Type.BaseType == PrimitiveVarType.RealType:
                            return new OpExpressionNodeOfT<double,double,double>(left,right, (l, r) => l/r);
                        case PrimitiveVarType.RealType when right.Type.BaseType == PrimitiveVarType.IntType:
                            return new OpExpressionNodeOfT<double,int,double>(left,right, (l, r) => l/(double)r);
                        case PrimitiveVarType.IntType when right.Type.BaseType == PrimitiveVarType.RealType:
                            return new OpExpressionNodeOfT<int,double,double>(left,right, (l, r) => l/r);
                        case PrimitiveVarType.IntType when right.Type.BaseType == PrimitiveVarType.IntType:
                            return new OpExpressionNodeOfT<int,int,double>(left,right, (l, r) => l/(double)r);
                        default:
                            throw new OutpuCastParseException($"\"{type}\" cast error. Left operand is {left.Type} and right is {right.Type}");
                    }
                case LexNodeType.Rema:
                    switch (left.Type.BaseType)
                    {
                        case PrimitiveVarType.RealType when right.Type.BaseType == PrimitiveVarType.RealType:
                            return new OpExpressionNodeOfT<double,double,double>(left,right, (l, r) => l%r);
                        case PrimitiveVarType.RealType when right.Type.BaseType == PrimitiveVarType.IntType:
                            return new OpExpressionNodeOfT<double,int,double>(left,right, (l, r) => l%r);
                        case PrimitiveVarType.IntType when right.Type.BaseType == PrimitiveVarType.RealType:
                            return new OpExpressionNodeOfT<int,double,double>(left,right, (l, r) => l%r);
                        case PrimitiveVarType.IntType when right.Type.BaseType == PrimitiveVarType.IntType:
                            return new OpExpressionNodeOfT<int,int,int>(left,right, (l, r) => l%r);
                        default:
                            throw new OutpuCastParseException($"\"{type}\" cast error. Left operand is {left.Type} and right is {right.Type}");
                    }
                case LexNodeType.Mult:
                    switch (left.Type.BaseType)
                    {
                        case PrimitiveVarType.RealType when right.Type.BaseType == PrimitiveVarType.RealType:
                            return new OpExpressionNodeOfT<double,double,double>(left,right, (l, r) => l*r);
                        case PrimitiveVarType.RealType when right.Type.BaseType == PrimitiveVarType.IntType:
                            return new OpExpressionNodeOfT<double,int,double>(left,right, (l, r) => l*r);
                        case PrimitiveVarType.IntType when right.Type.BaseType == PrimitiveVarType.RealType:
                            return new OpExpressionNodeOfT<int,double,double>(left,right, (l, r) => l*r);
                        case PrimitiveVarType.IntType when right.Type.BaseType == PrimitiveVarType.IntType:
                            return new OpExpressionNodeOfT<int,int,int>(left,right, (l, r) => l*r);
                        default:
                            throw new OutpuCastParseException($"\"{type}\" cast error. Left operand is {left.Type} and right is {right.Type}");
                    }
                case LexNodeType.Pow:
                    switch (left.Type.BaseType)
                    {
                        case PrimitiveVarType.RealType when right.Type.BaseType == PrimitiveVarType.RealType:
                            return new OpExpressionNodeOfT<double,double,double>(left,right, Math.Pow);
                        case PrimitiveVarType.RealType when right.Type.BaseType == PrimitiveVarType.IntType:
                            return new OpExpressionNodeOfT<double,int,double>(left,right, (l, r) => Math.Pow(l, r));
                        case PrimitiveVarType.IntType when right.Type.BaseType == PrimitiveVarType.RealType:
                            return new OpExpressionNodeOfT<int,double,double>(left,right, (l, r) => Math.Pow(l, r));
                        case PrimitiveVarType.IntType when right.Type.BaseType == PrimitiveVarType.IntType:
                            return new OpExpressionNodeOfT<int,int,int>(left,right, (l, r) => (int)Math.Pow(l, r));
                        default:
                            throw new OutpuCastParseException($"\"{type}\" cast error. Left operand is {left.Type} and right is {right.Type}");
                    }
                case LexNodeType.And:
                    return GetBooleanOpOrThrow(type, left, right, (a, b) => a && b);
                case LexNodeType.Or:
                    return GetBooleanOpOrThrow(type, left, right, (a, b) => a || b);
                case LexNodeType.Xor:
                    return GetBooleanOpOrThrow(type, left, right, (a, b) =>  a != b);
                case LexNodeType.Equal:
                    switch (left.Type.BaseType)
                    {
                        case PrimitiveVarType.RealType when right.Type == VarType.RealType:
                            return new OpExpressionNodeOfT<double,double,bool>(left,right, (l,r)=> l==r);
                        case PrimitiveVarType.RealType when right.Type == VarType.IntType:
                            return new OpExpressionNodeOfT<double,int,bool>(left,right, (l,r)=> l==r);
                        case PrimitiveVarType.RealType when right.Type == VarType.BoolType:
                            return new OpExpressionNodeOfT<double,bool,bool>(left,right, (l,r)=> l!=0 == r);
                        case PrimitiveVarType.IntType when right.Type == VarType.RealType:
                            return new OpExpressionNodeOfT<int,double,bool>(left,right, (l,r)=> l==r);
                        case PrimitiveVarType.IntType when right.Type == VarType.IntType:
                            return new OpExpressionNodeOfT<int,int,bool>(left,right, (l,r)=> l==r);
                        case PrimitiveVarType.IntType when right.Type == VarType.BoolType:
                            return new OpExpressionNodeOfT<int,bool,bool>(left,right, (l,r)=> l!=0 == r);
                        case PrimitiveVarType.BoolType when right.Type == VarType.RealType:
                            return new OpExpressionNodeOfT<bool,double,bool>(left,right, (l,r)=> l == (r!=0));
                        case PrimitiveVarType.BoolType when right.Type == VarType.IntType:
                            return new OpExpressionNodeOfT<bool,int,bool>(left,right, (l,r)=> l == (r!=0));
                        case PrimitiveVarType.BoolType when right.Type == VarType.BoolType:
                            return new OpExpressionNodeOfT<bool,bool,bool>(left,right, (l,r)=> l == r);
                        case PrimitiveVarType.Array when right.Type== VarType.ArrayOf(VarType.RealType):
                            return new OpExpressionNodeOfT<double[],double[],bool>(left,right, (l,r)=> l.SequenceEqual(r));
                        default:
                            throw new OutpuCastParseException($"\"{type}\" cast error. Left operand is {left.Type} and right is {right.Type}");
                    }
                case LexNodeType.NotEqual:
                    switch (left.Type.BaseType)
                    {
                        case PrimitiveVarType.RealType when right.Type == VarType.RealType:
                            return new OpExpressionNodeOfT<double,double,bool>(left,right, (l,r)=> l!=r);
                        case PrimitiveVarType.RealType when right.Type == VarType.IntType:
                            return new OpExpressionNodeOfT<double,int,bool>(left,right, (l,r)=> l!=r);
                        case PrimitiveVarType.RealType when right.Type == VarType.BoolType:
                            return new OpExpressionNodeOfT<double,bool,bool>(left,right, (l,r)=> l!=0 != r);
                        case PrimitiveVarType.IntType when right.Type == VarType.RealType:
                            return new OpExpressionNodeOfT<int,double,bool>(left,right, (l,r)=> l!=r);
                        case PrimitiveVarType.IntType when right.Type == VarType.IntType:
                            return new OpExpressionNodeOfT<int,int,bool>(left,right, (l,r)=> l!=r);
                        case PrimitiveVarType.IntType when right.Type == VarType.BoolType:
                            return new OpExpressionNodeOfT<int,bool,bool>(left,right, (l,r)=> l!=0 != r);
                        case PrimitiveVarType.BoolType when right.Type == VarType.RealType:
                            return new OpExpressionNodeOfT<bool,double,bool>(left,right, (l,r)=> l != (r!=0));
                        case PrimitiveVarType.BoolType when right.Type == VarType.IntType:
                            return new OpExpressionNodeOfT<bool,int,bool>(left,right, (l,r)=> l != (r!=0));
                        case PrimitiveVarType.BoolType when right.Type == VarType.BoolType:
                            return new OpExpressionNodeOfT<bool,bool,bool>(left,right, (l,r)=> l!=r);
                        case PrimitiveVarType.Array when right.Type== VarType.ArrayOf(VarType.RealType):
                            return new OpExpressionNodeOfT<double[],double[],bool>(left,right, (l,r)=> !l.SequenceEqual(r));
                        default:
                            throw new OutpuCastParseException($"\"{type}\" cast error. Left operand is {left.Type} and right is {right.Type}");
                    }              
                case LexNodeType.Less:
                    switch (left.Type.BaseType)
                    {
                        case PrimitiveVarType.RealType when right.Type == VarType.RealType:
                            return new OpExpressionNodeOfT<double,double,bool>(left,right, (l,r)=> l<r);
                        case PrimitiveVarType.RealType when right.Type == VarType.IntType:
                            return new OpExpressionNodeOfT<double,int,bool>(left,right, (l,r)=> l<r);
                        case PrimitiveVarType.IntType when right.Type == VarType.RealType:
                            return new OpExpressionNodeOfT<int,double,bool>(left,right, (l,r)=> l<r);
                        case PrimitiveVarType.IntType when right.Type == VarType.IntType:
                            return new OpExpressionNodeOfT<int,int,bool>(left,right, (l,r)=> l<r);
                        default:
                            throw new OutpuCastParseException($"\"{type}\" cast error. Left operand is {left.Type} and right is {right.Type}");
                    }
                case LexNodeType.LessOrEqual:
                    switch (left.Type.BaseType)
                    {
                        case PrimitiveVarType.RealType when right.Type == VarType.RealType:
                            return new OpExpressionNodeOfT<double,double,bool>(left,right, (l,r)=> l<=r);
                        case PrimitiveVarType.RealType when right.Type == VarType.IntType:
                            return new OpExpressionNodeOfT<double,int,bool>(left,right, (l,r)=> l<=r);
                        case PrimitiveVarType.IntType when right.Type == VarType.RealType:
                            return new OpExpressionNodeOfT<int,double,bool>(left,right, (l,r)=> l<=r);
                        case PrimitiveVarType.IntType when right.Type == VarType.IntType:
                            return new OpExpressionNodeOfT<int,int,bool>(left,right, (l,r)=> l<=r);
                        default:
                            throw new OutpuCastParseException($"\"{type}\" cast error. Left operand is {left.Type} and right is {right.Type}");
                    }     
                case LexNodeType.More:
                    switch (left.Type.BaseType)
                    {
                        case PrimitiveVarType.RealType when right.Type == VarType.RealType:
                            return new OpExpressionNodeOfT<double,double,bool>(left,right, (l,r)=> l>r);
                        case PrimitiveVarType.RealType when right.Type == VarType.IntType:
                            return new OpExpressionNodeOfT<double,int,bool>(left,right, (l,r)=> l>r);
                        case PrimitiveVarType.IntType when right.Type == VarType.RealType:
                            return new OpExpressionNodeOfT<int,double,bool>(left,right, (l,r)=> l>r);
                        case PrimitiveVarType.IntType when right.Type == VarType.IntType:
                            return new OpExpressionNodeOfT<int,int,bool>(left,right, (l,r)=> l>r);
                        default:
                            throw new OutpuCastParseException($"\"{type}\" cast error. Left operand is {left.Type} and right is {right.Type}");
                    }
                case LexNodeType.MoreOrEqual:
                    switch (left.Type.BaseType)
                    {
                        case PrimitiveVarType.RealType when right.Type == VarType.RealType:
                            return new OpExpressionNodeOfT<double,double,bool>(left,right, (l,r)=> l>=r);
                        case PrimitiveVarType.RealType when right.Type == VarType.IntType:
                            return new OpExpressionNodeOfT<double,int,bool>(left,right, (l,r)=> l>=r);
                        case PrimitiveVarType.IntType when right.Type == VarType.RealType:
                            return new OpExpressionNodeOfT<int,double,bool>(left,right, (l,r)=> l>=r);
                        case PrimitiveVarType.IntType when right.Type == VarType.IntType:
                            return new OpExpressionNodeOfT<int,int,bool>(left,right, (l,r)=> l>=r);
                        default:
                            throw new OutpuCastParseException($"\"{type}\" cast error. Left operand is {left.Type} and right is {right.Type}");
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
            throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        private static IExpressionNode GetBooleanOpOrThrow(LexNodeType type, IExpressionNode left, IExpressionNode right, Func<bool,bool,bool> op)
        {
            if (left.Type != VarType.BoolType && right.Type != VarType.BoolType)
                throw new OutpuCastParseException($"\"{type}\" cast error. Left operand is {left.Type} and right is {right.Type}");
            return new OpExpressionNodeOfT<bool, bool, bool>(left, right,op);
        }

        public static bool IsDefaultOp(LexNodeType type) => _ops.ContainsKey(type);
        private static Dictionary<LexNodeType, Func<double, double, double>> _ops =
            new Dictionary<LexNodeType, Func<double, double, double>>()
            {
                {LexNodeType.Plus,(a, b) => a + b},
                {LexNodeType.Minus,(a, b) => a - b},
                {LexNodeType.Div,(a, b) => a / b},
                {LexNodeType.Mult,(a, b) => a * b},
                {LexNodeType.Pow,Math.Pow},
                {LexNodeType.Rema,(a, b) => a % b},
                {LexNodeType.And,(a, b) => (a != 0 && b != 0) ? 1 : 0},
                {LexNodeType.Or,(a, b) => (a != 0 || b != 0) ? 1 : 0},        
                {LexNodeType.Xor,(a, b) => ((a != 0) != (b != 0)) ? 1 : 0},
                {LexNodeType.Equal, (a, b) => (a == b) ? 1 : 0},                    
                {LexNodeType.NotEqual,(a, b) => (a != b) ? 1 : 0},                    
                {LexNodeType.Less,(a, b) => (a < b) ? 1 : 0},                   
                {LexNodeType.LessOrEqual,(a, b) => (a <= b) ? 1 : 0},                    
                {LexNodeType.More,(a, b) => (a > b) ? 1 : 0},                    
                {LexNodeType.MoreOrEqual,(a, b) => (a >= b) ? 1 : 0},                  
            };
        
    }
}