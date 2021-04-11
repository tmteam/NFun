using System;

namespace NFun.FluentApi
{
    public class FunInvalidUsageTODOException : Exception
    {
        public FunInvalidUsageTODOException():base("TODO")
        {
            
        }

        public FunInvalidUsageTODOException(string message):base(message)
        {
        }
    }
}