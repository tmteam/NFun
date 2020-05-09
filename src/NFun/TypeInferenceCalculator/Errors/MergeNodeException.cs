using System;
using System.Collections.Generic;
using System.Text;

namespace NFun.TypeInferenceCalculator.Errors
{
    public class MergeNodeException: TicException
    {
        public MergeNodeException( string message) : base(message)
        {
        }
    }
}
