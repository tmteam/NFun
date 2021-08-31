using System;

namespace NFun.Exceptions
{
    /// <summary>
    /// The thing that should not be...
    /// </summary>
    internal class NFunImpossibleException : Exception
    {
        public NFunImpossibleException(string message) : base(message)
        {
        }
    }
}