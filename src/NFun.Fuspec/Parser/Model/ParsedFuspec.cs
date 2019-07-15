using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Nfun.Fuspec.Parser.FuspecParserErrors;

namespace Nfun.Fuspec.Parser.Model
{
    public class ParsedFuspec
    {
        public FuspecTestCase[] FuspecTestCases { get; }

        public ParsedFuspec(FuspecTestCases fuspecTests)
        {
            if (fuspecTests.HasErrors)
                throw new FuspecParserException( fuspecTests.Errors);
            FuspecTestCases = fuspecTests.TestCases;
        }
    }
}