using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Exceptions;
using NFun.Interpretation.Functions;
using NFun.ParseErrors;
using NFun.Runtime;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
using NFun.Tic;
using NFun.Tic.Errors;
using NFun.Tokenization;
using NFun.TypeInferenceAdapter;

namespace NFun.Interpretation {

internal static class RuntimeBuilder {
    private static readonly List<IFunctionSignature> EmptyUserFunctionsList
        = new();

    internal static FunnyRuntime Build(
        string script,
        IFunctionDictionary functionDictionary,
        DialectSettings dialect,
        IConstantList constants = null,
        AprioriTypesMap aprioriTypesMap = null) {
        aprioriTypesMap ??= new AprioriTypesMap();

        var flow = Tokenizer.ToFlow(script);
        var syntaxTree = Parser.Parse(flow);

        //Set node numbers
        var setNodeNumberVisitor = new SetNodeNumberVisitor();
        syntaxTree.ComeOver(setNodeNumberVisitor);
        syntaxTree.MaxNodeId = setNodeNumberVisitor.LastUsedNumber;
        return Build(
            syntaxTree,
            functionDictionary,
            constants ?? EmptyConstantList.Instance,
            aprioriTypesMap,
            dialect);
    }

    private static FunnyRuntime Build(
        SyntaxTree syntaxTree,
        IFunctionDictionary functionsDictionary,
        IConstantList constants,
        AprioriTypesMap aprioriTypesMap,
        DialectSettings dialect) {
        #region build user functions

        //get topology sort of the functions call
        //result is the order of functions that need to be compiled
        //functions that not references other functions have to be compiled firstly
        //Then those functions will be compiled
        //that refer to already compiled functions
        var functionSolveOrder = syntaxTree.FindFunctionSolvingOrderOrThrow();
        List<IFunctionSignature> userFunctions;
        IFunctionDictionary functionDictionary;
        if (functionSolveOrder.Length == 0)
        {
            functionDictionary = functionsDictionary;
            userFunctions = EmptyUserFunctionsList;
        }
        else
        {
            userFunctions = new List<IFunctionSignature>();

            var scopeFunctionDictionary = new ScopeFunctionDictionary(functionsDictionary);
            functionDictionary = scopeFunctionDictionary;
            //build user functions
            foreach (var functionSyntaxNode in functionSolveOrder)
            {
                //todo list capacity
                var userFun = BuildFunctionAndPutItToDictionary(
                    functionSyntaxNode: functionSyntaxNode,
                    constants: constants,
                    functionsDictionary: scopeFunctionDictionary,
                    dialect: dialect);
                userFunctions.Add(userFun);
            }
        }

        #endregion


        var bodyTypeSolving = SolveBodyTypes(syntaxTree, constants, functionDictionary, aprioriTypesMap, dialect);


        #region build body

        var variables = new VariableDictionary(dialect.TypeBehaviour);
        var equations = new List<Equation>();

        foreach (var treeNode in syntaxTree.Nodes)
        {
            if (treeNode is EquationSyntaxNode node)
            {
                var equation =
                    BuildEquationAndPutItToVariables(node, functionDictionary, variables, bodyTypeSolving, dialect);
                equations.Add(equation);
                if (Helper.DoesItLooksLikeSuperAnonymousVariable(equation.Id))
                    throw Errors.CannotUseSuperAnonymousVariableHere(new Interval(node.Interval.Start, node.Interval.Start + node.Id.Length));
                if (TraceLog.IsEnabled)
                    TraceLog.WriteLine($"\r\nEQUATION: {equation.Id}:{equation.Expression.Type} = ... \r\n");
            }
            else if (treeNode is VarDefinitionSyntaxNode varDef)
            {
                if (Helper.DoesItLooksLikeSuperAnonymousVariable(varDef.Id))
                    throw Errors.CannotUseSuperAnonymousVariableHere(varDef.Interval);

                var variableSource = VariableSource.CreateWithStrictTypeLabel(
                    varDef.Id,
                    varDef.FunnyType,
                    varDef.Interval,
                    FunnyVarAccess.Input,
                    dialect.TypeBehaviour,
                    varDef.Attributes);
                if (!variables.TryAdd(variableSource))
                {
                    var allUsages = variables.GetUsages(variableSource.Name);
                    throw Errors.VariableIsDeclaredAfterUsing(allUsages);
                }

                if (TraceLog.IsEnabled)
                    TraceLog.WriteLine($"\r\nVARIABLE: {variableSource.Name}:{variableSource.Type} = ... \r\n");
            }
            else if (treeNode is UserFunctionDefinitionSyntaxNode)
                continue; //user function was built above
            else
                throw new InvalidOperationException($"Type {treeNode} is not supported as tree root");
        }

        #endregion


        foreach (var userFunction in userFunctions)
        {
            if (userFunction is GenericUserFunction generic && generic.BuiltCount == 0)
            {
                // Generic function is declared but concrete was not built.
                // We have to build it at least once to search all possible errors
                GenericUserFunction.CreateSomeConcrete(generic);
            }

            if (variables.TryGetUsages(userFunction.Name, out var usage))
            {
                throw Errors.FunctionNameAndVariableNameConflict(usage);
            }
        }

        return new FunnyRuntime(equations, variables, dialect.TypeBehaviour);
    }


