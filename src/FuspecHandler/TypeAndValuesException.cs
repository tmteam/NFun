using System;
using System.Collections.Generic;
using Nfun.Fuspec.Parser.Model;
using ParcerV1;

namespace FuspecHandler
{
    public class TypeAndValuesException: Exception
    {
        private List<string> messages;
        public string[] Messages=>messages.ToArray();

        public TypeAndValuesException() => messages= new List<string>();

        public void AddErrorMessage(IEnumerable<string> mes) => messages.AddRange(mes);
    }
}