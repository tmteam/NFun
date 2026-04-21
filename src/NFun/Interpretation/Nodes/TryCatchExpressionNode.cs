using System.Collections.Generic;
using NFun.Exceptions;
using NFun.Runtime;
using NFun.Runtime.Arrays;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpretation.Nodes;

internal class TryCatchExpressionNode : IExpressionNode {
    private readonly IExpressionNode _tryNode;
    private readonly IExpressionNode _catchNode;
    private readonly VariableSource _errorVariable;

    public TryCatchExpressionNode(
        IExpressionNode tryNode, IExpressionNode catchNode,
        VariableSource errorVariable,
        Interval interval, FunnyType type) {
        _tryNode = tryNode;
        _catchNode = catchNode;
        _errorVariable = errorVariable;
        Type = type;
        Interval = interval;
    }

    public FunnyType Type { get; }
    public Interval Interval { get; }
    public IEnumerable<IRuntimeNode> Children => new IRuntimeNode[] { _tryNode, _catchNode };

    public object Calc() {
        try {
            return _tryNode.Calc();
        }
        catch (FunnyRuntimeException ex) {
            if (_errorVariable != null) {
                var message = ex.Message ?? "oops";
                var data = ex.OopsData ?? FunnyNone.Instance;
                var errorStruct = FunnyStruct.Create(
                    ("message", new TextFunnyArray(message)),
                    ("data", data));
                _errorVariable.SetFunnyValueUnsafe(errorStruct);
            }
            return _catchNode.Calc();
        }
    }

    public IExpressionNode Clone(ICloneContext context) =>
        new TryCatchExpressionNode(
            _tryNode.Clone(context), _catchNode.Clone(context),
            _errorVariable, Interval, Type);
}
