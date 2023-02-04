namespace NFun.Interpretation.Nodes;

using System;
using System.Collections.Generic;
using Functions;
using Tokenization;

internal class ComparisonChainExpressionNode : IExpressionNode {
    private readonly IExpressionNode[] _children;
    private readonly FunctionWithTwoArgs[] _functions;
    private readonly Func<object, object>[] _converters;

    public ComparisonChainExpressionNode(
        IExpressionNode[] children,
        FunctionWithTwoArgs[] functions,
        Func<object,object>[] converters, //first-left, second-right, second-left, thirt-right...
        Interval interval) {
        _children = children;
        _functions = functions;
        _converters = converters;
        Interval = interval;
    }

    public Interval Interval { get; }
    public IEnumerable<IRuntimeNode> Children => _children;
    public FunnyType Type { get; } = FunnyType.Bool;

    public IExpressionNode Clone(ICloneContext context) {
        var funCopy =  _functions.SelectToArray(o=>(FunctionWithTwoArgs) o.Clone(context));
        var argNodesCopy = _children.SelectToArray(a => a.Clone(context));
        return new ComparisonChainExpressionNode(argNodesCopy, funCopy, _converters, Interval);
    }

    public object Calc() {
        var arg1 = _children[0].Calc();

        for (int i = 0; i < _functions.Length; i++)
        {
            var arg2 = _children[i + 1].Calc();

            var indexOfLeftConverter = i*2;   //0, 2, 4..
            var indexOfRightConverter = i*2+1;

            var convertedArg1 = _converters[indexOfLeftConverter]?.Invoke(arg1) ?? arg1;
            var convertedArg2 = _converters[indexOfRightConverter]?.Invoke(arg2) ?? arg2;

            if (!_functions[i].Calc(convertedArg1, convertedArg2).Equals(true))
                return false;

            arg1 = arg2;
        }

        return true;
    }
}
