using System;

namespace Funny.Runtime
{
    public class FunStackoverflowException: Exception
    {
        public FunStackoverflowException(string message): base(message)
        {
            
        }
    }
}