using System;

namespace NFun.Exceptions
{
    /// <summary>
    /// The thing that should not be...
    /// </summary>
    public class ImpossibleException: Exception
    {
        public ImpossibleException(string message): base(message)
        {
            
        }
    }
}