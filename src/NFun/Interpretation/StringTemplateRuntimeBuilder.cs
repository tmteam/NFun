using System.Collections.Generic;
using System.Text;
using NFun.Exceptions;
using NFun.ParseErrors;
using NFun.Runtime;
using NFun.Tokenization;

namespace NFun.Interpretation {

internal static class StringTemplateRuntimeBuilder {
    private const string AnonymIdPrefix = "___intepol___";

    internal static StringTemplateCalculator Build(
        string script,
        IFunctionDictionary functionDictionary,
        DialectSettings dialect,
        IConstantList constants = null,
        AprioriTypesMap aprioriTypesMap = null) {
        //not the most effective way to build interpolation runtime
        //but at least it works
        SeparateStringTemplate(script, out var texts, out var scripts);

        //create new script with all scripts inside
        var sb = new StringBuilder();
        for (int i = 0; i < scripts.Count; i++)
            sb.Append($"{AnonymIdPrefix}{i}={scripts[i]};;");

        var runtime = RuntimeBuilder.Build(sb.ToString(), functionDictionary, dialect, constants, aprioriTypesMap);
        var outputVars = new IFunnyVar[scripts.Count];

        for (int i = 0; i < scripts.Count; i++)
            outputVars[i] = runtime[$"{AnonymIdPrefix}{i}"];

        return new StringTemplateCalculator(runtime, texts, outputVars);
    }

    /// <summary>
    /// separates template string to texts and inner scripts
    /// </summary>
    /// <param name="script">origin interpolation text</param>
    /// <param name="texts">texts between scripts</param>
    /// <param name="scripts">scripts between texts</param>
    /// <exception cref="FunParseException"></exception>
    private static void SeparateStringTemplate(string script, out List<string> texts, out List<string> scripts) {
        texts = new List<string>();
        scripts = new List<string>();

        int pos = -1;
        var reader = new Tokenizer();
        while (true)
        {
            var text = "";
            int endOfText = 0;

            if (pos != -1 || script.Length <= 0 || script[0] != '{')
            {
                (text, endOfText) = QuotationReader.ReadQuotation(script, pos, null);
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