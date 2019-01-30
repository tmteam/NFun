using System;
using System.Collections.Generic;

namespace Funny.Take2
{
    public interface IExpressionNode
    {
        IEnumerable<IExpressionNode> Children { get; }
        double Calc();
    }

    public class ValueExpressionNode: IExpressionNode
    {
        private readonly double _value;

        public ValueExpressionNode(double value)
        {
            _value = value;
        }

        public IEnumerable<IExpressionNode> Children {
            get { yield break;}
        }
        public double Calc() => _value;
    }

    public  class OpExpressionNode : IExpressionNode
    {
        private readonly IExpressionNode _a;
        private readonly IExpressionNode _b;
        private readonly Func<double, double, double> _op;
        public OpExpressionNode(IExpressionNode a, IExpressionNode b, Func<double,double,double> op)
        {
            _a = a;
            _b = b;
            _op = op;
        }
        public IEnumerable<IExpressionNode> Children {
            get { 
                yield return _a;
                yield return _b;
            }
        }
        public double Calc() => _op(_a.Calc(), _b.Calc());
    }
    
    public class VariableExpressionNode : IExpressionNode
    {
        public string Name { get; }

        public VariableExpressionNode(string name)
        {
            Name = name;
        }

        private double _value;
        public void SetValue(double value) => _value = value;
        public IEnumerable<IExpressionNode> Children {
            get { yield break;}
        }
        public double Calc() => _value;
    }
}