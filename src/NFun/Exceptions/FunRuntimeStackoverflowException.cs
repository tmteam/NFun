using System;

namespace NFun.Exceptions
{
    public class FunRuntimeStackoverflowException: Exception
    {
        public FunRuntimeStackoverflowException(string message): base(message)
        {
            
        }
    }
}