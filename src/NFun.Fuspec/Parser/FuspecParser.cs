using System;
using System.Collections.Generic;
using System.IO;
using Nfun.Fuspec.Parser.Model;

namespace Nfun.Fuspec.Parser
{
    public static class FuspecParser
    {
	    private static List<string> _listOfString;
	    
	    //todo cr : Is there only one public method? 
    	//If it is - may be it is better to unite FuspecParser 
    	//and fuspec parser helper?
        
        //todo cr: answer
        // FuspecParser is the one open class. It is the main class. 
        // FuspecparserHelper is an inner class.
    	
    	//todo cr: public method needs ///summary comments
        /// <summary>
        /// Read streamReader with Fuspec tests and return array of FuspecTestCase
        /// </summary>
        /// <param name="streamReader"></param>
        /// <returns></returns>
        public static FuspecTestCase[] Read(StreamReader streamReader)
        {
        	//todo cr : too complex expression. Split it
            string line;
            while ((line = streamReader.ReadLine()) != null)
	            _listOfString.Add(line);

            var inputText = InputText.Read(streamReader);
            
            var fuspectests = new TestCasesReader().Read(inputText);
            return new ParsedFuspec(fuspectests).FuspecTestCases;
        }
    }
}