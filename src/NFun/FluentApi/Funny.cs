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
                throw new ArgumentException("TODO");
            
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
                throw new ArgumentException("TODO");
                
            var resultOutput = runtime.Outputs.FirstOrDefault(o => o.Name == Parser.AnonymousEquationId);
            if (resultOutput.Name != Parser.AnonymousEquationId)
                throw new ArgumentException();
            
            var result = runtime.CalculateSafe(inputVals);
            return GetClrOut(result);
        }

        public static TOutput Calc<TInput, TOutput>(string expression, TInput input)
        {
            var builder = FunBuilder.With(expression);
            var inputValues = SetupInputs(input, builder);

            return CalcOutput<TOutput>(builder, inputValues);
        }
        
        private static object GetClrOut(CalculationResult result)
        {
            var outResult = result.Get(Parser.AnonymousEquationId);

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
                throw new ArgumentException("TODO");

            var result = runtime.CalculateSafe(inputValues);
            var outResult = result.Get(Parser.AnonymousEquationId);
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