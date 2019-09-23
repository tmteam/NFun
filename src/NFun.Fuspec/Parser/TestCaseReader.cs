using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Nfun.Fuspec.Parser.FuspecParserErrors;
using Nfun.Fuspec.Parser.Model;
using NFun.ParseErrors;
using NFun.Tokenization;
using NFun.Types;
using ParcerV1;
using static Nfun.Fuspec.Parser.FuspecParserHelper;

[assembly: InternalsVisibleTo("Nfun.Fuspectests")]
 
namespace Nfun.Fuspec.Parser
{
    class TestCasesReader
    {
        private List<string> _listOfString = new List<string>();
        private TestCaseBuilder _testCaseBuilder;
        private List<FuspecTestCase> _fuspecTestCases;
        private int _index = 0;
        private bool _isReading = false;
        private List<string> _script= new List<string>();
        private List<SetCheckPair> _setCheckKits= new List<SetCheckPair>();
        private SetCheckPair _setCheckPair= new SetCheckPair();
        private List<FuspecParserError> _errors=new List<FuspecParserError>();
        
        internal TestCasesReader(StreamReader streamReader)
        {
            string line;
            while ((line = streamReader.ReadLine()) != null)
                _listOfString.Add(line);
            _testCaseBuilder = new TestCaseBuilder();
            _fuspecTestCases = new List<FuspecTestCase>();
            
        }

