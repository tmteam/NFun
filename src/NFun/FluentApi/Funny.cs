using System;
using System.Linq;
using System.Reflection;
using NFun.Runtime;
using NFun.SyntaxParsing;
using NFun.Types;

namespace NFun.FluentApi
{
    public static class Funny
    {
        public static object Calc(string expression)
        {
            var runtime = FunBuilder
                .With(expression)
                .Build();
            if (runtime.Inputs.Any())
                throw new FunInvalidUsageTODOException();
            
            var result = runtime.CalculateSafe();
            return GetClrOut(result);
        }
        public static TOutput Calc<TOutput>(string expression)
        {
            var builder = FunBuilder.With(expression);
            
            return CalcOutput<TOutput>(builder, Span<VarVal>.Empty);
        }
        public static object Calc<TInput>(string expression, TInput input)
        {
            var builder = FunBuilder.With(expression);
            var inputVals = SetupInputs(input, builder);
            var runtime = builder.Build();
            var varsAsArray = inputVals.ToArray();
            if (runtime.Inputs.Any(i => varsAsArray.All(v => !v.Name.Equals(i.Name.ToLower()))))
                throw new FunInvalidUsageTODOException();
                
            var resultOutput = runtime.Outputs.FirstOrDefault(o => o.Name == Parser.AnonymousEquationId);
            if (resultOutput.Name != Parser.AnonymousEquationId)
                throw new FunInvalidUsageTODOException("out value is missed");

            var result = runtime.CalculateSafe(inputVals);
            return GetClrOut(result);
        }

        public static TOutput Calc<TInput, TOutput>(string expression, TInput input)
        {
            var builder = FunBuilder.With(expression);
            var inputValues = SetupInputs(input, builder);

            return CalcOutput<TOutput>(builder, inputValues);
        }

        public static TOutput CalcMany<TOutput>(string expression) where TOutput: new()
        {
            var builder = FunBuilder.With(expression);
            
            var outputs = SetupManyOutputs<TOutput>(builder);

            var runtime = builder.Build();
            if (runtime.Inputs.Any())
                throw new FunInvalidUsageTODOException();
            
            var calcResults = runtime.CalculateSafe();
            return CreateOutputValueFromResults<TOutput>(outputs, calcResults);
        }
        
        public static TOutput CalcMany<TInput, TOutput>(string expression, TInput input) where TOutput: new()
        {
            var builder = FunBuilder.With(expression);
            var inputVals = SetupInputs(input, builder);

            var outputs = SetupManyOutputs<TOutput>(builder);
            var runtime = builder.Build();
            
            var varsAsArray = inputVals.ToArray();
            if (runtime.Inputs.Any(i => varsAsArray.All(v => !v.Name.Equals(i.Name.ToLower()))))
                throw new FunInvalidUsageTODOException();
            
            var calcResults = runtime.CalculateSafe();
            return CreateOutputValueFromResults<TOutput>(outputs, calcResults);
        }

        private static TOutput CreateOutputValueFromResults<TOutput>(
            Span<(string, IOutputFunnyConverter, PropertyInfo)> outputs, 
            CalculationResult calcResults)
            where TOutput : new()
        {
            var answer = new TOutput();
            int settedCount = 0;
            for (int i = 0; i < outputs.Length; i++)
            {
                var (outName, outConverter, outProperty) = outputs[i];
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

        private static Span<(string, IOutputFunnyConverter, PropertyInfo)> 
            SetupManyOutputs<TOutput>(FunBuilder builder) where TOutput : new()
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

            return outputVarVals.AsSpan(0,actualOutputsCount);
        }

        private static object GetClrOut(CalculationResult result)
        {
            if (!result.TryGet(Parser.AnonymousEquationId, out var outResult))
                throw new FunInvalidUsageTODOException("output is not set");
            
            return FunnyTypeConverters
                .GetOutputConverter(outResult.Type)
                .ToClrObject(outResult.Value);
        }

        private static TOutput CalcOutput<TOutput>(FunBuilder builder, Span<VarVal> inputValues)
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

        private static Span<VarVal> SetupInputs<TInput>(TInput input, FunBuilder builder)
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
        
    }
}