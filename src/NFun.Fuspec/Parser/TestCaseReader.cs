using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nfun.Fuspec.Parser.FuspecParserErrors;
using Nfun.Fuspec.Parser.Model;
using ParcerV1;
using static Nfun.Fuspec.Parser.FuspecParserHelper;

namespace Nfun.Fuspec.Parser
{
    public class TestCasesReader
    {
        private List<string> _listOfString = new List<string>();
        private TestCaseBuilder _testCaseBuilder;
        private List<FuspecTestCase> _fuspecTestCases;
        private int _index = 0;
        private bool _isReading = false;
        private List<FuspecParserError> _errors=new List<FuspecParserError>();
        
        public TestCasesReader(StreamReader streamReader)
        {
            string line;
            while ((line = streamReader.ReadLine()) != null)
                _listOfString.Add(line);
            _testCaseBuilder = new TestCaseBuilder();
            _fuspecTestCases = new List<FuspecTestCase>();
        }

        public FuspecTestCases Read()
        {
            var state = TestCaseParseState.FindingOpeningString;
            foreach (var lineStr in _listOfString)
            {
                if (lineStr.Trim()!="")
                    state = ReadNext(lineStr, state);
                _index++;
            }
              if (_isReading)
                AddLast(); 
              
              if (_errors.Any())
                  return new FuspecTestCases(_errors.ToArray());
            return new FuspecTestCases(_fuspecTestCases.ToArray());
        }
        
        private TestCaseParseState ReadNext(string str, TestCaseParseState testCaseParseState)
        {
            switch (testCaseParseState)
            {   case TestCaseParseState.FindingOpeningString:
                    return FindOpeningString(str);
                case TestCaseParseState.ReadingName :
                    return FindName(str);
                case TestCaseParseState.ReadingTags:
                    return FindTags(str);
                case TestCaseParseState.ReadingBody:
                    return ReadBody(str);
                case TestCaseParseState.ReadingParams:
                    return ReadParams(str);
                case TestCaseParseState.ReadingValues:
                    return ReadValues(str);
                default:
                    return WriteError(new FuspecParserError(FuspecErrorType.Unknown, _index));
            }
        }

        private TestCaseParseState FindOpeningString(string str)
        {
            _isReading = false;
            _testCaseBuilder = new TestCaseBuilder();
            if (IsSeparatingLine(str, '*'))
            {
                _isReading = true;
                return TestCaseParseState.ReadingName;
            }
            return WriteError(new FuspecParserError(FuspecErrorType.OpeningStringMissed, _index));
        }
        
        private TestCaseParseState FindName(string str)
        {
            var name = FindKeyWord("| TEST", str);
            if (name == null || name.Trim()=="")
                return WriteError(new FuspecParserError(FuspecErrorType.NamedMissed, _index));
            _testCaseBuilder.Name =name.Trim();
            return TestCaseParseState.ReadingTags;
        }

        private TestCaseParseState FindTags(string str)
        {
            if (IsSeparatingLine(str, '*'))
                return TestCaseParseState.ReadingParams;
            var tags = FindKeyWord("| TAGS", str);
            if (tags == null )
                return WriteError(new FuspecParserError(FuspecErrorType.EndingHeadMissed,_index));
            if (tags.Trim() != "")
            {
                var splittedTags = tags.Split(',');
                foreach (var tag in splittedTags)
                    if (tag.Trim() != "")
                        _testCaseBuilder.Tags.Add(tag.Trim());

            }
            return TestCaseParseState.ReadingTags;
        }

        private TestCaseParseState ReadParams(string str) => ReadBody(str);

        private TestCaseParseState ReadBody(string str)
        {
            var stringOfScript = FindKeyWord("  ", str);
            if (stringOfScript == null)
            {
                if (_testCaseBuilder.Script == "")
                    return WriteError(new FuspecParserError(FuspecErrorType.ScriptMissed, _index));
                if (IsSeparatingLine(str, '-') || IsSeparatingLine(str, '*'))
                    return ReadValues(str);
                return AddTestCase(str);
            }

            if (stringOfScript.Trim() != "")
            {
                if (_testCaseBuilder.Script == "")
                    _testCaseBuilder.Script = stringOfScript;
                else
                _testCaseBuilder.Script = string.Concat(_testCaseBuilder.Script, '\r' + stringOfScript);
            }
            return TestCaseParseState.ReadingBody;
        }

        private TestCaseParseState ReadValues(string str)
        {
            if (IsSeparatingLine(str,'*'))
                return AddTestCase(str);
            return TestCaseParseState.ReadingValues;
        }
      
        private TestCaseParseState AddTestCase(string str)
        {
            _fuspecTestCases.Add(_testCaseBuilder.Build());
            return FindOpeningString(str);
        }

        private void AddLast()
        {
            if (_testCaseBuilder.Name == null || _testCaseBuilder.Script == "")
                WriteError(new FuspecParserError(FuspecErrorType.NoEndingTestCase, _index));
            else
                _fuspecTestCases.Add(_testCaseBuilder.Build());
        }
        
        private TestCaseParseState WriteError(FuspecParserError fuspecParserError)
        {
            _errors.Add(fuspecParserError);
            _isReading = false;
            return TestCaseParseState.FindingOpeningString;
        }
    }
}