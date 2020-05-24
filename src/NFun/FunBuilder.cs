using NFun.Interpritation;
using NFun.Interpritation.Functions;
using NFun.Runtime;

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

        internal FunBuilderWithDictionary(string script, IFunctionDictionary functionDictionary)
        {
            _script = script;
            _functionDictionary = functionDictionary;
        }

        public FunRuntime Build() => RuntimeBuilder.Build(_script, _functionDictionary, new EmptyConstantList());
    }

    public class FunBuilderWithConcreteFunctions : IFunBuilder
    {
        private readonly string _script;
        private readonly FunctionDictionary _functionDictionary;

        internal FunBuilderWithConcreteFunctions(string script)
        {
            _script = script;
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

        public FunRuntime Build() => RuntimeBuilder.Build(_script, _functionDictionary, new EmptyConstantList());
    }
    public  class FunBuilder : IFunBuilder
    {
        private readonly string _text;
        public static FunBuilder With(string text) => new FunBuilder(text);
        private FunBuilder(string text)
        {
            _text = text;
        }
        public FunBuilderWithDictionary With(IFunctionDictionary dictionary)
        {
            return  new FunBuilderWithDictionary(_text, dictionary);
        }

       

        public FunBuilderWithConcreteFunctions WithFunctions(params IFunctionSignature[] functions)
        {
            FunBuilderWithConcreteFunctions builder = new FunBuilderWithConcreteFunctions(_text);
            return builder.WithFunctions(functions);
        }

        public FunRuntime Build() => RuntimeBuilder.Build(_text, BaseFunctions.CreateDefaultDictionary(), new EmptyConstantList());

        public static FunRuntime Build(string text) =>
            RuntimeBuilder.Build(text, BaseFunctions.CreateDefaultDictionary(), new EmptyConstantList());
    }
}