    private static TypeInferenceResults SolveBodyTypes(
        SyntaxTree syntaxTree,
        IConstantList constants,
        IFunctionDictionary functionDictionary,
        AprioriTypesMap aprioriTypes,
        DialectSettings dialect) {
        var bodyTypeSolving = RuntimeBuilderHelper.SolveBodyOrThrow(
            syntaxTree, functionDictionary, constants, aprioriTypes, dialect);
        
        var enterVisitor = new ApplyTiResultEnterVisitor(bodyTypeSolving, TicTypesConverter.Concrete);
        var exitVisitor = new ApplyTiResultsExitVisitor();
        foreach (var syntaxNode in syntaxTree.Nodes)
        {
            //function nodes were solved above
            if (syntaxNode is UserFunctionDefinitionSyntaxNode)
                continue;

            //set types to nodes
            syntaxNode.ComeOver(enterVisitor, exitVisitor);
        }

        return bodyTypeSolving;
    }

    private static Equation BuildEquationAndPutItToVariables(
        EquationSyntaxNode equation,
        IFunctionDictionary functionsDictionary,
        VariableDictionary variables,
        TypeInferenceResults typeInferenceResults,
        DialectSettings dialect) {
        var expression = ExpressionBuilderVisitor.BuildExpression(
            node: equation.Expression,
            functions: functionsDictionary,
            outputType: equation.OutputType,
            variables: variables,
            typeInferenceResults: typeInferenceResults,
            typesConverter: TicTypesConverter.Concrete,
            dialect: dialect);

        VariableSource outputVariableSource;
        if (equation.OutputTypeSpecified)
            outputVariableSource = VariableSource.CreateWithStrictTypeLabel(
                name: equation.Id,
                type: equation.OutputType,
                typeSpecificationIntervalOrNull: equation.TypeSpecificationOrNull.Interval,
                access: FunnyVarAccess.Output,
                typeBehaviour: dialect.TypeBehaviour,
                attributes: equation.Attributes
            );
        else
            outputVariableSource = VariableSource.CreateWithoutStrictTypeLabel(
                name: equation.Id,
                type: equation.OutputType,
                access: FunnyVarAccess.Output,
                dialect.TypeBehaviour,
                equation.Attributes
            );

        var itVariable = variables.GetSuperAnonymousVariableOrNull();
        if (itVariable != null)
            throw Errors.CannotUseSuperAnonymousVariableHere(itVariable.Usages.First().Interval);


        if (!variables.TryAdd(outputVariableSource))
        {
            //some equation referenced the source before
            var usages = variables.GetUsages(equation.Id);
            if (usages.Source.IsOutput)
                throw Errors.OutputNameWithDifferentCase(equation.Id, equation.Expression.Interval);
            else
                throw Errors.CannotUseOutputValueBeforeItIsDeclared(usages);
        }


        //ReplaceInputType
        if (outputVariableSource.Type != expression.Type)
            throw new NFunImpossibleException("fitless");
        return new Equation(equation.Id, expression, outputVariableSource);
    }


    private static IFunctionSignature BuildFunctionAndPutItToDictionary(
        UserFunctionDefinitionSyntaxNode functionSyntaxNode,
        IConstantList constants,
        ScopeFunctionDictionary functionsDictionary,
        DialectSettings dialect) {
#if DEBUG
        TraceLog.WriteLine($"\r\n====BUILD {functionSyntaxNode.Id}(..) ====");
#endif
        ////introduce function variable
        var graphBuider = new GraphBuilder();
        var resultsBuilder = new TypeInferenceResultsBuilder();
        ITicResults types;

        try
        {
            if (!TicSetupVisitor.SetupTicForUserFunction(
                userFunctionNode: functionSyntaxNode,
                ticGraph: graphBuider,
                functions: functionsDictionary,
                constants: constants,
                results: resultsBuilder,
                dialect: dialect))
                throw Errors.FunctionIsNotSolved(functionSyntaxNode);

            // solve the types
            types = graphBuider.Solve();
        }
        catch (TicException e)
        {
            throw Errors.TranslateTicError(e, functionSyntaxNode);
        }

        resultsBuilder.SetResults(types);
        var typeInferenceResuls = resultsBuilder.Build();

        if (!types.HasGenerics)
        {
            #region concreteFunction

            //set types to nodes
            functionSyntaxNode.ComeOver(
                enterVisitor: new ApplyTiResultEnterVisitor(
                    solving: typeInferenceResuls,
                    tiToLangTypeConverter: TicTypesConverter.Concrete),
                exitVisitor: new ApplyTiResultsExitVisitor());

            var funType = TicTypesConverter.Concrete.Convert(
                typeInferenceResuls.GetVariableType(functionSyntaxNode.Id + "'" + functionSyntaxNode.Args.Count));

            var returnType = funType.FunTypeSpecification.Output;
            var argTypes = funType.FunTypeSpecification.Inputs;
            if (TraceLog.IsEnabled)
                TraceLog.WriteLine($"\r\n=====> Generic {functionSyntaxNode.Id} {funType}");
            //make function prototype
            var prototype = new ConcreteUserFunctionPrototype(functionSyntaxNode.Id, returnType, argTypes);
            //add prototype to dictionary for future use
            functionsDictionary.TryAdd(prototype);
            var function =
                functionSyntaxNode.BuildConcrete(
                    argTypes: argTypes,
                    returnType: returnType,
                    functionsDictionary: functionsDictionary,
                    results: typeInferenceResuls,
                    converter: TicTypesConverter.Concrete,
                    dialect: dialect);

            prototype.SetActual(function);
            return function;

            #endregion
        }
        else
        {
            var function = GenericUserFunction.Create(
                typeInferenceResuls, functionSyntaxNode, functionsDictionary,
                dialect);
            functionsDictionary.TryAdd(function);
            if (TraceLog.IsEnabled)
                TraceLog.WriteLine($"\r\n=====> Concrete {functionSyntaxNode.Id} {function}");
            return function;
        }
    }
}

}