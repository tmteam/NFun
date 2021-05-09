using System;
using NFun.Interpritation;
using NFun.Interpritation.Functions;
using NFun.Runtime;
using NFun.Types;

namespace NFun
{
    public class HardcoreBuilder
    {
        private readonly FlatMutableFunctionDictionary _flatMutableFunctionDictionary;
        private readonly IConstantList _constants;
        private readonly AprioriTypesMap _apriori;
        public HardcoreBuilder()
        {
            _flatMutableFunctionDictionary = BaseFunctions.CreateDefaultDictionary();
            _constants = new ConstantList();
            _apriori = new AprioriTypesMap();
        }
        private HardcoreBuilder(FlatMutableFunctionDictionary flatMutableFunctionDictionary, IConstantList constants,AprioriTypesMap apriori)
        {
            _apriori = apriori;
            _flatMutableFunctionDictionary = flatMutableFunctionDictionary;
            _constants = constants;
        }
        public HardcoreBuilder WithConstant<T>(string id, T clrValue) {
            var converter = FunnyTypeConverters.GetInputConverter(typeof(T));
            var varval = new VarVal(id, converter.ToFunObject(clrValue), converter.FunnyType);
            return new(_flatMutableFunctionDictionary, _constants.CloneWith(varval), _apriori);
        }
        public HardcoreBuilder WithConstants(params  VarVal[] funValues) => 
            new(_flatMutableFunctionDictionary, _constants.CloneWith(funValues), _apriori);
        public HardcoreBuilder WithApriori(string id, VarType type) => 
            new(_flatMutableFunctionDictionary, _constants, _apriori.CloneWith(id, type));
        public HardcoreBuilder WithApriori<T>(string id) =>
            WithApriori(id, FunnyTypeConverters.GetInputConverter(typeof(T)).FunnyType);
        public HardcoreBuilder WithFunctions(params IFunctionSignature[] functions) => 
            new( _flatMutableFunctionDictionary.CloneWith(functions), _constants,  _apriori);
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
            RuntimeBuilder.Build(script, _flatMutableFunctionDictionary, _constants, _apriori);
    }
}