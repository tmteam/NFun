using System;

namespace Funny.Runtime
{
    public class FunRuntimeException:Exception
    {
        public FunRuntimeException(string message): base(message)
        {
            
        }
    }
}