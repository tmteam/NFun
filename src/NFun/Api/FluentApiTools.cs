using System;
using System.Linq;
using System.Reflection;
using NFun.Interpretation;
using NFun.ParseErrors;
using NFun.Runtime;
using NFun.SyntaxParsing;
using NFun.Types;

namespace NFun {

internal static class FluentApiTools {
    public static TOutput CreateOutputModelFromResults<TOutput>(FunnyRuntime runtime,Memory<OutputProperty> outputs) 
        where TOutput : new() {
        var answer = new TOutput();
        var settedCount = SetResultsToModel(runtime, outputs, answer);
        if (settedCount == 0)
            throw Errors.NoOutputVariablesSetted(outputs);
        return answer;
    }

    public static int SetResultsToModel<TContext>(
        FunnyRuntime runtime,
        Memory<OutputProperty> outputs,
        TContext targetClrObject
    ) {
        int settedCount = 0;
        foreach (var output in outputs.Span)
        {
            var actualOutput = runtime[output.PropertyName];
            if (actualOutput == null) continue;
            output.SetValueToTargetProperty(actualOutput.FunnyValue, targetClrObject);
            settedCount++;
        }
        return settedCount;
    }

    internal static Memory<OutputProperty> AddManyAprioriOutputs<TOutput>(
        this MutableAprioriTypesMap aprioriTypesMap, 
        DialectSettings dialectSettings) {
        
        var outputPropertyInfos = typeof(TOutput).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        var outputs = new OutputProperty[outputPropertyInfos.Length];
        int actualOutputsCount = 0;
        foreach (var outputProperty in outputPropertyInfos)
        {
            if (!outputProperty.HasPublicSetter())
                continue;
            var converter = dialectSettings.TypeBehaviour.GetOutputConverterFor(outputProperty.PropertyType);
            var outputName = outputProperty.Name.ToLower();

            aprioriTypesMap.Add(outputName, converter.FunnyType);
            outputs[actualOutputsCount] = new OutputProperty(outputName,converter,outputProperty);
            actualOutputsCount++;
        }

        return outputs.AsMemory(0, actualOutputsCount);
    }

    internal static object GetClrOut(FunnyRuntime runtime) => GetFunnyOut(runtime).Value;

    internal static IFunnyVar GetFunnyOut(FunnyRuntime runtime) =>
        runtime[Parser.AnonymousEquationId] ?? throw Errors.OutputIsUnset();

    internal static void SetInputValues<TInput>(FunnyRuntime runtime,Memory<InputProperty> inputMap, TInput value) {
        var span = inputMap.Span;

        foreach (var inputProperty in span)
        {
            if (!runtime.VariableDictionary.TryGetUsages(inputProperty.PropertyName, out var variableUsages))
                continue;

            variableUsages.Source.SetFunnyValueUnsafe(inputProperty.GetFunValue(value));
        }
    }

    public static Memory<InputProperty> AddAprioriInputs<TInput>(
            this MutableAprioriTypesMap mutableApriori, 
            TypeBehaviour typeBehaviour, 
            bool ignoreIfHasSetter = false) {
        
        var inputProperties = typeof(TInput).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        var inputTypes = new InputProperty[inputProperties.Length];
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
            inputTypes[i] = new InputProperty(inputName,converter,inputProperty);
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
        Memory<InputProperty> expectedInputs) {
        var span = expectedInputs.Span;
        foreach (var actualInput in runtime.Variables)
        {
            if (actualInput.IsOutput)
                continue;

            bool known = false;
            for (var i = 0; i < span.Length; i++)
            {
                if (span[i].PropertyName.Equals(actualInput.Name, StringComparison.OrdinalIgnoreCase))
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