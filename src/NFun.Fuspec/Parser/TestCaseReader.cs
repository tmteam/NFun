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
    //todo cr: Too large class. 9 states.  SRP violation. 
    class TestCasesReader
    {
        //todo cr: Some fields initialized implicitly, some in ctor
        //todo cr: use readonly if field is initializing in ctor
        private InputText _inputText;
        private TestCaseBuilder _testCaseBuilder= new TestCaseBuilder();
        private List<FuspecTestCase> _fuspecTestCases =  new List<FuspecTestCase>();
        private List<string> _script = new List<string>();
        private List<SetCheckPair> _setCheckKits = new List<SetCheckPair>();
        private SetCheckPair _setCheckPair = new SetCheckPair();
        private List<FuspecParserError> _errors = new List<FuspecParserError>();

        internal FuspecTestCases Read(InputText inputText)
        {
            _inputText = inputText;
       
            var state = TestCaseParseState.FindingOpeningString;
           
            while (!_inputText.Eof)
            {
                  if (!(_inputText.IsCurentLineEmty() && (state!=TestCaseParseState.ReadingBody && state!=TestCaseParseState.ReadingParamsIn)))
                    state = ReadNext(_inputText.CurrentLine, state);
                _inputText.MoveNext();
            }

            if (_inputText.Eof)  
            AddLast(state);

            if (_errors.Any())
                return new FuspecTestCases(_errors.ToArray());
            return new FuspecTestCases(_fuspecTestCases.ToArray());
        }

        private TestCaseParseState ReadNext(string str, TestCaseParseState testCaseParseState)
        {
            switch (testCaseParseState)
            {
                case TestCaseParseState.FindingOpeningString:
                    return FindOpeningString(str);
                case TestCaseParseState.ReadingName:
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
                    return WriteError(new FuspecParserError(FuspecErrorType.Unknown, _inputText.Index));
            }
        }

        private TestCaseParseState FindOpeningString(string str)
        {
            _testCaseBuilder = new TestCaseBuilder();
            _testCaseBuilder.StartLine = _inputText.Index;
            if (_inputText.ISCurrentLineSeparated('*'))
            {
                _script = new List<string>();
                _setCheckKits = new List<SetCheckPair>();
                return TestCaseParseState.ReadingName;
            }

            return WriteError(new FuspecParserError(FuspecErrorType.OpeningStringMissed, _inputText.Index));
        }

        private TestCaseParseState FindName(string str)
        {
            if (_inputText.IsCurentLineEmty())
               return TestCaseParseState.ReadingName;

            var name = GetContentOrNullIfStartsFromKeyword("| TEST", str) ?? GetContentOrNullIfStartsFromKeyword("| TODO", str);
            if (name == null || name.Trim() == "")
                return WriteError(new FuspecParserError(FuspecErrorType.NamedMissed, _inputText.Index));
            _testCaseBuilder.Name = name.Trim();
            if (str.IndexOf("TODO") == 2)
                _testCaseBuilder.IsTestExecuted = false;
            return TestCaseParseState.ReadingTags;
        }

        private TestCaseParseState FindTags(string str)
        {
            if (_inputText.ISCurrentLineSeparated('*'))
                return TestCaseParseState.ReadingParamsIn;

            //todo cr: move all magic constants to class constants
            var tags = GetContentOrNullIfStartsFromKeyword("| TAGS", str);

            if (tags == null)
                return WriteError(new FuspecParserError(FuspecErrorType.EndingHeadMissed, _inputText.Index));

            if (tags.Trim() != "")
                _testCaseBuilder.Tags = SplitWithTrim(tags, ',').ToArray();

            return TestCaseParseState.ReadingTags;
        }

        private TestCaseParseState ReadParamsIn(string str)
        {
            if (str.Trim() == "")
            {
                _script.Add(str);
                return TestCaseParseState.ReadingParamsIn;
            }

            var paramInString = GetContentOrNullIfStartsFromKeyword("| in", str);
            if (paramInString == null)
                return ReadParamsOut(str);

            _script = new List<string>();
            if (paramInString.Trim() == "" || paramInString.Substring(0, 1) != " ")
                return WriteError(new FuspecParserError(FuspecErrorType.ParamInMissed, _inputText.Index));

            try
            {
                _testCaseBuilder.ParamsIn = ParseVarType(paramInString,false);
            }
            catch (Exception e)
            {
                return WriteError(new FuspecParserError(FuspecErrorType.NFunMessage_ICantParseParamTypeString, _inputText.Index));
            }

            if (_testCaseBuilder.ParamsIn == null)
                return WriteError(new FuspecParserError(FuspecErrorType.NFunMessage_ICantParseParamTypeString, _inputText.Index));

            if (!_testCaseBuilder.ParamsIn.Any())
                return WriteError(new FuspecParserError(FuspecErrorType.ParamInMissed, _inputText.Index));

            return TestCaseParseState.ReadingParamsOut;
        }

        private TestCaseParseState ReadParamsOut(string str)
        {
            if (_inputText.ISCurrentLineSeparated('-'))
                return TestCaseParseState.ReadingBody;

            var paramOutString = GetContentOrNullIfStartsFromKeyword("| out", str);
            if (paramOutString == null)
            {
                if (_testCaseBuilder.ParamsIn.Any() || _testCaseBuilder.ParamsOut.Any())
                    return WriteError(new FuspecParserError(FuspecErrorType.SeparatedStringMissed, _inputText.Index));
                return ReadBody(str);
            }

            if (paramOutString.Trim() == "" || paramOutString.Substring(0, 1) != " ")
                return WriteError(new FuspecParserError(FuspecErrorType.ParamOutMissed, _inputText.Index));

            try
            {
                _testCaseBuilder.ParamsOut = ParseVarType(paramOutString,true);
            }
            catch (Exception e)
            {
                return WriteError(new FuspecParserError(FuspecErrorType.NFunMessage_ICantParseParamTypeString, _inputText.Index));
            }

            if (_testCaseBuilder.ParamsOut == null)
                WriteError(new FuspecParserError(FuspecErrorType.NFunMessage_ICantParseParamTypeString, _inputText.Index));

            return TestCaseParseState.ReadingParamsOut;
        }

        private TestCaseParseState ReadBody(string str)
        {
            if (str.Trim() == "")
            {
                _script.Add(str);
                return TestCaseParseState.ReadingBody;
            }

            if (_inputText.ISCurrentLineSeparated('-'))
            {
                if (!(_script.Any(x => x.Trim() != "")))
                    WriteError(new FuspecParserError(FuspecErrorType.ScriptMissed, _inputText.Index));
                return TestCaseParseState.ReadingValues;
            }

            if (_inputText.ISCurrentLineSeparated('*'))
                return AddTestCase(str);

            if (GetContentOrNullIfStartsFromKeyword("|***", str) != null && !_inputText.ISCurrentLineSeparated('*'))
                return WriteError(new FuspecParserError(FuspecErrorType.OpeningStringMissed, _inputText.Index));


            if (GetContentOrNullIfStartsFromKeyword("| set", str) != null ||
                GetContentOrNullIfStartsFromKeyword("| check", str) != null)
            {
                if ((!_script.Any(x => x.Trim() != "")))
                    WriteError(new FuspecParserError(FuspecErrorType.ScriptMissed, _inputText.Index));
                WriteError(new FuspecParserError(FuspecErrorType.SeparatedStringMissed, _inputText.Index));
            }

            _script.Add(str);
            return TestCaseParseState.ReadingBody;
        }

        private TestCaseParseState ReadValues(string str)
        {
            if (_inputText.ISCurrentLineSeparated('*'))
                return AddTestCase(str);

            var setString = GetContentOrNullIfStartsFromKeyword("| set", str);
            var checkString = GetContentOrNullIfStartsFromKeyword("| check", str);

            if ((setString == null && checkString == null))
                return WriteError(new FuspecParserError(FuspecErrorType.ExpectedOpeningLine, _inputText.Index));

            if (setString != null)
            {
                if (setString.Trim() == "")
                    return WriteError(new FuspecParserError(FuspecErrorType.SetKitMissed, _inputText.Index));
                if (setString.Substring(0, 1) != " ")
                    return WriteError(new FuspecParserError(FuspecErrorType.WrongSetCheckKit, _inputText.Index));


                //if there was only a 'set' string before the current one, then add 'set-check-kit'
                if (!_setCheckPair.Check.Any() && _setCheckPair.Set.Any())
                    _setCheckKits.Add(_setCheckPair);
                _setCheckPair = new SetCheckPair();
                try
                {
                    _setCheckPair.AddSet(ParseValues(setString));
                }
                catch (FunParseException e)
                {
                    Console.WriteLine($"fpe {e.Code} {e.Message}");
                    return WriteError(new FuspecParserError(FuspecErrorType.NFunMessage_ICantParseValue, _inputText.Index));
                }
                catch (Exception e)
                {
                    return WriteError(new FuspecParserError(FuspecErrorType.NFunMessage_ICantParseValue, _inputText.Index));
                }
            }

            if (checkString != null)
            {
                if (checkString.Trim() == "")
                    return WriteError(new FuspecParserError(FuspecErrorType.CheckKitMissed, _inputText.Index));
                if (checkString.Substring(0, 1) != " ")
                    return WriteError(new FuspecParserError(FuspecErrorType.WrongSetCheckKit, _inputText.Index));

                try
                {
                    _setCheckPair.AddGet(ParseValues(checkString));
                }
                catch (Exception e)
                {
                    return WriteError(new FuspecParserError(FuspecErrorType.NFunMessage_ICantParseValue, _inputText.Index));
                }

                _setCheckKits.Add(_setCheckPair);
            }

            return TestCaseParseState.ReadingValues;
        }

        private TestCaseParseState AddTestCase(string str)
        {
            if (!(_script.Any(x => x.Trim() != "")))
                return WriteError(new FuspecParserError(FuspecErrorType.ScriptMissed, _inputText.Index));
            
           BuildTestCase();
            
            return FindOpeningString(str);
        }

        private void BuildTestCase()
        {
            
            if (!_setCheckPair.Check.Any() && _setCheckPair.Set.Any())
                _setCheckKits.Add(_setCheckPair);
            
           
            _testCaseBuilder.BuildSetCheckKits(_setCheckKits);
            _testCaseBuilder.BuildScript(_script);
            _fuspecTestCases.Add(_testCaseBuilder.Build());


            _setCheckKits = new List<SetCheckPair>();
            _setCheckPair = new SetCheckPair();   
        }

        private void AddLast(TestCaseParseState state)
        {
            if (state == TestCaseParseState.FindingOpeningString)
                return;
            if ((state == TestCaseParseState.ReadingName) || (state == TestCaseParseState.ReadingTags))
                WriteError(new FuspecParserError(FuspecErrorType.NoEndingTestCase, _inputText.Index));

            if (!(_script.Any(x => x.Trim() != "")) && (state != TestCaseParseState.ReadingValues))
                WriteError(new FuspecParserError(FuspecErrorType.NoEndingTestCase, _inputText.Index));
            
            if (!(_script.Any(x => x.Trim() != "")))
                WriteError(new FuspecParserError(FuspecErrorType.ScriptMissed, _inputText.Index));
           
            else
                BuildTestCase();
        }

        private TestCaseParseState WriteError(FuspecParserError fuspecParserError)
        {
            _errors.Add(fuspecParserError);
            return TestCaseParseState.FindingOpeningString;
        }
    }
}