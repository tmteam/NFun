using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Interpritation;
using NFun.Interpritation.Functions;
using NFun.Runtime;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;

namespace NFun
{
    public  class FunBuilder
    {
        private readonly string _text;

        public static FunBuilder With(string text) => new FunBuilder(text);

        private FunBuilder(string text)
        {
            _text = text;
        }

        readonly List<FunctionBase> _functions = new List<FunctionBase>();
        readonly List<GenericFunctionBase> _genericFunctions= new List<GenericFunctionBase>();
        public FunBuilder WithFunctions(params FunctionBase[] functions)
        {
            _functions.AddRange(functions);
            return this;
        }
        public FunBuilder WithFunctions(params GenericFunctionBase[] functions)
        {
            _genericFunctions.AddRange(functions);
            return this;
        }

        public FunRuntime Build()
        {
            var flow = Tokenizer.ToFlow(_text);
            var syntaxTree = Parser.Parse(flow);

            //Set node numbers
            syntaxTree.ComeOver(new SetNodeNumberVisitor());
            
            var functionsDictionary = CreateFunctionsDictionary();
            
            return RuntimeBuilder.Build(syntaxTree, functionsDictionary);
        }

        /// <summary>
        /// Creates functions dictionary that contains build in and custom functions
        /// </summary>
        private FunctionsDictionary CreateFunctionsDictionary()
        {
            var functionsDictionary = new FunctionsDictionary();
            foreach (var predefinedFunction in _functions.Concat(BaseFunctions.ConcreteFunctions))
                functionsDictionary.Add(predefinedFunction);
            foreach (var genericFunctionBase in _genericFunctions.Concat(BaseFunctions.GenericFunctions))
                functionsDictionary.Add(genericFunctionBase);
            return functionsDictionary;
        }        
        
        public static FunRuntime BuildDefault(string text)
            => FunBuilder.With(text).Build();
    }
}