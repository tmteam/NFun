namespace NFun.Tic.SolvingStates;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Language-level mutable struct. Fields are invariant; width subtyping holds.
/// MutStruct{a:T} &lt;: Struct{a:T}.</summary>
public class StateMutableStruct : StateStruct {

    public StateMutableStruct(Dictionary<string, TicNode> fields, bool isFrozen, bool isOpen = false)
        : base(fields, isFrozen, isOpen) { }

    public StateMutableStruct(bool isOpen = false) : base(isOpen) { }

    public StateMutableStruct(string name, TicNode node, bool isFrozen, bool isOpen = false)
        : base(name, node, isFrozen, isOpen) { }

    internal StateMutableStruct(FieldMap fields, bool isFrozen, bool isOpen = false)
        : base(fields, isFrozen, isOpen) { }

    public bool IsFieldMutable => true;

    public override ICompositeState GetNonReferenced() {
        var nodeCopy = new Dictionary<string, TicNode>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in Fields)
            nodeCopy.Add(key, value.GetNonReference());
        // Preserve TypeName + IsOptionalSourced across flattening so nominal short-circuit
        // still fires; mirrors StateStruct.GetNonReferenced.
        return new StateMutableStruct(nodeCopy, IsFrozen, IsOpen) {
            TypeName = TypeName,
            IsOptionalSourced = IsOptionalSourced,
        };
    }

    public static new StateMutableStruct Of(params (string, ITicNodeState)[] fields)
        => Of(true, fields);

    public static new StateMutableStruct Of(bool isFrozen, params (string, ITicNodeState)[] fields) {
        var nodeFields = new Dictionary<string, TicNode>();
        foreach (var (key, value) in fields)
        {
            var node = value switch {
                ITypeState at       => TicNode.CreateTypeVariableNode(at),
                StateRefTo aRef     => aRef.Node,
                ConstraintsState ct => TicNode.CreateInvisibleNode(ct),
                _                   => throw new InvalidOperationException()
            };
            nodeFields.Add(key, node);
        }

        return new StateMutableStruct(nodeFields, isFrozen);
    }

    public override bool Equals(object obj) {
        if (obj is not StateMutableStruct mutStruct) return false;
        if (ReferenceEquals(this, mutStruct)) return true;
        return base.Equals(mutStruct);
    }

    public override string ToString() {
        if (FieldsCount == 0)
            return IsFrozen ? "mut{frozen}" : "mut{}";
        return "mut{" + (IsFrozen ? "F|" : "") +
               string.Join("; ",
                   Fields.Select(n => $"{n.Key}:{(n.Value.State is StatePrimitive p ? p.ToString() : "..")}"))
               + "}";
    }

    public override string PrintState(int depth) {
        if (depth > 100)
            return "mut{...REQ...}";
        if (FieldsCount == 0)
            return IsFrozen ? "mut{frozen}" : "mut{}";
        return
            "mut{"
            + (IsFrozen ? "F|" : "")
            + string.Join("; ", Fields.Select(n => $"{n.Key}:{n.Value.State.PrintState(depth + 1)}"))
            + "}";
    }
}
