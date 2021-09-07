using System.Collections.Generic;
using NFun.Interpretation.Nodes;

namespace NFun.Runtime {

internal class VariableUsages {
    public readonly VariableSource Source;
    public readonly LinkedList<VariableExpressionNode> Usages = new();

    internal VariableUsages(VariableSource source) { Source = source; }
}

}