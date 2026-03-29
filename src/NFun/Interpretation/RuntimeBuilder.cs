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
using NFun.Types;

namespace NFun.Interpretation;

internal static class RuntimeBuilder {
    internal static FunnyRuntime Build(
        string script,
        IFunctionRegistry functionRegistry,
        DialectSettings dialect,
        IConstantList constants = null,
        IAprioriTypesMap aprioriTypesMap = null,
        ICustomTypeRegistry customTypes = null) {

        var flow = Tokenizer.ToFlow(script, dialect.AllowNewlineInStrings == AllowNewlineInStrings.Deny);
        var syntaxTree = Parser.Parse(flow);

        //Set node numbers
        var setNodeNumberVisitor = new SetNodeNumberVisitor(0);
        syntaxTree.ComeOver(setNodeNumberVisitor);
        syntaxTree.MaxNodeId = setNodeNumberVisitor.LastUsedNumber;
        return Build(
            syntaxTree,
            functionRegistry,
            EnsureBuiltInConstants(constants, dialect.Converter),
            aprioriTypesMap?? EmptyAprioriTypesMap.Instance,
            customTypes, dialect);
    }

    private static IConstantList EnsureBuiltInConstants(IConstantList userConstants, FunnyConverter converter) {
        if (userConstants == null)
            return converter.TypeBehaviour is RealIsDoubleTypeBehaviour
                ? BuiltInConstantList.Double
                : BuiltInConstantList.Decimal;
        if (userConstants is ConstantList cl) {
            cl.AddBuiltIns();
            return cl;
        }
        var list = new ConstantList(converter);
        list.AddBuiltIns();
        return list;
    }

