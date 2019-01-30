using System.Linq;

namespace Funny
{
    public class InterpritationResults
    {
        public InterpritationResults(Runtime runtime, InterpritationError[] interpritationErrors)
        {
            Runtime = runtime;
            InterpritationErrors = interpritationErrors;
        }

        public Runtime Runtime { get; }
        public InterpritationError[] InterpritationErrors { get; }
        public bool IsSuccesfully => InterpritationErrors.Any();
    }
}