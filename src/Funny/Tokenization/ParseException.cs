using System;

namespace Funny.Tokenization
{
    public class ParseException : Exception
    {
        public ParseException(string message):base(message)
        {
            
        }
    }
}