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

namespace NFun.Interpretation;

internal static class RuntimeBuilder {
    internal static FunnyRuntime Build(
        string script,
        IFunctionDictionary functionDictionary,
        DialectSettings dialect,
        IConstantList constants = null,
        IAprioriTypesMap aprioriTypesMap = null) {

        var flow = Tokenizer.ToFlow(script);
        var syntaxTree = Parser.Parse(flow);

        //Set node numbers
        var setNodeNumberVisitor = new SetNodeNumberVisitor(0);
        syntaxTree.ComeOver(setNodeNumberVisitor);
        syntaxTree.MaxNodeId = setNodeNumberVisitor.LastUsedNumber;
        return Build(
            syntaxTree,
            functionDictionary,
            constants ?? EmptyConstantList.Instance,
            aprioriTypesMap?? EmptyAprioriTypesMap.Instance,
            dialect);
    }

    private static FunnyRuntime Build(
        SyntaxTree syntaxTree,
        IFunctionDictionary functionsDictionary,
        IConstantList constants,
        IAprioriTypesMap aprioriTypes,
        DialectSettings dialect) {
        #region build user functions

        //get topology sort of the functions call
        //result is the order of functions that need to be compiled
        //functions that not references other functions have to be compiled firstly
        //Then those functions will be compiled
        //that refer to already compiled functions
        var functionSolveOrder = syntaxTree.FindFunctionSolvingOrderOrThrow();
        IUserFunction[] userFunctions;
        IFunctionDictionary functionDictionary;
        if (functionSolveOrder.Length == 0)
        {
            functionDictionary = functionsDictionary;
            userFunctions = Array.Empty<IUserFunction>();
        }
        else
        {
            userFunctions = new IUserFunction[functionSolveOrder.Length];

            var scopeFunctionDictionary = new ScopeFunctionDictionary(functionsDictionary, functionSolveOrder.Length);
            functionDictionary = scopeFunctionDictionary;
            //build user functions
            for (var i = 0; i < functionSolveOrder.Length; i++)
            {
                if (dialect.AllowUserFunctions == AllowUserFunctions.DenyUserFunctions)
                    throw Errors.UserFunctionIsDenied(functionSolveOrder[i].Interval);

                if(dialect.AllowUserFunctions == AllowUserFunctions.DenyRecursive && functionSolveOrder[i].IsRecursive)
                    throw Errors.RecursiveUserFunctionIsDenied(functionSolveOrder[i].Interval);

                userFunctions[i] = BuildFunctionAndPutItToDictionary(
                    functionSolveOrder[i],
                    constants,
                    scopeFunctionDictionary,
                    dialect);
            }
        }

        #endregion


        if(TraceLog.IsEnabled)
            TraceLog.WriteLine($"\r\n==== BUILD BODY ====");

        var bodyTypeSolving = SolveBodyTypes(syntaxTree, constants, functionDictionary, aprioriTypes, dialect);


        #region build body

        var variables = new VariableDictionary();
        var equations = new List<Equation>();

        foreach (var treeNode in syntaxTree.Nodes)
        {
            if (treeNode is EquationSyntaxNode node)
            {
                var equation =
                    BuildEquationAndPutItToVariables(node, functionDictionary, variables, bodyTypeSolving, dialect);
                equations.Add(equation);

                if (!variables.TryAdd(equation.OutputVariableSource))
                {
                    var alreadyExist = variables.GetOrNull(equation.OutputVariableSource.Name);
                    var usage = equations.FindFirstUsageOrNull(alreadyExist);
                    //some equation referenced the source before
                    if (equation.OutputVariableSource.IsOutput)
                        throw Errors.OutputNameWithDifferentCase(equation.Id, usage?.Interval ?? equation.Expression.Interval);
                    else
                        throw Errors.CannotUseOutputValueBeforeItIsDeclared(alreadyExist, usage);
                }

                if (Helper.DoesItLooksLikeSuperAnonymousVariable(equation.Id))
                    throw Errors.CannotUseSuperAnonymousVariableHere(
                        new Interval(node.Interval.Start, node.Interval.Start + node.Id.Length),
                        equation.Id);
                if (TraceLog.IsEnabled)
                    TraceLog.WriteLine($"\r\nEQUATION: {equation.Id}:{equation.Expression.Type} = ... \r\n");
            }
            else if (treeNode is VarDefinitionSyntaxNode varDef)
            {
                if (Helper.DoesItLooksLikeSuperAnonymousVariable(varDef.Id))
                    throw Errors.CannotUseSuperAnonymousVariableHere(varDef.Interval, varDef.Id);

                var variableSource = VariableSource.CreateWithStrictTypeLabel(
                    varDef.Id,
                    varDef.FunnyType,
                    varDef.Interval,
                    FunnyVarAccess.Input,
                    dialect.Converter,
                    varDef.Attributes);
                if (!variables.TryAdd(variableSource))
                {
                    var alreadyExisted = variables.GetOrNull(variableSource.Name);
                    var usage = equations.FindFirstUsageOrThrow(alreadyExisted);
                    throw Errors.VariableIsDeclaredAfterUsing(variableSource.Name, usage.Interval);
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
                // We have to build it at least once to search all possible errors and figure out - is it recursive or not
                GenericUserFunction.CreateSomeConcrete(generic);
            }

            var source = variables.GetOrNull(userFunction.Name);
            if(source!=null)
            {
                var usage = equations.FindFirstUsageOrNull(source);
                throw Errors.FunctionNameAndVariableNameConflict(source, usage);
            }
        }

        return new FunnyRuntime(equations, variables, userFunctions, dialect.Converter);
    }

    private static TypeInferenceResults SolveBodyTypes(
        SyntaxTree syntaxTree,
        IConstantList constants,
        IFunctionDictionary functionDictionary,
        IAprioriTypesMap aprioriTypes,
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
        VariableDictionary mutableVariables,
        TypeInferenceResults typeInferenceResults,
        DialectSettings dialect) {
        if(TraceLog.IsEnabled)
            TraceLog.WriteLine($"\r\n==== BUILD EQUATION '{equation.Id}' ====");

        var expression = ExpressionBuilderVisitor.BuildExpression(
            node: equation.Expression,
            functions: functionsDictionary,
            outputType: equation.OutputType,
            variables: mutableVariables,
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
                typeBehaviour: dialect.Converter,
                attributes: equation.Attributes
            );
        else
            outputVariableSource = VariableSource.CreateWithoutStrictTypeLabel(
                name: equation.Id,
                type: equation.OutputType,
                access: FunnyVarAccess.Output,
                dialect.Converter,
                equation.Attributes
            );

        var itVariable = mutableVariables
            .GetAll()
            .FirstOrDefault(c => Helper.DoesItLooksLikeSuperAnonymousVariable(c.Name));
        if (itVariable!=null)
        {
            var expressionNode = expression.FindFirstUsageOrNull(itVariable);
            throw Errors.CannotUseSuperAnonymousVariableHere(expressionNode.Interval, itVariable.Name);
        }

        if(outputVariableSource.Type != expression.Type)
            AssertChecks.Panic("fitless");

        return new Equation(equation.Id, expression, outputVariableSource);
    }


    private static IUserFunction BuildFunctionAndPutItToDictionary(
        UserFunctionDefinitionSyntaxNode functionSyntaxNode,
        IConstantList constants,
        ScopeFunctionDictionary functionsDictionary,
        DialectSettings dialect) {

        if(TraceLog.IsEnabled)
            TraceLog.WriteLine($"\r\n==== BUILD {functionSyntaxNode.Id}(..) ====");

        ////introduce function variable
        var graph = new GraphBuilder();
        var resultsBuilder = new TypeInferenceResultsBuilder();
        ITicResults types;

        try
        {
            if(!TicSetupVisitor.SetupTicForUserFunction(
                userFunctionNode: functionSyntaxNode,
                ticGraph: graph,
                functions: functionsDictionary,
                constants: constants,
                results: resultsBuilder,
                dialect: dialect))
                AssertChecks.Panic($"User Function '{functionSyntaxNode.Head}' was not solved due unknown reasons ");
            // solve the types. We ignore prefered types to get most common ancestor for function argument types instead of preferred type
            types = graph.Solve(ignorePrefered: true);
        }
        catch (TicException e)
        {
            throw Errors.TranslateTicError(e, functionSyntaxNode, graph);
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
            functionsDictionary.Add(prototype);
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
            functionsDictionary.Add(function);
            if (TraceLog.IsEnabled)
                TraceLog.WriteLine($"\r\n=====> Concrete {functionSyntaxNode.Id} {function}");
            return function;
        }
    }
}
