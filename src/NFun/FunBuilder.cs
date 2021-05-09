using NFun.Interpritation;
using NFun.Interpritation.Functions;
using NFun.Runtime;
using NFun.Types;

namespace NFun
{
    public abstract class FunBuilderBase:IFunBuilder
    {
        protected string _script;
        protected IFunctionDictionary _functionDictionary;
        protected IConstantList _constants;
        protected AprioriTypesMap _apriori = new();
        
        public IFunBuilder WithApriori(string s, VarType type)
        {
            _apriori.Add(s,type);
            return this;
        }

        public abstract FunRuntime Build();
    }
    public interface IFunBuilder
    {
        IFunBuilder WithApriori(string s, VarType type);
        FunRuntime Build();
    }
    public class FunBuilderWithDictionary:FunBuilderBase
    {
        internal FunBuilderWithDictionary(string script, IFunctionDictionary functionDictionary, IConstantList constants)
        {
            _script = script;
            _functionDictionary = functionDictionary;
            _constants = constants;
        }

        public override FunRuntime Build() 
            => RuntimeBuilder.Build(_script, _functionDictionary, _constants??new EmptyConstantList(), _apriori);
    }

    public class FunBuilderWithConcreteFunctions : FunBuilderBase
    {
        private readonly FlatMutableFunctionDictionary _flatMutableFunctionDictionary;

        internal FunBuilderWithConcreteFunctions(string script, IConstantList constants, AprioriTypesMap aprioriTypesMap)
        {
            _script = script;
            _constants = constants;
            _apriori = aprioriTypesMap;
            _flatMutableFunctionDictionary = BaseFunctions.CreateDefaultDictionary();
        }
        public FunBuilderWithConcreteFunctions WithFunctions(params IFunctionSignature[] functions)
        {
            foreach (var function in functions)
            {
                _flatMutableFunctionDictionary.AddOrThrow(function);
            }
            
            return this;
        }

        public override FunRuntime Build() => RuntimeBuilder.Build(_script, _flatMutableFunctionDictionary, _constants??new EmptyConstantList());
    }
    public  class FunBuilder : FunBuilderBase
    {
        public static FunBuilder With(string text) => new FunBuilder(text);
        private FunBuilder(string text) => _script = text;
        
        public FunBuilder With(IConstantList constants)
        {
            _constants = constants;
            return this;
        }
        public FunBuilderWithDictionary With(IFunctionDictionary dictionary) 
            => new(_script, dictionary, _constants);

        public FunBuilderWithConcreteFunctions WithFunctions(params IFunctionSignature[] functions)
        {
            var builder = new FunBuilderWithConcreteFunctions(_script, _constants, _apriori);
            return builder.WithFunctions(functions);
        }

        public override FunRuntime Build() 
            => RuntimeBuilder.Build(_script, BaseFunctions.DefaultDictionary, _constants??new EmptyConstantList(), _apriori);

        public static FunRuntime Build(string text) =>
            RuntimeBuilder.Build(text, BaseFunctions.DefaultDictionary, new EmptyConstantList());
    }
}