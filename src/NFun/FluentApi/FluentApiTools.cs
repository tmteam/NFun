using System;
using System.Linq;
using System.Reflection;
using NFun.Runtime;
using NFun.SyntaxParsing;
using NFun.Types;

namespace NFun.FluentApi
{
    internal static class FluentApiTools
    {
        public static TOutput CreateOutputValueFromResults<TOutput>(
            Memory<(string, IOutputFunnyConverter, PropertyInfo)> outputs, 
            CalculationResult calcResults)
            where TOutput : new()
        {
            var span = outputs.Span;
            var answer = new TOutput();
            int settedCount = 0;
            for (int i = 0; i < outputs.Length; i++)
            {
                var (outName, outConverter, outProperty) = span[i];
                if (calcResults.TryGet(outName, out var actualOutput))
                {
                    var clrValue = outConverter.ToClrObject(actualOutput.Value);
                    outProperty.SetValue(answer, clrValue);
                    settedCount++;
                }
            }

            if (settedCount == 0)
                throw new FunInvalidUsageTODOException("no output values were setted");
            return answer;
        }

        public  static Memory<(string, IOutputFunnyConverter, PropertyInfo)> SetupManyAprioriOutputs<TOutput>(IFunBuilder builder) where TOutput : new()
        {
            var outputProperties = typeof(TOutput).GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var outputVarVals = new (string, IOutputFunnyConverter, PropertyInfo)[outputProperties.Length];
            int actualOutputsCount = 0;
            for (var i = 0; i < outputProperties.Length; i++)
            {
                var outputProperty = outputProperties[i];
                if (!outputProperty.CanWrite)
                    continue;
                var converter = FunnyTypeConverters.GetOutputConverter(outputProperty.PropertyType);
                var outputName = outputProperty.Name.ToLower();

                builder.WithApriori(outputName, converter.FunnyType);
                outputVarVals[actualOutputsCount] = new(
                    outputName,
                    converter,
                    outputProperty);

                actualOutputsCount++;
            }

            return outputVarVals.AsMemory(0,actualOutputsCount);
        }

        public  static object GetClrOut(CalculationResult result)
        {
            if (!result.TryGet(Parser.AnonymousEquationId, out var outResult))
                throw new FunInvalidUsageTODOException("output is not set");
            
            return FunnyTypeConverters
                .GetOutputConverter(outResult.Type)
                .ToClrObject(outResult.Value);
        }

        public  static TOutput CalcSingleOutput<TOutput>(IFunBuilder builder, Span<VarVal> inputValues)
        {
            var outputConverter = FunnyTypeConverters.GetOutputConverter(typeof(TOutput));
            builder.WithApriori(Parser.AnonymousEquationId, outputConverter.FunnyType);
            var runtime = builder.Build();

            var varsAsArray = inputValues.ToArray();
            if (runtime.Inputs.Any(i => varsAsArray.All(v => !v.Name.Equals(i.Name.ToLower()))))
                throw new FunInvalidUsageTODOException();

            var result = runtime.CalculateSafe(inputValues);
            if (!result.TryGet(Parser.AnonymousEquationId, out var outResult))
                throw new FunInvalidUsageTODOException("out value is missed");
            
            return (TOutput) outputConverter.ToClrObject(outResult.Value);
        }

        public  static Span<VarVal> SetupInputs<TInput>(TInput input, IFunBuilder builder)
        {
            var inputProperties = typeof(TInput).GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var inputVarVals = new VarVal[inputProperties.Length];
            int actualInputsCount = 0;
            for (var i = 0; i < inputProperties.Length; i++)
            {
                var inputProperty = inputProperties[i];
                if (!inputProperty.CanRead)
                    continue;
                var converter = FunnyTypeConverters.GetInputConverter(inputProperty.PropertyType);
                var inputName = inputProperty.Name.ToLower();
                builder.WithApriori(inputName, converter.FunnyType);
                inputVarVals[i] = new VarVal(
                    inputName,
                    converter.ToFunObject(inputProperty.GetValue(input)),
                    converter.FunnyType
                );
                actualInputsCount++;
            }

            var inputVals = inputVarVals.AsSpan(0, actualInputsCount);
            return inputVals;
        }

        public static VarVal[] GetInputValues<TInput>(Memory<(string, IinputFunnyConverter, PropertyInfo)> inputMap, TInput value)
        {
            var span = inputMap.Span;
            var inputVarVals = new VarVal[span.Length];
                
            for (var i = 0; i < span.Length; i++)
            {
                var (name, converter, propertyInfo) = span[i];
                inputVarVals[i] = new VarVal(
                    name,
                    converter.ToFunObject(propertyInfo.GetValue(value)),
                    converter.FunnyType
                );
            }
            return inputVarVals;
        }
        
        public  static Memory<(string, IinputFunnyConverter, PropertyInfo)> 
            SetupAprioriInputs<TInput>(IFunBuilder builder)
        {
            var inputProperties = typeof(TInput).GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var inputTypes = new (string, IinputFunnyConverter, PropertyInfo)[inputProperties.Length];
            int actualInputsCount = 0;
            
            for (var i = 0; i < inputProperties.Length; i++)
            {
                var inputProperty = inputProperties[i];
                if (!inputProperty.CanRead)
                    continue;
                var converter = FunnyTypeConverters.GetInputConverter(inputProperty.PropertyType);
                var inputName = inputProperty.Name.ToLower();
                
                builder.WithApriori(inputName, converter.FunnyType);
                inputTypes[i] = new (
                    inputName,
                    converter,
                    inputProperty
                );
                actualInputsCount++;
            }

            return inputTypes.AsMemory(0, actualInputsCount);
        }
    }
}