using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Interpretation;
using NFun.Interpretation.Functions;
using NFun.Runtime;

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
        
        public IFunnyContext<TInput, TOutput> ForCalc<TInput, TOutput>() 
            => new FunnyContextSingle<TInput, TOutput>(this);
        public IFunnyContext<TInput, TOutput> ForCalcMany<TInput, TOutput>() where TOutput : new()
            => new FunnyContextMany<TInput, TOutput>(this);

    }
}