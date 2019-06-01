using System;
using System.IO;
using Nfun.Fuspec.Parser.Model;

namespace Nfun.Fuspec.Parser
{
    public class FuspecParser
    {
        public static FuspecTestCase[] Read(StreamReader streamReader)
        {
            return new ParsedFuspec(new TestCasesReader(streamReader).Read())
                .FuspecTestCases;
        }
    }
}