        internal FuspecTestCases Read()
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
                _setCheckKits = new List<SetCheckPair>();
                return TestCaseParseState.ReadingName;
            }
            return WriteError(new FuspecParserError(FuspecErrorType.OpeningStringMissed, _index));
        }
        
        private TestCaseParseState FindName(string str)
        {
            if (str.Trim() == "")
                return TestCaseParseState.ReadingName;
            var name = FindStringOrNullByKeyWord("| TEST", str);
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
            
            var tags = FindStringOrNullByKeyWord("| TAGS", str);
            
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
            var paramInString = FindStringOrNullByKeyWord("| in", str);
            if (paramInString == null)
                return ReadParamsOut(str);
            
            if (paramInString.Trim()=="" || paramInString.Substring(0,1)!=" ")
                return WriteError(new FuspecParserError(FuspecErrorType.ParamInMissed, _index));
           
            try
            {
                _testCaseBuilder.ParamsIn = GetInOutParam(paramInString);
            }
            catch (Exception e)
            {
                return WriteError(new FuspecParserError(FuspecErrorType.NFunMessage_ICantParseParamTypeString, _index));
            }
            
            if (_testCaseBuilder.ParamsIn == null)
                return WriteError(new FuspecParserError(FuspecErrorType.NFunMessage_ICantParseParamTypeString, _index));
                   
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

            var paramOutString = FindStringOrNullByKeyWord("| out", str);
            if (paramOutString == null)
            {
                if (_testCaseBuilder.ParamsIn.Any()||_testCaseBuilder.ParamsOut.Any())
                    return WriteError(new FuspecParserError(FuspecErrorType.SeparatedStringMissed,_index));
                return ReadBody(str);
            }
            
            if (paramOutString.Trim()=="" || paramOutString.Substring(0,1)!=" ")
                return WriteError(new FuspecParserError(FuspecErrorType.ParamOutMissed, _index));
            
            try
            {
            _testCaseBuilder.ParamsOut = GetInOutParam(paramOutString);
            }
            catch (Exception e)
            {
                return WriteError(new FuspecParserError(FuspecErrorType.NFunMessage_ICantParseParamTypeString, _index));
            }
            
            if (_testCaseBuilder.ParamsOut == null)
                WriteError(new FuspecParserError(FuspecErrorType.NFunMessage_ICantParseParamTypeString, _index));
            
            return TestCaseParseState.ReadingParamsOut;
        }

        private TestCaseParseState ReadBody(string str)
        {
            if (str.Trim() == "")
            {
                _script.Add(str);
                return TestCaseParseState.ReadingBody;
            }


            if (IsSeparatingLine(str, '-'))
            {
                if (!(_script.Any(x => x.Trim() != "")))
                    WriteError(new FuspecParserError(FuspecErrorType.ScriptMissed, _index));
                return TestCaseParseState.ReadingValues;
            }
            if (IsSeparatingLine(str, '*'))
                return AddTestCase(str);

            if (FindStringOrNullByKeyWord("|***", str) != null && !IsSeparatingLine(str, '*'))
                return WriteError(new FuspecParserError(FuspecErrorType.OpeningStringMissed, _index));


            if (FindStringOrNullByKeyWord("| set", str) != null || FindStringOrNullByKeyWord("| check", str) != null)
            {
                if ((!_script.Any(x => x.Trim() != "")))
                  WriteError(new FuspecParserError(FuspecErrorType.ScriptMissed, _index));
                WriteError(new FuspecParserError(FuspecErrorType.SeparatedStringMissed, _index));

            }
            
            _script.Add(str);
            return TestCaseParseState.ReadingBody;
        }

        private TestCaseParseState ReadValues(string str)
        {
            
            
            if (str.Trim() == "")
                return TestCaseParseState.ReadingValues;

            if (IsSeparatingLine(str, '*'))
                return AddTestCase(str);

            var setString = FindStringOrNullByKeyWord("| set", str);
            var checkString = FindStringOrNullByKeyWord("| check", str);

            if ((setString == null && checkString == null))
                return WriteError(new FuspecParserError(FuspecErrorType.ExpectedOpeningLine, _index));

            if (setString != null)
            {
                if (setString.Trim() == "")
                    return WriteError(new FuspecParserError(FuspecErrorType.SetKitMissed, _index));
                if (setString.Substring(0, 1) != " ")
                    return WriteError(new FuspecParserError(FuspecErrorType.WrongSetCheckKit, _index));
                

                //если была только сет строка перед текущей, то надо добавить SetCheckKit
                if (!_setCheckPair.Check.Any() && _setCheckPair.Set.Any())
                    _setCheckKits.Add(_setCheckPair);
                _setCheckPair = new SetCheckPair();
                try
                {
                    _setCheckPair.AddSet(GetValue(setString));
                }
                catch (FunParseException e)
                {
                   Console.WriteLine($"fpe {e.Code} {e.Message}");
                   return WriteError(new FuspecParserError(FuspecErrorType.NFunMessage_ICantParseValue, _index));
                }
                catch (Exception e)
                {
                    return WriteError(new FuspecParserError(FuspecErrorType.NFunMessage_ICantParseValue, _index));
                }
               
            }

            if (checkString != null)
            {
                if (checkString.Trim() == "")
                    return WriteError(new FuspecParserError(FuspecErrorType.CheckKitMissed, _index));
                if (checkString.Substring(0, 1) != " ")
                    return WriteError(new FuspecParserError(FuspecErrorType.WrongSetCheckKit, _index));
         
                try
                {
                    _setCheckPair.AddGet(GetValue(checkString));
                }
                catch (Exception e)
                {
                    return WriteError(new FuspecParserError(FuspecErrorType.NFunMessage_ICantParseValue, _index));
                }
                
                _setCheckKits.Add(_setCheckPair);
            }

            return TestCaseParseState.ReadingValues;

        }

        private TestCaseParseState AddTestCase(string str)
        {
            if (!(_script.Any(x=> x.Trim()!="")))
                return WriteError(new FuspecParserError(FuspecErrorType.ScriptMissed, _index));
            BuildScript();
            BuildSetCheckKits();
            _fuspecTestCases.Add(_testCaseBuilder.Build());
            return FindOpeningString(str);
        }

        private void AddLast(TestCaseParseState state)
        {
            
            if ((state == TestCaseParseState.ReadingName) || (state == TestCaseParseState.ReadingTags))
                WriteError(new FuspecParserError(FuspecErrorType.NoEndingTestCase, _index));
            
            if ( !(_script.Any(x=> x.Trim()!="")) && (state !=TestCaseParseState.ReadingValues))
                WriteError(new FuspecParserError(FuspecErrorType.NoEndingTestCase, _index));
            else
            {
                if (!(_script.Any(x=> x.Trim()!="")))
                    WriteError(new FuspecParserError(FuspecErrorType.ScriptMissed, _index));
                else
                {
                    BuildScript();
                    BuildSetCheckKits();
                    _fuspecTestCases.Add(_testCaseBuilder.Build());
                }
            }
        }

        private void BuildScript()
        {
            _testCaseBuilder.Script = "";
            foreach (var strScript in _script)
                _testCaseBuilder.Script = _testCaseBuilder.Script + strScript + "\r\n";
            _testCaseBuilder.Script =
                _testCaseBuilder.Script.Substring(0, _testCaseBuilder.Script.Length - 2);
        }
        
        private void BuildSetCheckKits()
        {
            if (!_setCheckPair.Check.Any() && _setCheckPair.Set.Any())
                _setCheckKits.Add(_setCheckPair);
         
            if (_setCheckKits.Any())
                _testCaseBuilder.SetCheckKits=_setCheckKits;
        
            _setCheckKits = new List<SetCheckPair>();
            _setCheckPair=new SetCheckPair();
        }

        private TestCaseParseState WriteError(FuspecParserError fuspecParserError)
        {
            _errors.Add(fuspecParserError);
            _isReading = false;
            return TestCaseParseState.FindingOpeningString;
        }
    }
}