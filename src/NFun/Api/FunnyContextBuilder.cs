using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Interpretation;
using NFun.Interpretation.Functions;
using NFun.ParseErrors;
using NFun.Runtime;
using NFun.SyntaxParsing;
using NFun.Types;

namespace NFun
{
    public class FunnyContextBuilder
    {
        internal static  FunnyContextBuilder Empty  => new();
        private ClassicDialectSettings _dialect = ClassicDialectSettings.Default;
        private readonly List<(string, object)> _constantList = new();
        private readonly List<IConcreteFunction> _concreteFunctions = new();
        
        public FunnyContextBuilder WithDialect(ClassicDialectSettings dialect)
        {
            _dialect = dialect;
            return this;
        }
        public FunnyContextBuilder WithConstant(string id, object value) {
            _constantList.Add((id,value));
            return this;
        }

        public FunnyContextBuilder WithFunction<Tin, TOut>(string name, Func<Tin, TOut> function)
        {
            _concreteFunctions.Add(LambdaWrapperFactory.Create(name, function));
            return this;
        }
        public FunnyContextBuilder WithFunction<Tin1,Tin2,TOut>(string name, Func<Tin1,Tin2,TOut> function)
        {
            _concreteFunctions.Add(LambdaWrapperFactory.Create(name, function));
            return this;
        }
        public FunnyContextBuilder WithFunction<Tin1,Tin2,Tin3,TOut>(string name, Func<Tin1,Tin2,Tin3,TOut> function)
        {
            _concreteFunctions.Add(LambdaWrapperFactory.Create(name, function));
            return this;
        }
        public FunnyContextBuilder WithFunction<Tin1,Tin2,Tin3,Tin4,TOut>(string name, Func<Tin1,Tin2,Tin3,Tin4,TOut> function)
        {
            _concreteFunctions.Add(LambdaWrapperFactory.Create(name, function));
            return this;
        }
        public FunnyContextBuilder WithFunction<Tin1,Tin2,Tin3,Tin4,Tin5,TOut>(string name, Func<Tin1,Tin2,Tin3,Tin4,Tin5,TOut> function)
        {
            _concreteFunctions.Add(LambdaWrapperFactory.Create(name, function));
            return this;
        }
        public FunnyContextBuilder WithFunction<Tin1,Tin2,Tin3,Tin4,Tin5,Tin6,TOut>(string name, Func<Tin1,Tin2,Tin3,Tin4,Tin5,Tin6,TOut> function)
        {
            _concreteFunctions.Add(LambdaWrapperFactory.Create(name, function));
            return this;
        }
        public FunnyContextBuilder WithFunction<Tin1,Tin2,Tin3,Tin4,Tin5,Tin6,Tin7,TOut>(string name, Func<Tin1,Tin2,Tin3,Tin4,Tin5,Tin6,Tin7,TOut> function)
        {
            _concreteFunctions.Add(LambdaWrapperFactory.Create(name, function));
            return this;
        }

        public IFunnyContext<TInput> ForCalc<TInput>() 
            => new FunnyContext<TInput>(this);
        public IFunnyContext<TInput, TOutput> ForCalc<TInput, TOutput>() 
            => new FunnyContextSingle<TInput, TOutput>(this);
        public IFunnyContext<TInput, TOutput> ForCalcMany<TInput, TOutput>() where TOutput : new()
            => new FunnyContextMany<TInput, TOutput>(this);


        public object Calc(string expression)
        {
            var runtime = CreateRuntime(expression, AprioriTypesMap.Empty);
            if (runtime.Variables.Any(v=>!v.IsOutput))
                throw ErrorFactory.UnknownInputs(runtime.GetInputVariableUsages());
            
            runtime.Run();
            return FluentApiTools.GetClrOut(runtime);
        }
        
        public TOutput Calc<TOutput>(string expression)
        {
            var outputConverter = FunnyTypeConverters.GetOutputConverter(typeof(TOutput));
            var apriories = AprioriTypesMap.Empty;
            apriories.Add(Parser.AnonymousEquationId, outputConverter.FunnyType);

            var runtime = CreateRuntime(expression, apriories);

            if (runtime.Variables.Any(v => !v.IsOutput))
                throw ErrorFactory.UnknownInputs(runtime.GetInputVariableUsages());

            runtime.Run();
            return (TOutput)outputConverter.ToClrObject(FluentApiTools.GetOut(runtime).FunnyValue);
        }

        public object Calc<TInput>(string expression, TInput input) => ForCalc<TInput>().Calc(expression, input);
        
        public TOutput Calc<TInput, TOutput>(string expression, TInput input)=> ForCalc<TInput, TOutput>().Calc(expression, input);


        public TOutput CalcMany<TOutput>(string expression) where TOutput : new()
        {
            var apriories = AprioriTypesMap.Empty;
            var outputs   = FluentApiTools.SetupManyAprioriOutputs<TOutput>(apriories);
            var runtime = CreateRuntime(expression,apriories);
            if (runtime.Variables.Any(v=>!v.IsOutput))
                throw ErrorFactory.UnknownInputs(runtime.GetInputVariableUsages());
            
            runtime.Run();
            return FluentApiTools.CreateOutputValueFromResults<TOutput>(runtime, outputs);
        }

        public TOutput CalcMany<TInput, TOutput>(string expression, TInput input) where TOutput : new()
            => ForCalcMany<TInput, TOutput>().Calc(expression, input);
        
        internal FunnyRuntime CreateRuntime(string expression, AprioriTypesMap apriories)
        {
            IConstantList constants = null;
            if (_constantList.Any())
            {
                var cl = new ConstantList();
                foreach (var constant in _constantList)
                {
                    cl.AddConstant(constant.Item1,constant.Item2);
                }
                constants = cl;
            }

            ImmutableFunctionDictionary dic = BaseFunctions.DefaultDictionary;
            if (_concreteFunctions.Any())
                dic = dic.CloneWith(_concreteFunctions.ToArray());
                                        

            return RuntimeBuilder.Build(
                script: expression,
                constants: constants??EmptyConstantList.Instance,
                functionDictionary: dic, 
                aprioriTypesMap: apriories, 
                dialect:_dialect);
        }

    }
}