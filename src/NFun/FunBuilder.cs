using NFun.Interpritation;
using NFun.Interpritation.Functions;
using NFun.Runtime;
using NFun.Types;

namespace NFun
{
    public interface IFunBuilder
    {
        FunRuntime Build();
    }
    public class FunBuilderWithDictionary:IFunBuilder
    {
        private readonly string _script;
        private readonly IFunctionDictionary _functionDictionary;
        private readonly IConstantList _constants;

        internal FunBuilderWithDictionary(string script, IFunctionDictionary functionDictionary, IConstantList constants)
        {
            _script = script;
            _functionDictionary = functionDictionary;
            _constants = constants;
        }

        public FunRuntime Build() => RuntimeBuilder.Build(_script, _functionDictionary, _constants??new EmptyConstantList());
    }

    public class FunBuilderWithConcreteFunctions : IFunBuilder
    {
        private readonly string _script;
        private readonly IConstantList _constants;
        private readonly FunctionDictionary _functionDictionary;
        private readonly AprioriTypesMap _aprioriTypesMap; 

        internal FunBuilderWithConcreteFunctions(string script, IConstantList constants, AprioriTypesMap aprioriTypesMap)
        {
            _script = script;
            _constants = constants;
            _aprioriTypesMap = aprioriTypesMap;
            _functionDictionary = BaseFunctions.CreateDefaultDictionary();
        }
        public FunBuilderWithConcreteFunctions WithFunctions(params IFunctionSignature[] functions)
        {
            foreach (var function in functions)
            {
                _functionDictionary.AddOrThrow(function);
            }
            
            return this;
        }

        public FunRuntime Build() => RuntimeBuilder.Build(_script, _functionDictionary, _constants??new EmptyConstantList());
    }
    public  class FunBuilder : IFunBuilder
    {
        private readonly string _text;
        
        
        public static FunBuilder With(string text) => new FunBuilder(text);
        private FunBuilder(string text) => _text = text;

        private IConstantList _constants = null;
        private readonly AprioriTypesMap _aprioriTypesMap = new AprioriTypesMap(); 
        public FunBuilder With(IConstantList constants)
        {
            _constants = constants;
            return this;
        }
        public FunBuilderWithDictionary With(IFunctionDictionary dictionary) 
            => new FunBuilderWithDictionary(_text, dictionary, _constants);

        public FunBuilderWithConcreteFunctions WithFunctions(params IFunctionSignature[] functions)
        {
            var builder = new FunBuilderWithConcreteFunctions(_text, _constants, _aprioriTypesMap);
            return builder.WithFunctions(functions);
        }

        public FunRuntime Build() 
            => RuntimeBuilder.Build(_text, BaseFunctions.DefaultDictionary, _constants??new EmptyConstantList(), _aprioriTypesMap);

        public static FunRuntime Build(string text) =>
            RuntimeBuilder.Build(text, BaseFunctions.DefaultDictionary, new EmptyConstantList());
        
        public FunBuilder WithAprioriInput(string s, VarType type)
        {
            _aprioriTypesMap.InputTypesMap.Add(s,type);
            return this;
        }

        public FunBuilder WithAprioriOutput(string s, VarType type)
        {
            _aprioriTypesMap.OutputTypesMap.Add(s,type);
            return this;
        }
    }
}