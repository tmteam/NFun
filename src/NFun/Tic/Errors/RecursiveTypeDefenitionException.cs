using NFun.TypeInferenceCalculator.Errors;

namespace NFun.Tic.Errors
{
    public class RecursiveTypeDefenitionException : TicException
    {
        public string[] NodeNames { get; }
        public int[] NodeIds { get; }

        public RecursiveTypeDefenitionException(string[] nodeNames, int[] nodeIds): base($"Recursive type defenition {string.Join("->", nodeNames)}")
        {
            NodeNames = nodeNames;
            NodeIds = nodeIds;
        }
    }
}