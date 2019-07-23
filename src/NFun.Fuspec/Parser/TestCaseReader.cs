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
        private int _index = 0;
        private bool _isReading = false;
        private List<string> _script= new List<string>();
        private SetCheckKit _setCheckKit = new SetCheckKit();
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
            if (IsSeparatingLine(str, '*'))
            {
                _isReading = true;
                _script=new List<string>();
                _setCheckKit = new SetCheckKit();
                return TestCaseParseState.ReadingName;
            }
            return WriteError(new FuspecParserError(FuspecErrorType.OpeningStringMissed, _index));
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
            
            if (tags.Trim() != "")
                _testCaseBuilder.Tags = SplitWithTrim(tags, ',');
               
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
            
            if (paramInString.Trim()=="" || paramInString.Substring(0,1)!=" ")
                return WriteError(new FuspecParserError(FuspecErrorType.ParamInMissed, _index));
           
            try
            {
                _testCaseBuilder.ParamsIn = GetPAram(paramInString);
            }
            catch (Exception e)
            {
                return WriteError(new FuspecParserError(FuspecErrorType.WrongParamType, _index));
            }
            
            if (_testCaseBuilder.ParamsIn == null)
                return WriteError(new FuspecParserError(FuspecErrorType.WrongParamType, _index));
                   
            if (!_testCaseBuilder.ParamsIn.Any())
                return WriteError(new FuspecParserError(FuspecErrorType.ParamInMissed, _index));
            
            return TestCaseParseState.ReadingParamsOut;
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
            
            if (paramOutString.Trim()=="" || paramOutString.Substring(0,1)!=" ")
                return WriteError(new FuspecParserError(FuspecErrorType.ParamOutMissed, _index));
            
            try
            {
            _testCaseBuilder.ParamsOut = GetPAram(paramOutString);
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
                if (IsSeparatingLine(str, '-') )
                    return TestCaseParseState.ReadingValues;
                if ( IsSeparatingLine(str, '*'))
                    return AddTestCase(str);
                return WriteError(new FuspecParserError(FuspecErrorType.OpeningStringMissed, _index));
            }
            _script.Add(str);
            return TestCaseParseState.ReadingBody;
        }

        private TestCaseParseState ReadValues(string str)
        {
            if (str.Trim() == "")
                return TestCaseParseState.ReadingValues;
            if (IsSeparatingLine(str, '-'))
            {
                if (_setCheckKit.CheckKit.Any() || _setCheckKit.SetKit.Any())
                {
                    _testCaseBuilder.SetCheckKits.Add(_setCheckKit);
                    _setCheckKit = new SetCheckKit();
                }
                return TestCaseParseState.ReadingValues;
            }

            if (IsSeparatingLine(str, '*'))
            {
                if (_setCheckKit.CheckKit.Any() || _setCheckKit.SetKit.Any())
                {
                    _testCaseBuilder.SetCheckKits.Add(_setCheckKit);
                    _setCheckKit = new SetCheckKit();
                };
                return AddTestCase(str);
            }
            
            if (_setCheckKit.CheckKit.Any())
                return WriteError(new FuspecParserError(FuspecErrorType.ExpectedSeparatedLine, _index));

            var setString = FindKeyWord("| set", str);
            var checkString = FindKeyWord("| check", str); 
            if ((setString==null & checkString==null))
                return WriteError(new FuspecParserError(FuspecErrorType.WrongSetCheckKit, _index));

            if (setString!= null)
            {
                if( setString.Substring(0,1)!=" ")
                    return WriteError(new FuspecParserError(FuspecErrorType.WrongSetCheckKit, _index));
                if (setString.Trim() == "")
                    return WriteError(new FuspecParserError(FuspecErrorType.SetKitMissed, _index));
                
                _setCheckKit.AddSetKit(SplitWithTrim(setString, ';'));

                if (!_setCheckKit.SetKit.Any())
                    return WriteError(new FuspecParserError(FuspecErrorType.SetKitMissed, _index));
                
              return TestCaseParseState.ReadingValues;
            }

         
            
                if (_setCheckKit.CheckKit.Any())
                    return WriteError(new FuspecParserError(FuspecErrorType.ExpectedSeparatedLine, _index));
                if (checkString.Substring(0, 1) != " ")
                    return WriteError(new FuspecParserError(FuspecErrorType.WrongSetCheckKit, _index));
                if (checkString.Trim() == "")
                    return WriteError(new FuspecParserError(FuspecErrorType.CheckKitMissed, _index));
                _setCheckKit.AddCheckKit(SplitWithTrim(checkString, ';'));

                if (!_setCheckKit.CheckKit.Any())
                    return WriteError(new FuspecParserError(FuspecErrorType.CheckKitMissed, _index));

                return TestCaseParseState.ReadingValues;
            
            /*             

            if (setString == null)
            {
                var CheckString = FindKeyWord("| check", str);
                if (CheckString == null || CheckString.Substring(0, 1) != " ")
                    return WriteError(new FuspecParserError(FuspecErrorType.WrongSetCheckKit, _index));
                if (CheckString.Trim() == "")
                    return WriteError(new FuspecParserError(FuspecErrorType.CheckKitMissed, _index));
                if (_setCheckKit.CheckKit.Any())
                          return WriteError(new FuspecParserError(FuspecErrorType.NotAllowSecondCheckSet, _index));
                    
                _setCheckKit.AddCheckKit(SplitWithTrim(CheckString, ';'));
                
                if (!_setCheckKit.CheckKit.Any())
                    return WriteError(new FuspecParserError(FuspecErrorType.CheckKitMissed, _index));
               
                return TestCaseParseState.ReadingValues;
            }

            if (setString.Trim() == "" || setString.Substring(0, 1) != " ")
                return WriteError(new FuspecParserError(FuspecErrorType.SetKitMissed, _index));

            _setCheckKit.AddSetKit(SplitWithTrim(setString, ';'));

            if (!_setCheckKit.SetKit.Any())
                 return WriteError(new FuspecParserError(FuspecErrorType.SetKitMissed, _index));

           // _testCaseBuilder.SetCheckKits.Add(_setCheckKit);
            //_setCheckKit = new SetCheckKit();
            return TestCaseParseState.ReadingValues;
*/
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
                    if (_setCheckKit.SetKit.Any() || _setCheckKit.CheckKit.Any())
                        _testCaseBuilder.SetCheckKits.Add(_setCheckKit);
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

        private void BuildSetCheckKits()
        {
            _testCaseBuilder.SetCheckKits.Add(_setCheckKit);
            _setCheckKit = new SetCheckKit();
            
        }

        private TestCaseParseState WriteError(FuspecParserError fuspecParserError)
        {
            _errors.Add(fuspecParserError);
            _isReading = false;
            return TestCaseParseState.FindingOpeningString;
        }
    }
}