using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Interpretation;
using NFun.Interpretation.Functions;
using NFun.Runtime;

namespace NFun
{
    public class FunnyCalculatorBuilder
    {
        internal static  FunnyCalculatorBuilder Default  => new();
        private DialectSettings _dialect = DialectSettings.Default;
        private readonly List<(string, object)> _constantList = new();
        private readonly List<IConcreteFunction> _concreteFunctions = new();
        
        public FunnyCalculatorBuilder WithDialect(DialectSettings dialect)
        {
            _dialect = dialect;
            return this;
        }
        
        public FunnyCalculatorBuilder WithConstant(string id, object value) {
            _constantList.Add((id,value));
            return this;
        }

        public FunnyCalculatorBuilder WithFunction<Tin, TOut>(string name, Func<Tin, TOut> function)
        {
            _concreteFunctions.Add(LambdaWrapperFactory.Create(name, function));
            return this;
        }
        public FunnyCalculatorBuilder WithFunction<Tin1,Tin2,TOut>(string name, Func<Tin1,Tin2,TOut> function)
        {
            _concreteFunctions.Add(LambdaWrapperFactory.Create(name, function));
            return this;
        }
        public FunnyCalculatorBuilder WithFunction<Tin1,Tin2,Tin3,TOut>(string name, Func<Tin1,Tin2,Tin3,TOut> function)
        {
            _concreteFunctions.Add(LambdaWrapperFactory.Create(name, function));
            return this;
        }
        public FunnyCalculatorBuilder WithFunction<Tin1,Tin2,Tin3,Tin4,TOut>(string name, Func<Tin1,Tin2,Tin3,Tin4,TOut> function)
        {
            _concreteFunctions.Add(LambdaWrapperFactory.Create(name, function));
            return this;
        }
        public FunnyCalculatorBuilder WithFunction<Tin1,Tin2,Tin3,Tin4,Tin5,TOut>(string name, Func<Tin1,Tin2,Tin3,Tin4,Tin5,TOut> function)
        {
            _concreteFunctions.Add(LambdaWrapperFactory.Create(name, function));
            return this;
        }
        public FunnyCalculatorBuilder WithFunction<Tin1,Tin2,Tin3,Tin4,Tin5,Tin6,TOut>(string name, Func<Tin1,Tin2,Tin3,Tin4,Tin5,Tin6,TOut> function)
        {
            _concreteFunctions.Add(LambdaWrapperFactory.Create(name, function));
            return this;
        }
        public FunnyCalculatorBuilder WithFunction<Tin1,Tin2,Tin3,Tin4,Tin5,Tin6,Tin7,TOut>(string name, Func<Tin1,Tin2,Tin3,Tin4,Tin5,Tin6,Tin7,TOut> function)
        {
            _concreteFunctions.Add(LambdaWrapperFactory.Create(name, function));
            return this;
        }
        
        public ICalculator<TInput> BuildForCalc<TInput>() 
            => new Calculator<TInput>(this);
        public ICalculator<TInput, TOutput> BuildForCalc<TInput, TOutput>() 
            => new CalculatorSingle<TInput, TOutput>(this);
        public ICalculator<TInput, TOutput> BuildForCalcMany<TInput, TOutput>() where TOutput : new()
            => new CalculatorMany<TInput, TOutput>(this);
        public IConstantCalculator<object> BuildForCalcConstant()
            => new ConstantCalculatorSingle(this);
        public IConstantCalculator<TOutput> BuildForCalcConstant<TOutput>()
            => new ConstantCalculatorSingle<TOutput>(this);
        public IConstantCalculator<TOutput> BuildForCalcManyConstants<TOutput>() where TOutput : new() 
            => new ConstantCalculatorMany<TOutput>(this);
        
        public object Calc(string expression) => BuildForCalcConstant().Calc(expression);
       
        public TOutput Calc<TOutput>(string expression) => BuildForCalcConstant<TOutput>().Calc(expression);
        
        public object Calc<TInput>(string expression, TInput input) => BuildForCalc<TInput>().Calc(expression, input);
        
        public TOutput Calc<TInput, TOutput>(string expression, TInput input)=> BuildForCalc<TInput, TOutput>().Calc(expression, input);
        
        public TOutput CalcMany<TOutput>(string expression) where TOutput : new() => BuildForCalcManyConstants<TOutput>().Calc(expression);
        
        public TOutput CalcMany<TInput, TOutput>(string expression, TInput input) where TOutput : new()
            => BuildForCalcMany<TInput, TOutput>().Calc(expression, input);
        
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