    private static FunnyRuntime Build(
        SyntaxTree syntaxTree,
        IFunctionRegistry functionsRegistry,
        IConstantList constants,
        IAprioriTypesMap aprioriTypes,
        ICustomTypeRegistry customTypes,
        DialectSettings dialect) {
        #region build user functions

        //get topology sort of the functions call
        //result is the order of functions that need to be compiled
        //functions that not references other functions have to be compiled firstly
        //Then those functions will be compiled
        //that refer to already compiled functions
        var functionSolveOrder = syntaxTree.FindFunctionSolvingOrderOrThrow();
        IUserFunction[] userFunctions;
        IFunctionRegistry functionRegistry;
        if (functionSolveOrder.Length == 0)
        {
            functionRegistry = functionsRegistry;
            userFunctions = Array.Empty<IUserFunction>();
        }
        else
        {
            userFunctions = new IUserFunction[functionSolveOrder.Length];

            var scopeFunctionDictionary = new ScopeFunctionRegistry(functionsRegistry, functionSolveOrder.Length);
            functionRegistry = scopeFunctionDictionary;
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

        var bodyTypeSolving = SolveBodyTypes(syntaxTree, constants, functionRegistry, aprioriTypes, customTypes, dialect);


        #region build body

        var variables = new VariableDictionary();
        var equations = new List<Equation>();

        foreach (var treeNode in syntaxTree.Nodes)
        {
            if (treeNode is EquationSyntaxNode node)
            {
                var equation =
                    BuildEquationAndPutItToVariables(node, functionRegistry, variables, bodyTypeSolving, dialect);
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

                var resolvedType = TypeSyntaxResolver.Resolve(varDef.TypeSyntax, customTypes);
                var variableSource = VariableSource.CreateWithStrictTypeLabel(
                    varDef.Id,
                    resolvedType,
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
        IFunctionRegistry functionRegistry,
        IAprioriTypesMap aprioriTypes,
        ICustomTypeRegistry customTypes,
        DialectSettings dialect) {

        var bodyTypeSolving = RuntimeBuilderHelper.SolveBodyOrThrow(
            syntaxTree, functionRegistry, constants, aprioriTypes, customTypes, dialect);

        var enterVisitor = new ApplyTiResultEnterVisitor(bodyTypeSolving, TicTypesConverter.Concrete);
        foreach (var syntaxNode in syntaxTree.Nodes)
        {
            //function nodes were solved above
            if (syntaxNode is UserFunctionDefinitionSyntaxNode)
                continue;

            //set types to nodes
            syntaxNode.ComeOver(enterVisitor);
        }

        return bodyTypeSolving;
    }

    private static Equation BuildEquationAndPutItToVariables(
        EquationSyntaxNode equation,
        IFunctionRegistry functionsRegistry,
        VariableDictionary mutableVariables,
        TypeInferenceResults typeInferenceResults,
        DialectSettings dialect) {
        if(TraceLog.IsEnabled)
            TraceLog.WriteLine($"\r\n==== BUILD EQUATION '{equation.Id}' ====");

        var expression = ExpressionBuilderVisitor.BuildExpression(
            node: equation.Expression,
            functions: functionsRegistry,
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
        ScopeFunctionRegistry functionsRegistry,
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
                functions: functionsRegistry,
                constants: constants,
                results: resultsBuilder,
                dialect: dialect))
                AssertChecks.Panic($"User Function '{functionSyntaxNode.Head}' was not solved due unknown reasons ");
            // solve the types. We ignore prefered types to get most common ancestor for function argument types instead of preferred type
            types = graph.Solve(ignorePrefered: true);
        }
        catch (TicException e)
        {
            throw Errors.TranslateTicError(e, functionSyntaxNode, graph, functionsRegistry);
        }

        resultsBuilder.SetResults(types);
        var typeInferenceResuls = resultsBuilder.Build();

        // Precompute default values for typed parameters BEFORE body TIC setup.
        // This is needed so call sites can create ConstantSyntaxNode with the right value.
        PrecomputeDefaultValues(functionSyntaxNode, typeInferenceResuls, functionsRegistry, dialect);

        if (!types.HasGenerics)
        {
            #region concreteFunction


            //set types to nodes
            functionSyntaxNode.ComeOver(
                enterVisitor: new ApplyTiResultEnterVisitor(
                    solving: typeInferenceResuls,
                    tiToLangTypeConverter: TicTypesConverter.Concrete));

            var funType = TicTypesConverter.Concrete.Convert(
                typeInferenceResuls.GetVariableType(functionSyntaxNode.Id + "'" + functionSyntaxNode.Args.Count));

            var returnType = funType.FunTypeSpecification.Output;
            var argTypes = funType.FunTypeSpecification.Inputs;
            if (TraceLog.IsEnabled)
                TraceLog.WriteLine($"\r\n=====> Generic {functionSyntaxNode.Id} {funType}");
            //make function prototype
            var prototype = new ConcreteUserFunctionPrototype(functionSyntaxNode.Id, returnType, argTypes);
            //add prototype to dictionary for future use
            functionsRegistry.Add(prototype);
            var function =
                functionSyntaxNode.BuildConcrete(
                    argTypes: argTypes,
                    returnType: returnType,
                    functionsRegistry: functionsRegistry,
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
                typeInferenceResuls, functionSyntaxNode, functionsRegistry,
                dialect);
            functionsRegistry.Add(function);
            if (TraceLog.IsEnabled)
                TraceLog.WriteLine($"\r\n=====> Concrete {functionSyntaxNode.Id} {function}");
            return function;
        }
    }

    private static void PrecomputeDefaultValues(
        UserFunctionDefinitionSyntaxNode functionSyntax,
        TypeInferenceResults results,
        IFunctionRegistry functions,
        DialectSettings dialect) {
        for (int i = 0; i < functionSyntax.Args.Count; i++)
        {
            var arg = functionSyntax.Args[i];
            if (!arg.HasDefault || arg.TypeSyntax is TypeSyntax.EmptyType)
                continue;
            var paramType = TypeSyntaxResolver.Resolve(arg.TypeSyntax);
            // none default → skip precomputation, use DefaultValueSyntaxNode at call site
            if (arg.DefaultValue is ConstantSyntaxNode { Value: FunnyNone })
                continue;
            try
            {
                var defaultExprNode = ExpressionBuilderVisitor.BuildExpression(
                    node: arg.DefaultValue,
                    functions: functions,
                    outputType: paramType,
                    variables: new VariableDictionary(),
                    typeInferenceResults: results,
                    typesConverter: TicTypesConverter.Concrete,
                    dialect: dialect);
                arg.PrecomputedDefaultValue = defaultExprNode.Calc();
                arg.PrecomputedDefaultType = paramType;
            }
            catch { /* conversion failed or non-constant — caller will use original expression */ }
        }
    }
}
