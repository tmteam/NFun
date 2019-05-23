namespace NFun.HindleyMilner.Tyso
{
    public interface INodeBehavior
    {
        FType MakeType(int maxDepth);
        INodeBehavior SetLimit(FType newLimit);
        INodeBehavior SetStrict(FType newType);
        INodeBehavior SetLca(SolvingNode[] otherNodes);
        INodeBehavior SetReference(SolvingNode otherNode);
        INodeBehavior SetGeneric(SolvingNode otherGeneric);

        string ToSmartString(int maxDepth = 10);
        INodeBehavior Optimize(out bool o);
        ConvertResults CanBeConvertedTo(FType candidateType, int maxDepth);
    }
}