using System.Linq;
using NFun.Tic.SolvingStates;

namespace NFun.Tic
{
    public static class FunCallBuilderExtensions
    {
        public static void SetCall(this GraphBuilder builder, Primitive ofTheCall, params int[] argumentsThenResult)
        {
            var types = argumentsThenResult.Select(s => (IState)ofTheCall).ToArray();
            builder.SetCall(types, argumentsThenResult);
        }

        public static void SetEquality(this GraphBuilder builder, int leftId, int rightId, int resultId)
        {
            var t = builder.InitializeVarNode();
            
            builder.SetCall(
                argThenReturnTypes: new IState []{t, t, Primitive.Bool},
                argThenReturnIds: new []{leftId, rightId, resultId});
        }
        
        public static void SetComparable(this GraphBuilder builder,int leftId, int rightId, int resultId)
        {
            var t = builder.InitializeVarNode(isComparable: true);

            builder.SetCall(
                argThenReturnTypes: new IState[] { t, t, Primitive.Bool },
                argThenReturnIds: new[] { leftId, rightId, resultId });
        }
        
        public static void SetBitwiseInvert(this GraphBuilder builder, int argId, int resultId)
        {
            var t = builder.InitializeVarNode(Primitive.U8, Primitive.I96);

            builder.SetCall(
                argThenReturnTypes: new IState[] { t, t },
                argThenReturnIds: new[] { argId, resultId});
        }

        public static void SetBitwise(this GraphBuilder builder, int leftId, int rightId, int resultId)
        {
            var t = builder.InitializeVarNode(Primitive.U8, Primitive.I96);

            builder.SetCall(
                argThenReturnTypes: new IState[] { t, t,t },
                argThenReturnIds: new[] { leftId,rightId, resultId });
        }

        public static void SetBitShift(this GraphBuilder builder, int leftId, int rightId, int resultId)
        {
            var t = builder.InitializeVarNode(Primitive.U24, Primitive.I96);

            builder.SetCall(
                argThenReturnTypes: new IState[] { t, Primitive.I48, t },
                argThenReturnIds: new[] { leftId, rightId, resultId });
        }

        public static void SetBoolCall(this GraphBuilder builder, int leftId, int rightId, int resultId)
        {

            builder.SetCall(
                argThenReturnTypes: new IState[] { Primitive.Bool, Primitive.Bool, Primitive.Bool},
                argThenReturnIds: new[] { leftId, rightId, resultId });
        }

        public static void SetArith(this GraphBuilder builder, int leftId, int rightId, int resultId)
        {
            var t = builder.InitializeVarNode(Primitive.U24, Primitive.Real);

            builder.SetCall(
                argThenReturnTypes: new IState[] { t, t, t },
                argThenReturnIds: new[] { leftId, rightId, resultId });
        }

        public static void SetNegateCall(this GraphBuilder builder,int argId, int resultId)
        {
            var t = builder.InitializeVarNode(Primitive.I16, Primitive.Real);

            builder.SetCall(
                argThenReturnTypes: new IState[] { t, t },
                argThenReturnIds: new[] { argId, resultId });
        }

        public static void SetArrGetCall(this GraphBuilder builder, int arrArgId, int indexArgId, int resId)
        {
            var varNode = builder.InitializeVarNode();
            builder.SetCall(
                new IState []{Array.Of(varNode), Primitive.I32, varNode },new []{arrArgId,indexArgId, resId});
        }
        
        public static void SetConcatCall(this GraphBuilder builder, int firstId, int secondId, int resultId)
        {
            var varNode = builder.InitializeVarNode();
            
            builder.SetCall(new IState[]
            {
                Array.Of(varNode),Array.Of(varNode),Array.Of(varNode),
            }, new []{firstId, secondId, resultId});

        }

        public static void SetSumCall(this GraphBuilder builder, int argId, int resultId)
        {
            var varNode = builder.InitializeVarNode(Primitive.U24, Primitive.Real);

            builder.SetCall(new IState[]{Array.Of(varNode), varNode}, new []{argId,resultId});
        }
        public static void SetIsAny(this GraphBuilder builder, int arrId, int funId, int resultId)
        {
            var inNode = builder.InitializeVarNode();
            if (inNode != null)
                builder.SetCall(new IState[]
                {
                    Array.Of(inNode),
                    Fun.Of(returnType: Primitive.Bool, argType: inNode),
                    Primitive.Bool,
                }, new[] {arrId, funId, resultId});
        }

        public static void SetGetFirst(this GraphBuilder builder, int arrId, int funId, int resultId)
        {
            var inNode = builder.InitializeVarNode();
            if (inNode != null)
                builder.SetCall(new IState[]
                {
                    Array.Of(inNode),
                    Fun.Of(returnType: Primitive.Bool, argType: inNode),
                    inNode,
                }, new[] { arrId, funId, resultId });
        }

        public static void SetMap(this GraphBuilder builder, int arrId, int funId, int resultId)
        {
            var inNode = builder.InitializeVarNode();
            var outNode = builder.InitializeVarNode();
            builder.SetCall(new IState[]{Array.Of(inNode), Fun.Of(
                returnType: outNode, 
                argType: inNode), 
                Array.Of(outNode)}, new []{arrId,funId, resultId});
        }
        public static void SetReduceCall(this GraphBuilder graph, int arrId, int funId, int returnId)
        {
            var generic = graph.InitializeVarNode();

            graph.SetCall(new IState[]
            {
                Array.Of(generic),
                Fun.Of(new[] {generic, generic}, generic),
                generic
            }, new[] { arrId, funId, returnId });
        }

        public static void SetFoldCall(this GraphBuilder graph, int arrId, int funId, int returnId)
        {
            var inT = graph.InitializeVarNode();
            var outT = graph.InitializeVarNode();

            graph.SetCall(new IState[]
            {
                Array.Of(inT),
                Fun.Of(new[] {outT,inT}, outT),
                outT
            }, new[] { arrId, funId, returnId });
        }

        public static void SetReverse(this GraphBuilder graph, int arrId, int resultId)
        {
            var t = graph.InitializeVarNode();
            graph.SetCall(new[] { Array.Of(t), Array.Of(t) }, new[] { arrId, resultId });
        }
    }
}
