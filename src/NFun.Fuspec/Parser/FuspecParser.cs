using System;
using System.IO;
using Nfun.Fuspec.Parser.Model;

namespace Nfun.Fuspec.Parser
{
    public static class FuspecParser
    {
    	//todo cr : Is there only one public method? 
    	//If it is - may be it is better to unite FuspecParser 
    	//and fuspec parser helper?
    	
    	//todo cr: public method needs ///summary comments
        public static FuspecTestCase[] Read(StreamReader streamReader)
        {
        	//todo cr : too complex expression. Split it
            return new ParsedFuspec(new TestCasesReader(streamReader).Read())
                .FuspecTestCases;
        }
    }
}