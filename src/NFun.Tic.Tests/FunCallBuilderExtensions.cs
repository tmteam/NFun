using NFun.Tic.SolvingStates;

namespace NFun.Tic {

public static class FunCallBuilderExtensions {
    public static void SetCall(this GraphBuilder builder, StatePrimitive ofTheCall, params int[] argumentsThenResult) {
        var types = new ITicNodeState[argumentsThenResult.Length];
        for (int i = 0; i < types.Length; i++) types[i] = ofTheCall;
        builder.SetCall(types, argumentsThenResult);
    }

    public static StateRefTo SetEquality(this GraphBuilder builder, int leftId, int rightId, int resultId) {
        var t = builder.InitializeVarNode();

        builder.SetCall(
            argThenReturnTypes: new ITicNodeState[] { t, t, StatePrimitive.Bool },
            argThenReturnIds: new[] { leftId, rightId, resultId });
        return t;
    }

    public static void SetComparable(this GraphBuilder builder, int leftId, int rightId, int resultId) {
        var t = builder.InitializeVarNode(isComparable: true);

        builder.SetCall(
            argThenReturnTypes: new ITicNodeState[] { t, t, StatePrimitive.Bool },
            argThenReturnIds: new[] { leftId, rightId, resultId });
    }

    public static void SetBitwiseInvert(this GraphBuilder builder, int argId, int resultId) {
        var t = builder.InitializeVarNode(StatePrimitive.U8, StatePrimitive.I96);
        builder.SetCall(t, new[] { argId, resultId });
    }

    public static void SetBitwise(this GraphBuilder builder, int leftId, int rightId, int resultId) {
        var t = builder.InitializeVarNode(StatePrimitive.U8, StatePrimitive.I96);
        builder.SetCall(t, new[] { leftId, rightId, resultId });
    }

    public static void SetBitShift(this GraphBuilder builder, int leftId, int rightId, int resultId) {
        var t = builder.InitializeVarNode(StatePrimitive.U24, StatePrimitive.I96);
        builder.SetCall(
            argThenReturnTypes: new ITicNodeState[] { t, StatePrimitive.I48, t },
            argThenReturnIds: new[] { leftId, rightId, resultId });
    }

    public static void SetBoolCall(this GraphBuilder builder, int leftId, int rightId, int resultId) {
        builder.SetCall(StatePrimitive.Bool, new[] { leftId, rightId, resultId });
    }

    public static void SetArith(this GraphBuilder builder, int leftId, int rightId, int resultId) {
        var t = builder.InitializeVarNode(StatePrimitive.U24, StatePrimitive.Real);
        builder.SetCall(t, new[] { leftId, rightId, resultId });
    }

    public static void SetNegateCall(this GraphBuilder builder, int argId, int resultId) {
        var t = builder.InitializeVarNode(StatePrimitive.I16, StatePrimitive.Real);
        builder.SetCall(t, new[] { argId, resultId });
    }


    public static void SetArrGetCall(this GraphBuilder builder, int arrArgId, int indexArgId, int resId) {
        var varNode = builder.InitializeVarNode();
        builder.SetCall(
            new ITicNodeState[] { StateArray.Of(varNode), StatePrimitive.I32, varNode },
            new[] { arrArgId, indexArgId, resId });
    }

    public static void SetConcatCall(this GraphBuilder builder, int firstId, int secondId, int resultId) {
        var varNode = builder.InitializeVarNode();

        builder.SetCall(new ITicNodeState[] {
            StateArray.Of(varNode), StateArray.Of(varNode), StateArray.Of(varNode)
        }, new[] { firstId, secondId, resultId });
    }

    public static void SetSumCall(this GraphBuilder builder, int argId, int resultId) {
        var varNode = builder.InitializeVarNode(StatePrimitive.U24, StatePrimitive.Real);

        builder.SetCall(new ITicNodeState[] { StateArray.Of(varNode), varNode }, new[] { argId, resultId });
    }

    public static void SetIsAny(this GraphBuilder builder, int arrId, int funId, int resultId) {
        var inNode = builder.InitializeVarNode();
        if (inNode != null)
            builder.SetCall(new ITicNodeState[] {
                StateArray.Of(inNode),
                StateFun.Of(returnType: StatePrimitive.Bool, argType: inNode),
                StatePrimitive.Bool
            }, new[] { arrId, funId, resultId });
    }

    public static void SetGetFirst(this GraphBuilder builder, int arrId, int funId, int resultId) {
        var inNode = builder.InitializeVarNode();
        if (inNode != null)
            builder.SetCall(new ITicNodeState[] {
                StateArray.Of(inNode),
                StateFun.Of(returnType: StatePrimitive.Bool, argType: inNode),
                inNode
            }, new[] { arrId, funId, resultId });
    }

    public static void SetMap(this GraphBuilder builder, int arrId, int funId, int resultId) {
        var inNode = builder.InitializeVarNode();
        var outNode = builder.InitializeVarNode();
        builder.SetCall(new ITicNodeState[] {
            StateArray.Of(inNode), StateFun.Of(
                returnType: outNode,
                argType: inNode),
            StateArray.Of(outNode)
        }, new[] { arrId, funId, resultId });
    }

    public static void SetfoldCall(this GraphBuilder graph, int arrId, int funId, int returnId) {
        var generic = graph.InitializeVarNode();

        graph.SetCall(new ITicNodeState[] {
            StateArray.Of(generic),
            StateFun.Of(new[] { generic, generic }, generic),
            generic
        }, new[] { arrId, funId, returnId });
    }

    public static void SetSizeOfArrayCall(this GraphBuilder graph, int argId, int resId) {
        var tOfCount = graph.InitializeVarNode();
        //count
        graph.SetCall(new ITicNodeState[] { StateArray.Of(tOfCount), StatePrimitive.I32 }, new[] { argId, resId });
    }

    public static void SetRangeCall(this GraphBuilder graph, int fromId, int toId, int resId) {
        var generic = graph.InitializeVarNode(anc: StatePrimitive.I48);
        //range
        graph.SetCall(new ITicNodeState[] { generic, generic, StateArray.Of(generic) }, new[] { fromId, toId, resId });
    }

    public static void SetfoldCall(this GraphBuilder graph, int arrId, int defId, int funId, int resId) {
        var tRes = graph.InitializeVarNode();
        var tArg = graph.InitializeVarNode();
        //fold call   fold( T[], G, (G,T)->G )->G 
        graph.SetCall(new ITicNodeState[] {
                StateArray.Of(tArg),
                tRes,
                StateFun.Of(new ITicNodeState[] { tRes, tArg }, tRes),
                tRes
            },
            new[] { arrId, defId, funId, resId });
    }

    public static void SetFoldCall(this GraphBuilder graph, int arrId, int funId, int returnId) {
        var inT = graph.InitializeVarNode();
        var outT = graph.InitializeVarNode();

        graph.SetCall(new ITicNodeState[] {
            StateArray.Of(inT),
            StateFun.Of(new[] { outT, inT }, outT),
            outT
        }, new[] { arrId, funId, returnId });
    }

    public static void SetReverse(this GraphBuilder graph, int arrId, int resultId) {
        var t = graph.InitializeVarNode();
        graph.SetCall(new ITicNodeState[] { StateArray.Of(t), StateArray.Of(t) }, new[] { arrId, resultId });
    }
}

}