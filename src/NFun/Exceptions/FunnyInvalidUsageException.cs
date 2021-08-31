using System;

namespace NFun.Exceptions
{
    public class FunnyInvalidUsageException : Exception
    {
        public static FunnyInvalidUsageException OutputTypeConstainsNoParameterlessCtor(Type type)
            => new($"Output type '{type.Name}' contains no parameterless constructor");
        
        private FunnyInvalidUsageException(string message):base(message){}
    }
}