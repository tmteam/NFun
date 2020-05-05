using System;
using System.Linq;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
using NFun.Tic.SolvingStates;
using NFun.Tokenization;
using NFun.TypeInferenceAdapter;
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
            return new GenericUserFunction(typeInferenceResults, syntaxNode, dictionary, langConstrains, retType, argTypes);
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

        public override FunctionBase CreateConcrete(VarType[] concreteTypes)
        {
            //set types to nodes
            var converter = TicTypesConverter.ReplaceGenericTypesConverter(_constrainsMap, concreteTypes);

            _syntaxNode.ComeOver(
                enterVisitor: new ApplyTiResultEnterVisitor(
                    solving: _typeInferenceResults,
                    tiToLangTypeConverter: converter),
                exitVisitor: new ApplyTiResultsExitVisitor());

            var funType = converter.Convert(
                _typeInferenceResults.GetVariableType(_syntaxNode.Id + "'" + _syntaxNode.Args.Count));

            var returnType = funType.FunTypeSpecification.Output;
            var argTypes   = funType.FunTypeSpecification.Inputs;

            var function = _syntaxNode.BuildConcrete(
                argTypes:   argTypes, 
                returnType: returnType,
                functionsDictionary: _dictionary,
                results:    _typeInferenceResults, 
                converter:  converter);
            return function;
        }

        public override object Calc(object[] args) => throw new NotImplementedException();
    }
   
}