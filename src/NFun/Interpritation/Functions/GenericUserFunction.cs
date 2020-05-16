using System;
using System.Collections.Generic;
using System.Linq;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
using NFun.Tic.SolvingStates;
using NFun.TypeInferenceAdapter;
using NFun.TypeInferenceCalculator;
using NFun.Types;

namespace NFun.Interpritation.Functions
{
    public class GenericUserFunction : GenericFunctionBase
    {
        private readonly TypeInferenceResults _typeInferenceResults;
        private UserFunctionDefenitionSyntaxNode _syntaxNode;
        private IFunctionDicitionary _dictionary;

        private Constrains[] _constrainsMap;

        public static GenericUserFunction Create(
            TypeInferenceResults typeInferenceResults,
            UserFunctionDefenitionSyntaxNode syntaxNode,
            IFunctionDicitionary dictionary)
        {
            var ticGenerics = typeInferenceResults.Generics;
            var langConstrains = new GenericConstrains[ticGenerics.Length];
            for (int i = 0; i < ticGenerics.Length; i++)
            {
                var ticConstrains = ticGenerics[i];
                langConstrains[i] = GenericConstrains.FromTicConstrains(ticConstrains);
            }
            var ticFunName = syntaxNode.Id + "'" + syntaxNode.Args.Count;
            var ticSignature = (Fun)typeInferenceResults.GetVariableType(ticFunName);
            var signatureConverter = TicTypesConverter.GenericSignatureConverter(ticGenerics);

            var argTypes = ticSignature.Args.Select(a => signatureConverter.Convert(a)).ToArray();
            var retType = signatureConverter.Convert(ticSignature.ReturnType);
            if (TraceLog.IsEnabled)
            {
                TraceLog.WriteLine($"CREATE GENERIC FUN {syntaxNode.Id}({string.Join(",",argTypes)}):{retType}");
                TraceLog.WriteLine($"    ...where {string.Join(", ", langConstrains)}");
            }
            var function =  new GenericUserFunction(typeInferenceResults, syntaxNode, dictionary, langConstrains, retType, argTypes);
            return function;
        }

        public static void CreateSomeConcrete(GenericUserFunction function)
        {
            var varType = new VarType[function._constrainsMap.Length];

            for (var i = 0; i < function._constrainsMap.Length; i++)
            {
                var anc = function._constrainsMap[i].Ancestor ?? Primitive.Any;
                var concrete = TicTypesConverter.ToConcrete(anc.Name);
                varType[i] =concrete;
            }

            function.CreateConcrete(varType);
        }

        private GenericUserFunction(
            TypeInferenceResults typeInferenceResults,
            UserFunctionDefenitionSyntaxNode syntaxNode,
            IFunctionDicitionary dictionary,
            GenericConstrains[] constrains,
            VarType returnType,
            VarType[] argTypes
            ) : base(syntaxNode.Id, constrains, returnType, argTypes)
        {
            _typeInferenceResults = typeInferenceResults;
            _constrainsMap = _typeInferenceResults.Generics;
            _syntaxNode = syntaxNode;
            _dictionary = dictionary;
        }
        Dictionary<string, FunctionBase> _concreteFunctionsCache = new Dictionary<string, FunctionBase>();
        public override FunctionBase CreateConcrete(VarType[] concreteTypes)
        {
            var id = string.Join(",", concreteTypes);
            if (_concreteFunctionsCache.TryGetValue(id, out var alreadyExists))
                return alreadyExists;
            //set types to nodes
            var converter = TicTypesConverter.ReplaceGenericTypesConverter(_constrainsMap, concreteTypes);
            var ticSignature = _typeInferenceResults.GetVariableType(_syntaxNode.Id + "'" + _syntaxNode.Args.Count);
            var funType = converter.Convert(ticSignature);

            var returnType = funType.FunTypeSpecification.Output;
            var argTypes = funType.FunTypeSpecification.Inputs;

            //Create function prototype and put it to cache for recursive cases
            //If the function is recursive - function will take recursive prototype from cache
            var concretePrototype = new ConcreteUserFunctionPrototype(Name, returnType, argTypes);
            _concreteFunctionsCache.Add(id, concretePrototype);

            _syntaxNode.ComeOver(
                enterVisitor: new ApplyTiResultEnterVisitor(
                    solving: _typeInferenceResults,
                    tiToLangTypeConverter: converter),
                exitVisitor: new ApplyTiResultsExitVisitor());

            var function = _syntaxNode.BuildConcrete(
                argTypes:   argTypes, 
                returnType: returnType,
                functionsDictionary: _dictionary,
                results:    _typeInferenceResults, 
                converter:  converter);

            concretePrototype.SetActual(function,_syntaxNode.Interval);
            return function;
        }

        public override object Calc(object[] args) => throw new NotImplementedException();
    }
   
}