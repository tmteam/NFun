using System;

namespace NFun.Jet
{
    public class JetParseException : Exception
    {
        public JetParseException(string notFound): base(notFound)
        {
        }
    }
}