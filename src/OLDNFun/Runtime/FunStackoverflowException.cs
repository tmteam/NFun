using System;

namespace NFun.Runtime
{
    public class FunStackoverflowException: Exception
    {
        public FunStackoverflowException(string message): base(message)
        {
            
        }
    }
}