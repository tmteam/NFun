using System;
using System.Linq;
using System.Reflection;
using NFun.Interpretation;
using NFun.Interpretation.Functions;
using NFun.ParseErrors;
using NFun.Runtime;
using NFun.SyntaxParsing;
using NFun.Types;

namespace NFun {

internal static class FluentApiTools {
    public static TOutput CreateOutputValueFromResults<TOutput>(
        FunnyRuntime runtime,
        Memory<(string, IOutputFunnyConverter, PropertyInfo)> outputs
    )  where TOutput : new() {
        var answer = new TOutput();
        var settedCount = FillOutputValueFromResults(runtime, outputs, answer);
        if (settedCount == 0)
            throw Errors.NoOutputVariablesSetted(outputs);
        return answer;
    }

    public static int FillOutputValueFromResults<TContext>(
        FunnyRuntime runtime,
        Memory<(string, IOutputFunnyConverter, PropertyInfo)> outputs,
        TContext context
    ) {
        var span = outputs.Span;
        int settedCount = 0;
        for (int i = 0; i < outputs.Length; i++)
        {
            var (outName, outConverter, outProperty) = span[i];
            var actualOutput = runtime[outName];
            if (actualOutput == null) continue;
            outProperty.SetValue(context, outConverter.ToClrObject(actualOutput.FunnyValue));
            settedCount++;
        }

        return settedCount;
    }

    internal static Memory<(string, IOutputFunnyConverter, PropertyInfo)> AddManyAprioriOutputs<TOutput>(
        this MutableAprioriTypesMap aprioriTypesMap, DialectSettings dialectSettings) {
        var outputProperties = typeof(TOutput).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        var outputVarVals = new (string, IOutputFunnyConverter, PropertyInfo)[outputProperties.Length];
        int actualOutputsCount = 0;
        foreach (var outputProperty in outputProperties)
        {
            if (!outputProperty.HasPublicSetter())
                continue;
            var converter = dialectSettings.TypeBehaviour.GetOutputConverterFor(outputProperty.PropertyType);
            var outputName = outputProperty.Name.ToLower();

            aprioriTypesMap.Add(outputName, converter.FunnyType);
            outputVarVals[actualOutputsCount] = new ValueTuple<string, IOutputFunnyConverter, PropertyInfo>(
                outputName,
                converter,
                outputProperty);

            actualOutputsCount++;
        }

        return outputVarVals.AsMemory(0, actualOutputsCount);
    }

    internal static object GetClrOut(FunnyRuntime runtime) => GetOut(runtime).Value;

    internal static IFunnyVar GetOut(FunnyRuntime runtime) =>
        runtime[Parser.AnonymousEquationId] ?? throw Errors.OutputIsUnset();

    internal static void SetInputValues<TInput>(
        FunnyRuntime runtime,
        Memory<(string, IInputFunnyConverter, PropertyInfo)> inputMap, TInput value) {
        var span = inputMap.Span;

        foreach (var (name, converter, propertyInfo) in span)
        {
            if (!runtime.VariableDictionary.TryGetUsages(name, out var variableUsages))
                continue;

            variableUsages.Source.SetFunnyValueUnsafe(converter.ToFunObject(propertyInfo.GetValue(value)));
        }
    }

    public static Memory<(string, IInputFunnyConverter, PropertyInfo)> AddAprioriInputs<TInput>(
            this MutableAprioriTypesMap mutableApriori, 
            TypeBehaviour typeBehaviour, 
            bool ignoreIfHasSetter = false) {
        
        var inputProperties = typeof(TInput).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        var inputTypes = new (string, IInputFunnyConverter, PropertyInfo)[inputProperties.Length];
        int actualInputsCount = 0;

        for (var i = 0; i < inputProperties.Length; i++)
        {
            var inputProperty = inputProperties[i];

            if (!inputProperty.HasPublicGetter())
                continue;
            
            if(ignoreIfHasSetter && inputProperty.HasPublicSetter())
                continue;
            
            var converter = typeBehaviour.GetInputConverterFor(inputProperty.PropertyType);
            var inputName = inputProperty.Name.ToLower();

            mutableApriori.Add(inputName, converter.FunnyType);
            inputTypes[i] = new ValueTuple<string, IInputFunnyConverter, PropertyInfo>(
                inputName,
                converter,
                inputProperty
            );
            actualInputsCount++;
        }

        return inputTypes.AsMemory(0, actualInputsCount);
    }

    internal static void ThrowIfHasInputs(FunnyRuntime runtime) {
        if (runtime.Variables.Any(v => !v.IsOutput))
            throw Errors.UnknownInputs(runtime.GetInputVariableUsages());
    }

    internal static void ThrowIfHasNoDefaultOutput(FunnyRuntime runtime) {
        if (runtime[Parser.AnonymousEquationId]?.IsOutput != true)
            throw Errors.OutputIsUnset();
    }

    internal static void ThrowIfHasUnknownInputs(
        FunnyRuntime runtime,
        Memory<(string, IInputFunnyConverter, PropertyInfo)> expectedInputs) {
        var span = expectedInputs.Span;
        foreach (var actualInput in runtime.Variables)
        {
            if (actualInput.IsOutput)
                continue;

            bool known = false;
            for (var i = 0; i < span.Length; i++)
            {
                if (span[i].Item1.Equals(actualInput.Name, StringComparison.OrdinalIgnoreCase))
                {
                    known = true;
                    break;
                }
            }

            if (!known)
            {
                throw Errors.UnknownInputs(runtime.GetInputVariableUsages());
            }
        }
    }
}

}