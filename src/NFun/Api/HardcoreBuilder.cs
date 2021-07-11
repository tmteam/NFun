using System;
using NFun.Interpretation;
using NFun.Interpretation.Functions;
using NFun.Runtime;
using NFun.Types;

namespace NFun
{
    public class HardcoreBuilder
    {
        private readonly ImmutableFunctionDictionary _immutableFunctionDictionary;
        private readonly IConstantList _constants;
        private readonly AprioriTypesMap _apriori;
        internal HardcoreBuilder()
        {
            _immutableFunctionDictionary = BaseFunctions.DefaultDictionary;
            _constants = new ConstantList();
            _apriori = AprioriTypesMap.Empty;
        }
        private HardcoreBuilder(ImmutableFunctionDictionary immutableFunctionDictionary, IConstantList constants,AprioriTypesMap apriori)
        {
            _apriori = apriori;
            _immutableFunctionDictionary = immutableFunctionDictionary;
            _constants = constants;
        }
        
        public HardcoreBuilder WithConstant<T>(string id, T clrValue) => 
            new(_immutableFunctionDictionary, _constants.CloneWith((id, clrValue)), _apriori);
        public HardcoreBuilder WithConstants(params (string, object)[] funValues) =>
            new(_immutableFunctionDictionary, _constants.CloneWith(funValues), _apriori);
        public HardcoreBuilder WithApriori(string id, FunnyType type) => 
            new(_immutableFunctionDictionary, _constants, _apriori.CloneWith(id, type));
        public HardcoreBuilder WithApriori<T>(string id) =>
            WithApriori(id, FunnyTypeConverters.GetInputConverter(typeof(T)).FunnyType);
        public HardcoreBuilder WithFunction(IFunctionSignature function) => 
            new( _immutableFunctionDictionary.CloneWith(function), _constants,  _apriori);
        public HardcoreBuilder WithFunctions(params IFunctionSignature[] functions) => 
            new( _immutableFunctionDictionary.CloneWith(functions), _constants,  _apriori);
        public HardcoreBuilder WithFunctions(ImmutableFunctionDictionary functionDictionary) =>
            //todo - deletes previous added functions
            new( functionDictionary, _constants,  _apriori);
        public HardcoreBuilder WithFunction<Tin, TOut>(string name, Func<Tin, TOut> function) => 
            WithFunctions(LambdaWrapperFactory.Create(name, function));
        public HardcoreBuilder WithFunction<Tin1,Tin2, TOut>(string name, Func<Tin1,Tin2, TOut> function) => 
            WithFunctions(LambdaWrapperFactory.Create(name, function));
        public HardcoreBuilder WithFunction<Tin1,Tin2,Tin3, TOut>(string name, Func<Tin1,Tin2,Tin3, TOut> function) => 
            WithFunctions(LambdaWrapperFactory.Create(name, function));
        public HardcoreBuilder WithFunction<Tin1,Tin2,Tin3,Tin4, TOut>(string name, Func<Tin1,Tin2,Tin3,Tin4, TOut> function) => 
            WithFunctions(LambdaWrapperFactory.Create(name, function));
        public HardcoreBuilder WithFunction<Tin1,Tin2,Tin3,Tin4,Tin5, TOut>(string name, Func<Tin1,Tin2,Tin3,Tin4,Tin5, TOut> function) => 
            WithFunctions(LambdaWrapperFactory.Create(name, function));
        public HardcoreBuilder WithFunction<Tin1,Tin2,Tin3,Tin4,Tin5,Tin6, TOut>(string name, Func<Tin1,Tin2,Tin3,Tin4,Tin5,Tin6, TOut> function) => 
            WithFunctions(LambdaWrapperFactory.Create(name, function));
        public HardcoreBuilder WithFunction<Tin1,Tin2,Tin3,Tin4,Tin5,Tin6,Tin7, TOut>(string name, Func<Tin1,Tin2,Tin3,Tin4,Tin5,Tin6,Tin7, TOut> function) => 
            WithFunctions(LambdaWrapperFactory.Create(name, function));
        
        public FunRuntime Build(string script) => 
            RuntimeBuilder.Build(script, _immutableFunctionDictionary, _constants, _apriori);
    }
}