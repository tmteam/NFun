using System;

namespace NFun.Exceptions
{
    public class FunInvalidUsageException : Exception
    {
        public static FunInvalidUsageException OutputTypeConstainsNoParameterlessCtor(Type type)
            => new($"Output type '{type.Name}' contains no parameterless constructor");
        
        private FunInvalidUsageException(string message):base(message)
        {
        }
    }
}