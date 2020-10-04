using System;
using System.Collections.Generic;

namespace FuspecHandler
{
    public class TypeAndValuesException: Exception
    {
        private readonly List<string> messages;
        public string[] Messages=>messages.ToArray();

        public TypeAndValuesException() => messages= new List<string>();

        public void AddErrorMessage(IEnumerable<string> mes) => messages.AddRange(mes);
    }
}