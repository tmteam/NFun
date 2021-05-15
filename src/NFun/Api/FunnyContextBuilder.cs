using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Interpritation;
using NFun.Interpritation.Functions;
using NFun.Runtime;
using NFun.Types;

namespace NFun
{
    public class FunnyContextBuilder
    {
        internal static  FunnyContextBuilder Empty  => new();
        
        private readonly List<(string, object)> _constantList = new();
        private readonly List<IConcreteFunction> _concreteFunctions = new();
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
        internal FunRuntime CreateRuntime(string expression, AprioriTypesMap apriories)
        {
            IConstantList constants = null;
            if (_constantList.Any())
            {
                var cl = new ConstantList();
                foreach (var constant in _constantList)
                {
                    var converter = FunnyTypeConverters.GetInputConverter(constant.Item2.GetType());
                    cl.AddConstant(new VarVal(constant.Item1,constant.Item2,converter.FunnyType));   
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
                aprioriTypesMap: apriories);
        }
        
        public IFunnyContext<TInput, TOutput> ForCalc<TInput, TOutput>() 
            => new FunnyContextSingle<TInput, TOutput>(this);
        public IFunnyContext<TInput, TOutput> ForCalcMany<TInput, TOutput>() where TOutput : new()
            => new FunnyContextMany<TInput, TOutput>(this);
        public IFunnyContext<TInput> ForCalc<TInput>()
            => new FunnyContext<TInput>(this);
    }
}