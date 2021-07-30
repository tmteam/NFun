using System;

namespace NFun.Exceptions
{
    public class FunnyRuntimeException:Exception
    {
        public FunnyRuntimeException(string message, Exception e): base(message, e)
        {
            
        }
        public FunnyRuntimeException(string message): base(message)
        {
            
        }
    }
}