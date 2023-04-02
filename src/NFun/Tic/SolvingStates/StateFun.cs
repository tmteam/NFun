using System;
using System.Collections.Generic;
using System.Linq;

namespace NFun.Tic.SolvingStates;

public class StateFun : ICompositeState, ITypeState, ITicNodeState {
    public static StateFun Of(ITicNodeState[] argTypes, ITicNodeState returnType) {
        TicNode[] argNodes = new TicNode[argTypes.Length];

        TicNode retNode = returnType switch {
                              ITypeState rt     => TicNode.CreateTypeVariableNode(rt),
                              StateRefTo retRef => retRef.Node,
                              _                 => throw new InvalidOperationException()
                          };

        for (int i = 0; i < argTypes.Length; i++)
        {
            argNodes[i] = argTypes[i] switch {
                              ITypeState at   => TicNode.CreateTypeVariableNode(at),
                              StateRefTo aRef => aRef.Node,
                              _               => throw new InvalidOperationException()
                          };
        }


        return new StateFun(argNodes, retNode);
    }

    public static StateFun Of(ITicNodeState argType, ITicNodeState returnType)
        => Of(new[] { argType }, returnType);

    public static StateFun Of(ITypeState[] argTypes, ITypeState retType) {
        var argNodes = new TicNode[argTypes.Length];
        for (int i = 0; i < argTypes.Length; i++) argNodes[i] = TicNode.CreateTypeVariableNode(argTypes[i]);
        return new StateFun(
            argNodes: argNodes,
            retNode: TicNode.CreateTypeVariableNode(retType));
    }

    public static StateFun Of(TicNode[] argNodes, TicNode returnNode)
        => new(argNodes, returnNode);

    public static StateFun Of(TicNode argNode, TicNode returnNode)
        => new(new[] { argNode }, returnNode);

    private StateFun(TicNode[] argNodes, TicNode retNode) {
        ArgNodes = argNodes;
        RetNode = retNode;
    }

    public ITicNodeState ReturnType => RetNode.State;
    private ITicNodeState GetArgType(int index) => ArgNodes[index].State;
    public TicNode RetNode { get; }
    public TicNode[] ArgNodes { get; }
    public IEnumerable<ITicNodeState> Args => ArgNodes.Select(a => a.State);

    public int ArgsCount => ArgNodes.Length;
    public bool IsSolved => RetNode.IsSolved && ArgNodes.All(n => n.IsSolved);
    public bool IsMutable => !IsSolved;

    public ITypeState GetLastCommonAncestorOrNull(ITypeState otherType) {
        var funType = otherType as StateFun;

        if (funType == null)
            return StatePrimitive.Any;

        if (funType.ArgsCount != ArgsCount)
            return StatePrimitive.Any;

        if (ReturnType is not ITypeState returnType)
            return null;
        if (funType.ReturnType is not ITypeState returnTypeB)
            return null;
        if (!returnType.IsSolved || !returnTypeB.IsSolved)
            return null;

        var returnAnc = returnType.GetLastCommonAncestorOrNull(returnTypeB);

        ITypeState[] argTypes = new ITypeState[ArgsCount];

        for (int i = 0; i < ArgsCount; i++)
        {
            var aArg = GetArgType(i);
            var bArg = funType.GetArgType(i);
            if (!(aArg is ITypeState typeA && bArg is ITypeState typeB))
                return null;


            if (!(typeA.IsSolved && typeB.IsSolved))
                return null;

            if (typeA.Equals(typeB))
                argTypes[i] = typeA;
            else if (aArg is StatePrimitive primitiveA && bArg is StatePrimitive primitiveB)
            {
                var argDesc = primitiveA.GetFirstCommonDescendantOrNull(primitiveB);
                if (argDesc == null)
                    return null;
                argTypes[i] = argDesc;
            }
            else return null;
        }

        return Of(retType: returnAnc, argTypes: argTypes);
    }

    public bool CanBeImplicitlyConvertedTo(StatePrimitive type)
        => type.Equals(StatePrimitive.Any);

    public override bool Equals(object obj) {
        if (obj is not StateFun fun)
            return false;
        if (fun.ArgsCount != ArgsCount)
            return false;

        for (int i = 0; i < ArgsCount; i++)
        {
            var funArg = fun.ArgNodes[i];
            var myArg = ArgNodes[i];
            if (funArg.IsMutable || myArg.IsMutable)
            {
                if (funArg != myArg)
                    return false;
            }
            else
            {
                if (!funArg.GetNonReference().State.Equals(myArg.GetNonReference().State))
                    return false;
            }
        }

        return fun.ReturnType.Equals(ReturnType);
    }

    public ICompositeState GetNonReferenced() {
        var nonRefArgNodes = new TicNode[ArgNodes.Length];
        for (int i = 0; i < ArgNodes.Length; i++) nonRefArgNodes[i] = ArgNodes[i].GetNonReference();

        return new StateFun(nonRefArgNodes, RetNode.GetNonReference());
    }

    /// <summary>
    /// State of any Member node is 'RefTo'
    /// </summary>
    public bool HasAnyReferenceMember
    {
        get
        {
            if (RetNode.State is StateRefTo)
                return true;
            foreach (var arg in ArgNodes)
            {
                if (arg.State is StateRefTo)
                    return true;
            }
            return false;
        }
    }

    public IEnumerable<TicNode> Members => ArgNodes.Append(RetNode);

    public IEnumerable<TicNode> AllLeafTypes
    {
        get
        {
            foreach (var member in Members)
            {
                if (member.State is ICompositeState composite)
                {
                    foreach (var leaf in composite.AllLeafTypes)
                    {
                        yield return leaf;
                    }
                }
                else
                {
                    yield return member;
                }
            }
        }
    }

    public override string ToString() {
        if (ArgsCount == 1)
            return $"({GetArgType(0)}->{ReturnType})";
        return $"(({string.Join(",", ArgNodes.Select(a => a.State))})->{ReturnType})";
    }

    public string Description => $"({string.Join(",", ArgNodes.Select(a => a.Name))})->{RetNode.Name}";
}
