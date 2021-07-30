using System;

namespace NFun.Exceptions
{
    /// <summary>
    /// The thing that should not be...
    /// </summary>
    internal class NfunImpossibleException: Exception
    {
        public NfunImpossibleException(string message): base(message)
        {
            
        }
    }
}