using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using NFun.Exceptions;
using NFun.Fuspec.Parser.FuspecParserErrors;
using NFun.Fuspec.Parser.Interfaces;
using NFun.Fuspec.Parser.Model;
using NFun.ParseErrors;
using ParcerV1;
using static NFun.Fuspec.Parser.FuspecParserHelper;

[assembly: InternalsVisibleTo("NFun.Fuspectests")]

namespace NFun.Fuspec.Parser
{
    class TestCasesReader
    {
        private InputText _inputText;
        private TestCaseBuilder _testCaseBuilder= new TestCaseBuilder();
        private readonly List<FuspecTestCase> _fuspecTestCases =  new List<FuspecTestCase>();
        private List<string> _script = new List<string>();
        private List<ISetCheckData> _setChecks = new List<ISetCheckData>();
        private readonly List<FuspecParserError> _errors = new List<FuspecParserError>();

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
                    return ReadSetCheckValues(str);
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
                _setChecks = new List<ISetCheckData>();

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

        private TestCaseParseState ReadSetCheckValues(string str)
        {
            TestCaseParseState parseState = TestCaseParseState.ReadingValues;
            if (_inputText.ISCurrentLineSeparated('*'))
                return AddTestCase(str);

            var setString = GetContentOrNullIfStartsFromKeyword("| set", str);
            var checkString = GetContentOrNullIfStartsFromKeyword("| check", str);

            if ((setString == null && checkString == null))
                return WriteError(new FuspecParserError(FuspecErrorType.ExpectedOpeningLine, _inputText.Index));

            if (setString != null)
            {
                parseState = AddSetCheckKitToSetCkecks(setString, new SetData());
                if (parseState != TestCaseParseState.ReadingValues)
                    return parseState;
           }

            if (checkString!=null)
                            parseState = AddSetCheckKitToSetCkecks(checkString, new CheckData());
            return parseState;
        }

        private TestCaseParseState AddSetCheckKitToSetCkecks(string setCheckString, ISetCheckData setOrCheckKit)
        {
            if (setCheckString.Trim() == "")
                return WriteError(new FuspecParserError(FuspecErrorType.SetOrCheckKitMissed, _inputText.Index));
            if (setCheckString.Substring(0, 1) != " ")
                return WriteError(new FuspecParserError(FuspecErrorType.WrongSetCheckKit, _inputText.Index));
            try
            {                
                setOrCheckKit.AddValue(ParseValues(setCheckString));
                _setChecks.Add(setOrCheckKit);
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
            _testCaseBuilder.BuildSetCheckKits(_setChecks);
            _testCaseBuilder.BuildScript(_script);
            _fuspecTestCases.Add(_testCaseBuilder.Build());
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