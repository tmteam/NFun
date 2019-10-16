namespace NFun.TypeInference.Solving
{
    public interface INodeBehavior
    {
        TiType MakeType(int maxDepth);
        INodeBehavior SetLimit(TiType newLimit);
        INodeBehavior SetStrict(TiType newType);
        INodeBehavior SetLca(SolvingNode[] otherNodes);
        INodeBehavior SetReference(SolvingNode otherNode);
        INodeBehavior SetGeneric(SolvingNode otherGeneric);

        string ToSmartString(int maxDepth = 10);
        INodeBehavior Optimize(out bool hasChanged);
        FitResult CanBeConvertedTo(TiType candidateType, int maxDepth);
        FitResult CanBeConvertedFrom(TiType candidateType, int maxDepth);
    }
}