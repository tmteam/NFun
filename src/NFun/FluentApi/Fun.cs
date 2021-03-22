using System;
using System.Reflection;
using NFun.SyntaxParsing;
using NFun.Types;

namespace NFun.FluentApi
{
    public static class Fun
    {
        public static TOutput Calc<TInput, TOutput>(string expresion, TInput input) where TOutput : new()
        {
             var inputProperties = typeof(TInput).GetProperties(BindingFlags.Instance | BindingFlags.Public);
             
             var builder = FunBuilder.With(expresion);
             var inputVarVals = new VarVal[inputProperties.Length];
             for (var i = 0; i < inputProperties.Length; i++)
             {
                 var inputProperty = inputProperties[i];
                 if (!inputProperty.CanRead)
                     throw new ArgumentException($"Property '{inputProperty.Name}' has no getter");
                 var converter = GetConverterOrThrow(inputProperty.PropertyType, inputProperty.Name);
                 var inputName = inputProperty.Name.ToLower();
                 builder.WithApriori(inputName, converter.FunType);
                 inputVarVals[i] = new VarVal(
                     inputName,
                     converter.ToFunObject(inputProperty.GetValue(input)),
                     converter.FunType
                 );
             }

             var outputConverter = GetConverterOrThrow(typeof(TOutput), Parser.AnonymousEquationId);
             builder.WithApriori(Parser.AnonymousEquationId, outputConverter.FunType);
             var runtime = builder.Build();
             var result = runtime.CalculateSafe(inputVarVals);
             var outResult = result.Get(Parser.AnonymousEquationId);
             return  (TOutput)outResult.Value;
        }

        private static FunTypesConverter GetConverterOrThrow(Type type, string name)
        {
            if (FunTypesConverter.TryGetSpecificConverter(type, out var converter)) 
                return converter;
            
            var primitiveType = VarVal.ToPrimitiveFunType(type);
            if (primitiveType == null)
            {
                throw new ArgumentException(
                    $"Input property '{name}' has unsupported type '{type.Name}'");
            }
            return new PrimitiveTypeConverter(primitiveType);
        }
    }
}