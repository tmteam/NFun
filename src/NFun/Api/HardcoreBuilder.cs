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
        private readonly ClassicDialectSettings _dialect;

        internal HardcoreBuilder()
        {
            _immutableFunctionDictionary = BaseFunctions.DefaultDictionary;
            _constants = new ConstantList();
            _apriori = AprioriTypesMap.Empty;
            _dialect = ClassicDialectSettings.Default;
        }
        
        private HardcoreBuilder(
            ImmutableFunctionDictionary immutableFunctionDictionary, 
            IConstantList constants,
            AprioriTypesMap apriori,
            ClassicDialectSettings dialect)
        {
            _dialect = dialect;
            _apriori = apriori;
            _immutableFunctionDictionary = immutableFunctionDictionary;
            _constants = constants;
        }

        public HardcoreBuilder WithDialect(ClassicDialectSettings dialect) => 
            new(_immutableFunctionDictionary, _constants, _apriori, dialect);
        public HardcoreBuilder WithConstant<T>(string id, T clrValue) => 
            new(_immutableFunctionDictionary, _constants.CloneWith((id, clrValue)), _apriori, _dialect);
        public HardcoreBuilder WithConstants(params (string, object)[] funValues) =>
            new(_immutableFunctionDictionary, _constants.CloneWith(funValues), _apriori, _dialect);
        public HardcoreBuilder WithApriori(string id, FunnyType type) => 
            new(_immutableFunctionDictionary, _constants, _apriori.CloneWith(id, type), _dialect);
        public HardcoreBuilder WithApriori<T>(string id) =>
            WithApriori(id, FunnyTypeConverters.GetInputConverter(typeof(T)).FunnyType);
        public HardcoreBuilder WithFunction(IFunctionSignature function) => 
            new( _immutableFunctionDictionary.CloneWith(function), _constants,  _apriori, _dialect);
        public HardcoreBuilder WithFunctions(params IFunctionSignature[] functions) => 
            new( _immutableFunctionDictionary.CloneWith(functions), _constants,  _apriori, _dialect);
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
        
        public FunnyRuntime Build(string script) => 
            RuntimeBuilder.Build(script, _immutableFunctionDictionary, _dialect, _constants, _apriori);
    }
}