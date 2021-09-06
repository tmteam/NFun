using System;
using System.Collections.Generic;
using System.Text;
using NFun.Exceptions;
using NFun.Interpretation;
using NFun.Interpretation.Functions;
using NFun.ParseErrors;
using NFun.Runtime;
using NFun.Tokenization;
using NFun.Types;

namespace NFun {

public class HardcoreBuilder {
    private readonly ImmutableFunctionDictionary _immutableFunctionDictionary;
    private readonly IConstantList _constants;
    private readonly AprioriTypesMap _apriori;
    private readonly DialectSettings _dialect;

    internal HardcoreBuilder() {
        _immutableFunctionDictionary = BaseFunctions.DefaultDictionary;
        _constants = new ConstantList();
        _apriori = new AprioriTypesMap();
        _dialect = DialectSettings.Default;
    }

    private HardcoreBuilder(
        ImmutableFunctionDictionary immutableFunctionDictionary,
        IConstantList constants,
        AprioriTypesMap apriori,
        DialectSettings dialect) {
        _dialect = dialect;
        _apriori = apriori;
        _immutableFunctionDictionary = immutableFunctionDictionary;
        _constants = constants;
    }

    public HardcoreBuilder WithDialect(DialectSettings dialect) =>
        new(_immutableFunctionDictionary, _constants, _apriori, dialect);

    public HardcoreBuilder WithConstant<T>(string id, T clrValue) =>
        new(_immutableFunctionDictionary, _constants.CloneWith((id, clrValue)), _apriori, _dialect);

    public HardcoreBuilder WithConstants(params (string, object)[] funValues) =>
        new(_immutableFunctionDictionary, _constants.CloneWith(funValues), _apriori, _dialect);

    public HardcoreBuilder WithApriori(string id, FunnyType type) =>
        new(_immutableFunctionDictionary, _constants, _apriori.CloneWith(id, type), _dialect);

    public HardcoreBuilder WithApriori<T>(string id) =>
        WithApriori(id, FunnyTypeConverters.GetInputConverter(typeof(T)).FunnyType);

    public HardcoreBuilder WithFunction(IFunctionSignature function) =>
        new(_immutableFunctionDictionary.CloneWith(function), _constants, _apriori, _dialect);

    public HardcoreBuilder WithFunctions(params IFunctionSignature[] functions) =>
        new(_immutableFunctionDictionary.CloneWith(functions), _constants, _apriori, _dialect);

    public HardcoreBuilder WithFunction<Tin, TOut>(string name, Func<Tin, TOut> function) =>
        WithFunction(LambdaWrapperFactory.Create(name, function));

    public HardcoreBuilder WithFunction<Tin1, Tin2, TOut>(string name, Func<Tin1, Tin2, TOut> function) =>
        WithFunction(LambdaWrapperFactory.Create(name, function));

    public HardcoreBuilder
        WithFunction<Tin1, Tin2, Tin3, TOut>(string name, Func<Tin1, Tin2, Tin3, TOut> function) =>
        WithFunction(LambdaWrapperFactory.Create(name, function));

    public HardcoreBuilder WithFunction<Tin1, Tin2, Tin3, Tin4, TOut>(
        string name,
        Func<Tin1, Tin2, Tin3, Tin4, TOut> function) =>
        WithFunction(LambdaWrapperFactory.Create(name, function));

    public HardcoreBuilder WithFunction<Tin1, Tin2, Tin3, Tin4, Tin5, TOut>(
        string name,
        Func<Tin1, Tin2, Tin3, Tin4, Tin5, TOut> function) =>
        WithFunction(LambdaWrapperFactory.Create(name, function));

    public HardcoreBuilder WithFunction<Tin1, Tin2, Tin3, Tin4, Tin5, Tin6, TOut>(
        string name,
        Func<Tin1, Tin2, Tin3, Tin4, Tin5, Tin6, TOut> function) =>
        WithFunction(LambdaWrapperFactory.Create(name, function));

    public HardcoreBuilder WithFunction<Tin1, Tin2, Tin3, Tin4, Tin5, Tin6, Tin7, TOut>(
        string name,
        Func<Tin1, Tin2, Tin3, Tin4, Tin5, Tin6, Tin7, TOut> function) =>
        WithFunction(LambdaWrapperFactory.Create(name, function));

    public FunnyRuntime Build(string script) =>
        RuntimeBuilder.Build(script, _immutableFunctionDictionary, _dialect, _constants, _apriori);

    public InterpolationCalculator BuildInterpolation(string script) {
        var texts = new List<string>();
        var scripts = new List<string>();
        SeparateInterpolation(script, texts, scripts);
        var sb = new StringBuilder();
        for (int i = 0; i < scripts.Count; i++)
            sb.Append($"___intepol{i}={scripts[i]};;");

        var runtime = Build(sb.ToString());
        var outputVars = new IFunnyVar[scripts.Count];

        for (int i = 0; i < scripts.Count; i++)
            outputVars[i] = runtime[$"___intepol{i}"];

        return new InterpolationCalculator(runtime, texts, outputVars);
    }

    private static void SeparateInterpolation(string script, List<string> texts, List<string> scripts) {
        int pos = -1;
        var reader = new Tokenizer();
        while (true)
        {
            var text = "";
            int endOfText = 0;

            if (pos != -1 || script.Length <= 0 || script[0] != '{')
            {
                (text, endOfText) = QuotationReader.ReadQuotation(script, pos, false);
                if (endOfText == -1)
                {
                    texts.Add(script.Substring(pos + 1));
                    break;
                }
            }

            pos = endOfText;

            texts.Add(text);

            var nextSymbol = script[pos];
            if (nextSymbol != '{')
                throw new NFunImpossibleException($"Unexpected symbol '{nextSymbol}'");

            int obrCount = 1;
            pos++;
            //search end of quotation
            while (obrCount != 0)
            {
                var res = reader.TryReadNext(script, pos);
                if (res.Type == TokType.FiObr)
                    obrCount++;
                else if (res.Type == TokType.FiCbr)
                    obrCount--;
                else if (res.Type == TokType.Eof)
                    throw ErrorFactory.ClosingQuoteIsMissed('}', pos, text.Length);

                pos = res.Finish;
            }

            scripts.Add(script.Substring(endOfText + 1, pos - endOfText - 2));
            pos--;
            //end of script body here!
        }
    }
}

}