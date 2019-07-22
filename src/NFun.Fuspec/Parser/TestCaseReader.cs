using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Nfun.Fuspec.Parser.FuspecParserErrors;
using Nfun.Fuspec.Parser.Model;
using NFun.Tokenization;
using NFun.Types;
using ParcerV1;
using static Nfun.Fuspec.Parser.FuspecParserHelper;

namespace Nfun.Fuspec.Parser
{
    public class TestCasesReader
    {
        private List<string> _listOfString = new List<string>();
        private TestCaseBuilder _testCaseBuilder;
        private List<FuspecTestCase> _fuspecTestCases;
        private int _index;
        private bool _isReading;
        private List<string> _script= new List<string>();
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
                state = ReadNext(lineStr, state);
                _index++;
            }

            if (_isReading)
                AddLast(state); 
              
            return _errors.Any() 
                  ? new FuspecTestCases(_errors.ToArray()) 
                  : new FuspecTestCases(_fuspecTestCases.ToArray());
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
                case TestCaseParseState.ReadingParamsIn:
                    return ReadParamsIn(str);
                case TestCaseParseState.ReadingParamsOut:
                    return ReadParamsOut(str);
                case TestCaseParseState.ReadingValues:
                    return ReadValues(str);
                default:
                    return WriteError(new FuspecParserError(FuspecErrorType.Unknown, _index));
            }
        }

        private TestCaseParseState FindOpeningString(string str)
        {
            if (str.Trim() == "")
                return TestCaseParseState.FindingOpeningString;
            _isReading = false;
            
            _testCaseBuilder = new TestCaseBuilder();
            if (!IsSeparatingLine(str, '*'))
                return WriteError(new FuspecParserError(FuspecErrorType.OpeningStringMissed, _index));

            _isReading = true;
            _script=new List<string>();
            return TestCaseParseState.ReadingName;
        }
        
        private TestCaseParseState FindName(string str)
        {
            if (str.Trim() == "")
                return TestCaseParseState.ReadingName;
            var name = FindKeyWord("| TEST", str);
            if (name == null || name.Trim()=="")
                return WriteError(new FuspecParserError(FuspecErrorType.NamedMissed, _index));
            _testCaseBuilder.Name =name.Trim();
            return TestCaseParseState.ReadingTags;
        }

        private TestCaseParseState FindTags(string str)
        {
            if (str.Trim()== "")
                return TestCaseParseState.ReadingTags;
            if (IsSeparatingLine(str, '*'))
                return TestCaseParseState.ReadingParamsIn;
            var tags = FindKeyWord("| TAGS", str);
            if (tags == null )
                return WriteError(new FuspecParserError(FuspecErrorType.EndingHeadMissed,_index));

            if (tags.Trim() == "")
                return TestCaseParseState.ReadingTags;

            var splittedTags = tags.Split(',');
            foreach (var tag in splittedTags)
                if (tag.Trim() != "")
                    _testCaseBuilder.Tags.Add(tag.Trim());
            return TestCaseParseState.ReadingTags;
        }
        
        private TestCaseParseState ReadParamsIn(string str)
        {
            if (str.Trim() == "")
            {
                _script.Add(str);
                return TestCaseParseState.ReadingParamsIn;
            }
            var paramInString = FindKeyWord("| in", str);
            if (paramInString == null)
                return ReadParamsOut(str);
            
            if (paramInString.Trim() == "")
                return WriteError(new FuspecParserError(FuspecErrorType.ParamInMissed, _index));
           
            try
            {
                _testCaseBuilder.ParamsIn = GetParam(paramInString);
            }
            catch (Exception e)
            {
                return WriteError(new FuspecParserError(FuspecErrorType.WrongParamType, _index));
            }
            
            if (_testCaseBuilder.ParamsIn == null)
                WriteError(new FuspecParserError(FuspecErrorType.WrongParamType, _index));
                   
            return !(_testCaseBuilder.ParamsIn ?? throw new InvalidOperationException()).Any() 
                ? WriteError(new FuspecParserError(FuspecErrorType.ParamInMissed, _index)) 
                : TestCaseParseState.ReadingParamsOut;
        }

        private TestCaseParseState ReadParamsOut(string str)
        {
            if (str.Trim() == "")
                return TestCaseParseState.ReadingParamsOut;
            if (IsSeparatingLine(str, '-'))
                return TestCaseParseState.ReadingBody;

            var paramOutString = FindKeyWord("| out", str);
            if (paramOutString == null)
            {
                if (_testCaseBuilder.ParamsIn.Any())
                    return WriteError(new FuspecParserError(FuspecErrorType.SeparatedStringMissed,_index));
                return ReadBody(str);
            }
            
            if (paramOutString=="" || paramOutString.Substring(0,1)!=" ")
                return WriteError(new FuspecParserError(FuspecErrorType.ParamOutMissed, _index));
            
            try
            {
                _testCaseBuilder.ParamsOut = GetParam(paramOutString);
            }
            catch (Exception e)
            {
                return WriteError(new FuspecParserError(FuspecErrorType.WrongParamType, _index));
            }
            
            if (_testCaseBuilder.ParamsOut == null)
                WriteError(new FuspecParserError(FuspecErrorType.WrongParamType, _index));
            
            return TestCaseParseState.ReadingParamsOut;
        }

        private TestCaseParseState ReadBody(string str)
        {
            if (str.Trim() == "")
            {
                _script.Add(str);
                return TestCaseParseState.ReadingBody;
            }

            if (FindKeyWord("|***", str) != null || FindKeyWord("|---",str)!=null)
            {
                if (IsSeparatingLine(str, '-') || IsSeparatingLine(str, '*'))
                    return ReadValues(str);
                return WriteError(new FuspecParserError(FuspecErrorType.OpeningStringMissed, _index));
            }
            _script.Add(str);
            return TestCaseParseState.ReadingBody;
        }

        private TestCaseParseState ReadValues(string str)
        {
            if (str.Trim() == "")
                return TestCaseParseState.ReadingValues;

            return IsSeparatingLine(str,'*') 
                ? AddTestCase(str) 
                : TestCaseParseState.ReadingValues;
        }

        private TestCaseParseState AddTestCase(string str)
        {
            if (!_script.Any())
                return WriteError(new FuspecParserError(FuspecErrorType.ScriptMissed, _index));
            BuildScript();
            _fuspecTestCases.Add(_testCaseBuilder.Build());
            return FindOpeningString(str);
        }

        private void AddLast(TestCaseParseState state)
        {
            if ((state == TestCaseParseState.ReadingName) || (state == TestCaseParseState.ReadingTags))
                WriteError(new FuspecParserError(FuspecErrorType.NoEndingTestCase, _index));
            if (!_script.Any() && ((state == TestCaseParseState.ReadingParamsOut ||
                                   state == TestCaseParseState.ReadingParamsIn)))
                WriteError(new FuspecParserError(FuspecErrorType.NoEndingTestCase, _index));
            else
            {
                if (!_script.Any())
                    WriteError(new FuspecParserError(FuspecErrorType.NoEndingTestCase, _index));
                else
                {
                    BuildScript();
                    _fuspecTestCases.Add(_testCaseBuilder.Build());
                }
            }
        }

        private void BuildScript()
        {
            _testCaseBuilder.Script = "";
            foreach (var strScript in _script)
                _testCaseBuilder.Script = _testCaseBuilder.Script + strScript + "\r";
            _testCaseBuilder.Script =
                _testCaseBuilder.Script.Substring(0, _testCaseBuilder.Script.Length - 1);
        }

        private TestCaseParseState WriteError(FuspecParserError fuspecParserError)
        {
            _errors.Add(fuspecParserError);
            _isReading = false;
            return TestCaseParseState.FindingOpeningString;
        }
    }
}