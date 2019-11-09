using System;
using System.Collections.Generic;
using Nfun.Fuspec.Parser.Model;
using ParcerV1;

namespace FuspecHandler
{
    public class OutputInputException: Exception
    {
        private List<string> messages;
        public string[] Messages=>messages.ToArray();

        public OutputInputException() => messages= new List<string>();

        public void AddErrorMessage(string mes) => messages.Add(mes);
    }
}