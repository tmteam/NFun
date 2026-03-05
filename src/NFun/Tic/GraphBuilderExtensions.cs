using System;
using System.Collections.Generic;
using NFun.Tic.SolvingStates;

namespace NFun.Tic;

/// <summary>
/// Convenience methods that compose core GraphBuilder operations
/// into NFun language constructs (constants, arrays, structs, lambdas, etc.)
/// </summary>
public static class GraphBuilderExtensions {

    public static void SetVar(this GraphBuilder b, string name, int node) {
        var namedNode = b.GetNamedNode(name);
        var idNode = b.GetOrCreateNode(node);
        if (idNode.State is ConstraintsState)
            namedNode.AddAncestor(idNode);
        else
            throw new InvalidOperationException(
                $"Node {node} cannot be referenced by '{name}' because it is not constrained node.");
    }

    public static void SetIfElse(this GraphBuilder b, int[] conditions, int[] expressions, int resultId) {
        var result = b.GetOrCreateNode(resultId);
        foreach (var exprId in expressions)
        {
            var expr = b.GetOrCreateNode(exprId);
            expr.AddAncestor(result);
        }

        foreach (var condId in conditions)
            b.SetOrCreatePrimitive(condId, StatePrimitive.Bool);
    }

    public static void SetConst(this GraphBuilder b, int id, StatePrimitive type)
        => b.SetOrCreatePrimitive(id, type);

    public static void SetIntConst(this GraphBuilder b, int id, StatePrimitive desc)
        => b.SetGenericConst(id,
            desc: desc,
            anc: StatePrimitive.Real,
            preferred: StatePrimitive.Real);

    public static void SetGenericConst(this GraphBuilder b, int id,
        StatePrimitive desc = null, StatePrimitive anc = null, StatePrimitive preferred = null) {
        var node = b.GetOrCreateNode(id);
        if (node.State is ConstraintsState constrains)
        {
            constrains.AddAncestor(anc);
            constrains.AddDescendant(desc);
            constrains.Preferred = preferred;
        }
        else
            throw new InvalidOperationException();
    }

    public static void SetArrayConst(this GraphBuilder b, int id, StatePrimitive elementType) {
        var eNode = b.CreateVarType(elementType);
        var node = b.GetOrCreateNode(id);
        if (node.State is ConstraintsState c)
        {
            var arrayOf = StateArray.Of(eNode);
            if (c.CanBeConvertedTo(arrayOf))
            {
                node.State = arrayOf;
                return;
            }
        }
        else if (node.State is StateArray a)
        {
            if (a.Element.Equals(elementType))
                return;
        }

        throw new InvalidOperationException();
    }

    public static void SetStructConst(this GraphBuilder b, int id, StateStruct @struct) {
        if (!@struct.IsSolved)
            throw new InvalidOperationException();
        b.GetOrCreateStructNode(id, @struct);
    }

    public static void CreateLambda(this GraphBuilder b, int returnId, int lambdaId, params string[] varNames) {
        var args = b.GetNamedNodes(varNames);
        var ret = b.GetOrCreateNode(returnId);
        b.SetOrCreateLambda(lambdaId, args, ret);
    }

    public static void CreateLambda(this GraphBuilder b, int returnId, int lambdaId, ITypeState returnType,
        params string[] varNames) {
        var args = b.GetNamedNodes(varNames);
        var exprId = b.GetOrCreateNode(returnId);
        var returnTypeNode = b.CreateVarType(returnType);
        exprId.AddAncestor(returnTypeNode);
        //expr<=returnType<= ...
        b.SetOrCreateLambda(lambdaId, args, returnTypeNode);
    }

    public static StateRefTo SetStrictArrayInit(this GraphBuilder b, int resultIds, params int[] elementIds) {
        var elementType = b.CreateVarType();
        b.GetOrCreateArrayNode(resultIds, elementType);

        foreach (var id in elementIds)
        {
            elementType.BecomeReferenceFor(b.GetOrCreateNode(id));
            elementType.IsMemberOfAnything = true;
        }

        return new StateRefTo(elementType);
    }

    public static void SetSoftArrayInit(this GraphBuilder b, int resultIds, params int[] elementIds) {
        var elementType = b.CreateVarType();
        b.GetOrCreateArrayNode(resultIds, elementType);
        foreach (var id in elementIds)
        {
            b.GetOrCreateNode(id).AddAncestor(elementType);
            elementType.IsMemberOfAnything = true;
        }
    }

    public static void SetFieldAccess(this GraphBuilder b, int structNodeId, int opId, string fieldName) {
        var node = b.GetOrCreateStructNode(structNodeId, new StateStruct())
            .GetNonReference();

        var state = (StateStruct)node.State;
        var memberNode = state.GetFieldOrNull(fieldName);
        if (memberNode == null)
        {
            memberNode = b.CreateVarType();
            node.State = state.With(fieldName, memberNode);
        }

        b.MergeOrSetNode(opId, new StateRefTo(memberNode));
    }

    public static void SetStructInit(this GraphBuilder b, string[] fieldNames, int[] fieldExpressionIds, int id) {
        var fields = new Dictionary<string, TicNode>(fieldNames.Length);
        for (int i = 0; i < fieldNames.Length; i++)
            fields.Add(fieldNames[i], b.GetOrCreateNode(fieldExpressionIds[i]));

        b.GetOrCreateStructNode(id, new StateStruct(fields, false));
    }

    public static void SetCompareChain(this GraphBuilder b, int nodeOrderNumber, StateRefTo[] generics, int[] ids) {
        for (int i = 0; i < generics.Length; i++)
        {
            var generic = generics[i];
            b.SetCallArgument(generic, ids[i]);
            b.SetCallArgument(generic, ids[i + 1]);
        }

        b.SetOrCreatePrimitive(nodeOrderNumber, StatePrimitive.Bool);
    }
}
