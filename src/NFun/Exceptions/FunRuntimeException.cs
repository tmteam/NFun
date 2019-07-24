using System;

namespace NFun.BuiltInFunctions
{
    public class FunRuntimeException:Exception
    {
        public FunRuntimeException(string message, Exception e): base(message, e)
        {
            
        }
        public FunRuntimeException(string message): base(message)
        {
            
        }
    }
}