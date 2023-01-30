namespace NFun.Interpretation.Nodes;

using System.Collections.Generic;
using Tokenization;

public interface IRuntimeNode {
    Interval Interval { get; }
    IEnumerable<IRuntimeNode> Children { get; }
}
