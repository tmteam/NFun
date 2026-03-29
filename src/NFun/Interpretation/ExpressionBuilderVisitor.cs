using System;
using System.Linq;
using NFun.Exceptions;
using NFun.Functions;
using NFun.Interpretation.Functions;
using NFun.Interpretation.Nodes;
using NFun.ParseErrors;
using NFun.Runtime;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;
using NFun.TypeInferenceAdapter;
using NFun.Types;

namespace NFun.Interpretation;

using System;

internal sealed class ExpressionBuilderVisitor : ISyntaxNodeVisitor<IExpressionNode> {
    private readonly IFunctionRegistry _functions;
    private readonly VariableDictionary _variables;
    private readonly TypeInferenceResults _typeInferenceResults;
    private readonly TicTypesConverter _typesConverter;
    private readonly DialectSettings _dialect;

    private static IExpressionNode BuildExpression(
        ISyntaxNode node,
        IFunctionRegistry functions,
        VariableDictionary variables,
        TypeInferenceResults typeInferenceResults,
        TicTypesConverter typesConverter,
        DialectSettings dialect) =>
        node.Accept(
            new ExpressionBuilderVisitor(
                functions, variables, typeInferenceResults, typesConverter, dialect));

    internal static IExpressionNode BuildExpression(
        ISyntaxNode node,
        IFunctionRegistry functions,
        FunnyType outputType,
        VariableDictionary variables,
        TypeInferenceResults typeInferenceResults,
        TicTypesConverter typesConverter,
        DialectSettings dialect) {
        var result = node.Accept(
            new ExpressionBuilderVisitor(functions, variables, typeInferenceResults, typesConverter, dialect));
        if (result.Type == outputType)
            return result;
        var converter =
            VarTypeConverter.GetConverterOrThrow(dialect.Converter.TypeBehaviour, result.Type, outputType,
                node.Interval);

        return new CastExpressionNode(result, outputType, converter, node.Interval);
    }

    private ExpressionBuilderVisitor(
        IFunctionRegistry functions,
        VariableDictionary variables,
        TypeInferenceResults typeInferenceResults,
        TicTypesConverter typesConverter,
        DialectSettings dialect) {
        _dialect = dialect;
        _functions = functions;
        _variables = variables;
        _typeInferenceResults = typeInferenceResults;
        _typesConverter = typesConverter;
        _dialect = dialect;
    }

    public IExpressionNode Visit(SuperAnonymFunctionSyntaxNode node) {
        var outputTypeFunDefinition = node.OutputType.FunTypeSpecification;
        if (outputTypeFunDefinition == null)
            throw new NFunImpossibleException("Fun definition expected");
        string[] argNames;
        if (outputTypeFunDefinition.Inputs.Length == 1)
            argNames = new[] { "it" };
        else
        {
            argNames = new string[outputTypeFunDefinition.Inputs.Length];
            for (int i = 0; i < outputTypeFunDefinition.Inputs.Length; i++)
            {
                argNames[i] = $"it{i + 1}";
            }
        }

        //Prepare local variable scope
        //Capture all outer scope variables
        var localVariables = new VariableDictionary(_variables.GetAll(), _variables.Count);

        var arguments = new VariableSource[argNames.Length];
        for (var i = 0; i < argNames.Length; i++)
        {
            var arg = argNames[i];
            var type = outputTypeFunDefinition.Inputs[i];
            var source =
                VariableSource.CreateWithoutStrictTypeLabel(arg, type, FunnyVarAccess.Input, _dialect.Converter);
            //collect argument
            arguments[i] = source;
            //add argument to local scope
            //if argument with it* name already exist - replace it
            localVariables.AddOrReplace(source);
        }

        var body = node.Body;
        return BuildAnonymousFunction(node.Interval, body, localVariables, arguments);
    }

    public IExpressionNode Visit(StructFieldAccessSyntaxNode node) {
        var structNode = ReadNode(node.Source);

        if (node.IsSafeAccess)
        {
            if (_dialect.OptionalTypesSupport != OptionalTypesSupport.ExperimentalEnabled)
                throw Errors.SafeAccessNotSupported(node.Interval);
            if (structNode.Type.BaseType != BaseFunnyType.Optional)
                throw Errors.SafeAccessOnNonOptional(node.Interval);
            var innerType = structNode.Type.OptionalTypeSpecification.ElementType;
            if (!innerType.StructTypeSpecification.ContainsKey(node.FieldName))
                throw Errors.FieldNotExists(node.FieldName, node.Interval);
            return new SafeFieldAccessExpressionNode(node.FieldName, structNode, node.Interval);
        }

        if (structNode.Type.BaseType == BaseFunnyType.Optional)
        {
            var innerType = structNode.Type.OptionalTypeSpecification.ElementType;
            if (!innerType.StructTypeSpecification.ContainsKey(node.FieldName))
                throw Errors.FieldNotExists(node.FieldName, node.Interval);
            return new SafeFieldAccessExpressionNode(node.FieldName, structNode, node.Interval);
        }

        // Funtic allows default values for not specified types
        // so call:
        //  y = {}.missingField
        // is allowed, but it semantically incorrect

        if (!structNode.Type.StructTypeSpecification.ContainsKey(node.FieldName))
            throw Errors.FieldNotExists(node.FieldName, node.Interval);

        return new StructFieldAccessExpressionNode(node.FieldName, structNode, node.Interval);
    }

    public IExpressionNode Visit(StructInitSyntaxNode node) {
        var types = new StructTypeSpecification(node.Fields.Count, isFrozen: true);
        var names = new string[node.Fields.Count];
        var nodes = new IExpressionNode[node.Fields.Count];

        for (int i = 0; i < node.Fields.Count; i++)
        {
            var field = node.Fields[i];
            nodes[i] = ReadNode(field.Node);
            names[i] = field.Name;
            types.Add(field.Name, field.Node.OutputType);
        }

        foreach (var (key, _) in node.OutputType.StructTypeSpecification)
        {
            if (!types.ContainsKey(key))
                throw Errors.FieldIsMissed(key, node.Interval);
        }

        return new StructInitExpressionNode(names, nodes, node.Interval, FunnyType.StructOf(types));
    }

    public IExpressionNode Visit(DefaultValueSyntaxNode node) =>
        new DefaultValueExpressionNode(
            node.OutputType.GetDefaultFunnyValue(),
            node.OutputType,
            node.Interval);

    public IExpressionNode Visit(AnonymFunctionSyntaxNode node) {
        node.Definition.NotNull($"{nameof(node.Definition)} is missing");
        node.Body.NotNull($"{nameof(node.Body)} is missing");

        //Anonym fun arguments list
        var argumentLexNodes = node.ArgumentsDefinition;

        //Prepare local variable scope
        //Capture all outerscope variables
        var localVariables = new VariableDictionary(_variables.GetAll(), _variables.Count);

        var arguments = new VariableSource[argumentLexNodes.Count];
        var argIndex = 0;
        foreach (var arg in argumentLexNodes)
        {
            //Convert argument node
            var varNode = FunArgumentDeclarationRuntimeNode.CreateWith(arg);
            var source = VariableSource.CreateWithStrictTypeLabel(
                varNode.Name, varNode.Type, arg.Interval,
                FunnyVarAccess.Input,
                _dialect.Converter);
            //collect argument
            arguments[argIndex] = source;
            argIndex++;
            //add argument to local scope
            if (!localVariables.TryAdd(source))
            {
                //Check for duplicated arg-names
                //If outer-scope contains the conflict variable name
                if (_variables.GetOrNull(varNode.Name) != null)
                    throw Errors.AnonymousFunctionArgumentConflictsWithOuterScope(
                        varNode.Name,
                        node.Interval);
                else //else it is duplicated arg name
                    throw Errors.AnonymousFunctionArgumentDuplicates(varNode, node.Definition);
            }
        }

        var body = node.Body;
        return BuildAnonymousFunction(node.Interval, body, localVariables, arguments);
    }

    public IExpressionNode Visit(ArraySyntaxNode node) {
        var elements = new IExpressionNode[node.Expressions.Count];
        var expectedElementType = node.OutputType.ArrayTypeSpecification.FunnyType;
        for (int i = 0; i < node.Expressions.Count; i++)
        {
            var elementNode = ReadNode(node.Expressions[i]);
            elements[i] = CastExpressionNode.GetConvertedOrOriginOrThrow(elementNode, expectedElementType,
                _dialect.Converter.TypeBehaviour);
        }

        return new ArrayExpressionNode(elements, node.Interval, node.OutputType);
    }

    public IExpressionNode Visit(FunCallSyntaxNode node) {
        var id = node.Id;
        var args = _typeInferenceResults.GetResolvedCallArgsOrNull(node.OrderNumber) ?? node.Args;

        // Safe array access (?[]) — handled as special expression node (TIC graph op, not generic func)
        if (id == CoreFunNames.SafeGetElementName)
        {
            var source = ReadNode(args[0]);
            var index = ReadNode(args[1]);
            // Compute element converter: source array element type may differ from result element type
            // (e.g. source has U8[] but coalesce pushes I32 to result)
            Func<object, object> elementConverter = null;
            var sourceType = source.Type;
            if (sourceType.BaseType == BaseFunnyType.Optional)
                sourceType = sourceType.OptionalTypeSpecification.ElementType;
            if (sourceType.BaseType == BaseFunnyType.ArrayOf)
            {
                var actualElemType = sourceType.ArrayTypeSpecification.FunnyType;
                var resultType = node.OutputType;
                var expectedElemType = resultType.BaseType == BaseFunnyType.Optional
                    ? resultType.OptionalTypeSpecification.ElementType
                    : resultType;
                if (actualElemType != expectedElemType)
                    elementConverter = VarTypeConverter.GetConverterOrNull(
                        _dialect.Converter.TypeBehaviour, actualElemType, expectedElemType);
            }

            return new SafeArrayAccessExpressionNode(source, index, node.OutputType, elementConverter, node.Interval);
        }

        var someFunc = node.ResolvedSignature
            ?? _typeInferenceResults.GetResolvedCallSignatureOrNull(node.OrderNumber)
            ?? _functions.GetOrNull(id, args.Length);

        if (someFunc is null)
        {
            //todo move to variable syntax node
            //hi order function
            var functionalVariableSource = _variables.GetOrNull(id);
            if (functionalVariableSource?.Type.FunTypeSpecification == null)
                throw Errors.FunctionNotFoundForHiOrderUsage(node, _functions);
            return CreateFunctionCall(node, ConcreteHiOrderFunction.Create(functionalVariableSource));
        }

        if (someFunc is IConcreteFunction f) //concrete function
            return CreateFunctionCall(node, f);


        if (someFunc is IGenericFunction genericFunction) //generic function
        {
            FunnyType[] genericArgs;
            // Generic function type arguments usually stored in tic results
            var genericTypes = _typeInferenceResults.GetGenericCallArguments(node.OrderNumber);
            if (genericTypes == null)
            {
                // Generic call arguments are unknown  in case of generic recursion function .
                // Take them from type inference results
                var recCallSignature = _typeInferenceResults
                    .GetRecursiveCallOrNull(node.OrderNumber);

                if (recCallSignature == null)
                    throw new NFunImpossibleException($"MJ78. Function {id}`{args.Length} was not found");

                // If a user function shadows a builtin at the same arity, the scope dictionary
                // contains the user's GenericUserFunction which should be used instead of the builtin.
                var scopeFunc = _functions.GetOrNull(id, args.Length);
                if (scopeFunc is IGenericFunction userGeneric && scopeFunc != genericFunction)
                    genericFunction = userGeneric;

                var varTypeCallSignature = _typesConverter.Convert(recCallSignature);
                //Calculate generic call arguments by concrete function signature
                genericArgs = genericFunction.CalcGenericArgTypeList(varTypeCallSignature.FunTypeSpecification);
            }
            else
            {
                genericArgs = new FunnyType[genericTypes.Length];
                for (int i = 0; i < genericTypes.Length; i++)
                    genericArgs[i] = _typesConverter.Convert(genericTypes[i]);
            }

            var function = genericFunction.CreateConcrete(genericArgs, _dialect);

            if (_dialect.OptionalTypesSupport != OptionalTypesSupport.ExperimentalEnabled
                && id is CoreFunNames.ForceUnwrap or CoreFunNames.NullCoalesce)
                throw Errors.OptionalTypesNotSupported(id, node.Interval);

            return CreateFunctionCall(node, function);
        }

        throw new NFunImpossibleException($"MJ101. Function {id}`{args.Length} type is unknown");
    }

    public IExpressionNode Visit(ComparisonChainSyntaxNode node) {
        var expressionNodes = node.Operands.SelectToArray(ReadNode);
        var functions = new FunctionWithTwoArgs[node.Operators.Count];
        var converters = new Func<object, object>[functions.Length * 2];

        for (int i = 0; i < functions.Length; i++)
        {
            var op = node.Operators[i];
            var functionName = SyntaxNodeReader.GetOperatorFunctionName(op.Type)
                               ?? throw new NFunImpossibleException("MJ987");
            var genericFunction = _functions.GetOrNull(functionName, 2) as IGenericFunction
                                  ?? throw new NFunImpossibleException("MJ989");
            var ticType = _typeInferenceResults.GetSyntaxNodeTypeOrNull(node.Operands[i].OrderNumber);
            var gArg = _typesConverter.Convert(ticType);
            var concreteFunction = genericFunction.CreateConcrete(new[] { gArg }, _dialect);
            functions[i] = (concreteFunction as FunctionWithTwoArgs).NotNull("Not a two args");

            var l = expressionNodes[i];
            if (l.Type != gArg)
                converters[i * 2] =
                    VarTypeConverter.GetConverterOrThrow(_dialect.Converter.TypeBehaviour, l.Type, gArg, l.Interval);

            var r = expressionNodes[i + 1];
            if (r.Type != gArg)
                converters[i * 2 + 1] =
                    VarTypeConverter.GetConverterOrThrow(_dialect.Converter.TypeBehaviour, r.Type, gArg, r.Interval);
        }

        return new ComparisonChainExpressionNode(expressionNodes, functions, converters, node.Interval);
    }

    public IExpressionNode Visit(ResultFunCallSyntaxNode node) {
        var functionGenerator = ReadNode(node.ResultExpression);
        var function = ConcreteHiOrderFunctionWithSyntaxNode.Create(functionGenerator);
        return CreateFunctionCall(node, function);
    }


    public IExpressionNode Visit(IfThenElseSyntaxNode node) {
        if (_dialect.IfExpressionSetup == IfExpressionSetup.Deny)
            throw Errors.IfElseExpressionIsDenied(node.Interval);

        //expressions
        //if (...) {here}
        var expressionNodes = new IExpressionNode[node.Ifs.Length];
        //conditions
        // if ( {here} ) ...
        var conditionNodes = new IExpressionNode[node.Ifs.Length];

        if (_dialect.IfExpressionSetup == IfExpressionSetup.IfElseIf && node.Ifs.Length > 1)
            throw Errors.ElseKeywordIsMissing(node.Interval.Start, node.Ifs[1].Interval.Start);

        for (int i = 0; i < expressionNodes.Length; i++)
        {
            var ifCaseNode = node.Ifs[i];

            conditionNodes[i] = ReadNode(ifCaseNode.Condition);
            var exprNode = ReadNode(ifCaseNode.Expression);
            expressionNodes[i] =
                CastExpressionNode.GetConvertedOrOriginOrThrow(exprNode, node.OutputType,
                    _dialect.Converter.TypeBehaviour);
        }

        var elseNode = CastExpressionNode.GetConvertedOrOriginOrThrow(ReadNode(node.ElseExpr), node.OutputType,
            _dialect.Converter.TypeBehaviour);

        return new IfElseExpressionNode(
            expressionNodes, conditionNodes, elseNode,
            node.Interval, node.OutputType);
    }

    public IExpressionNode Visit(ConstantSyntaxNode node) {
        if (_dialect.OptionalTypesSupport != OptionalTypesSupport.ExperimentalEnabled
            && node.Value is FunnyNone)
            throw Errors.NoneLiteralNotSupported(node.Interval);
        var (enode, type) = GetConstantNodeOrNull(node.Value, node);
        return enode ?? new ConstantExpressionNode(node.Value, type, node.Interval);
    }

    public IExpressionNode Visit(IpAddressConstantSyntaxNode node) =>
        new ConstantExpressionNode(node.Value, FunnyType.Ip, node.Interval);

    public IExpressionNode Visit(GenericIntSyntaxNode node) {
        var (enode, _) = GetConstantNodeOrNull(node.Value, node);
        return enode.NotNull($"Generic syntax node has wrong value type: {node.Value.GetType().Name}");
    }

    private (IExpressionNode, FunnyType) GetConstantNodeOrNull(object value, ISyntaxNode node) {
        var type = _typesConverter.Convert(_typeInferenceResults.GetSyntaxNodeTypeOrNull(node.OrderNumber));
        return (value switch {
            long l => ConstantExpressionNode.CreateConcrete(type, l, _dialect.Converter.TypeBehaviour, node.Interval),
            ulong u => ConstantExpressionNode.CreateConcrete(type, u, _dialect.Converter.TypeBehaviour, node.Interval),
            string d => new ConstantExpressionNode(
                _dialect.Converter.TypeBehaviour.ParseOrNull(d) ?? throw Errors.CannotParseDecimalNumber(node.Interval),
                type, node.Interval),
            _ => null
        }, type);
    }

    public IExpressionNode Visit(NamedIdSyntaxNode node) {
        if (node.IdType == NamedIdNodeType.Constant)
        {
            var varVal = (ConstantValueAndType)node.IdContent;
            return new ConstantExpressionNode(varVal.FunnyValue, varVal.Type, node.Interval);
        }

        var funVariable = _typeInferenceResults.GetFunctionalVariableOrNull(node.OrderNumber);
        if (funVariable != null)
        {
            if (funVariable is IGenericFunction genericFunction)
            {
                var genericTypes = _typeInferenceResults
                    .GetGenericCallArguments(node.OrderNumber)
                    .NotNull($"Generic function is missed at {node.OrderNumber}:  {node.Id}`{genericFunction.Name}");

                var genericArgs = new FunnyType[genericTypes.Length];
                for (int i = 0; i < genericTypes.Length; i++)
                    genericArgs[i] = _typesConverter.Convert(genericTypes[i]);

                var function = genericFunction.CreateConcrete(genericArgs, _dialect);
                return new FunVariableExpressionNode(function, node.Interval);
            }
            else if (funVariable is IConcreteFunction concrete)
                return new FunVariableExpressionNode(concrete, node.Interval);
        }

        var vType = node.VariableType.BaseType == BaseFunnyType.Empty
            ? node.OutputType
            : node.VariableType;

        var source = _variables.GetOrNull(node.Id);
        if (source == null)
        {
            source = VariableSource.CreateWithoutStrictTypeLabel(node.Id, vType, FunnyVarAccess.Input,
                _dialect.Converter);
            _variables.TryAdd(source);
        }

        var node1 = new VariableExpressionNode(source, node.Interval);
        if (node1.Source.Name != node.Id)
            throw Errors.InputNameWithDifferentCase(node.Id, node1.Source.Name, node.Interval);
        return node1;
    }


    #region not an expression

    public IExpressionNode Visit(EquationSyntaxNode node)
        => ThrowNotAnExpression(node);

    public IExpressionNode Visit(IfCaseSyntaxNode node)
        => ThrowNotAnExpression(node);

    public IExpressionNode Visit(ListOfExpressionsSyntaxNode node)
        => ThrowNotAnExpression(node);

    public IExpressionNode Visit(SyntaxTree node)
        => ThrowNotAnExpression(node);

    public IExpressionNode Visit(TypedVarDefSyntaxNode node)
        => ThrowNotAnExpression(node);

    public IExpressionNode Visit(UserFunctionDefinitionSyntaxNode node)
        => ThrowNotAnExpression(node);

    public IExpressionNode Visit(VarDefinitionSyntaxNode node)
        => ThrowNotAnExpression(node);

    #endregion


    private IExpressionNode BuildAnonymousFunction(
        Interval interval, ISyntaxNode body,
        VariableDictionary localVariables, VariableSource[] arguments) {
        // Collect function arguments, to find closured variables in future
        var originVariables = localVariables.GetAllAsArray();
        var originVariableNames = new string[originVariables.Length];
        for (int i = 0; i < originVariableNames.Length; i++) originVariableNames[i] = originVariables[i].Name;

        var expr = BuildExpression(
            body, _functions, localVariables, _typeInferenceResults, _typesConverter,
            _dialect);
        //New variables, that are not in origin function arguments - are closured variables
        var closured = localVariables
            .GetAll()
            .Where(s => !originVariableNames.Contains(s.Name))
            .ToList();

        var wrongItVariable = closured.FirstOrDefault(c => Helper.DoesItLooksLikeSuperAnonymousVariable(c.Name));
        if (wrongItVariable != null)
            throw Errors.CannotUseSuperAnonymousVariableHere(
                expr.FindFirstUsageOrThrow(wrongItVariable).Interval, wrongItVariable.Name);

        //Add closured vars to outer-scope dictionary
        foreach (var newVar in closured)
            _variables.TryAdd(newVar); //add full usage info to allow analyze outer errors

        var fun = ConcreteUserFunction.Create(
            isRecursive: false,
            name: "rule",
            variables: arguments,
            expression: expr);
        return new FunRuleExpressionNode(fun, interval);
    }

    private IExpressionNode CreateFunctionCall(IFunCallSyntaxNode node, IConcreteFunction function) {
        var callArgs = node is FunCallSyntaxNode fc
            ? (_typeInferenceResults.GetResolvedCallArgsOrNull(fc.OrderNumber) ?? fc.Args)
            : node.Args;
        var children = callArgs.SelectToArray(ReadNode);
        var converted = function.CreateWithConvertionOrThrow(children, _dialect.Converter.TypeBehaviour, node.Interval);
        if (converted.Type != node.OutputType)
        {
            var converter = VarTypeConverter.GetConverterOrThrow(_dialect.Converter.TypeBehaviour, converted.Type,
                node.OutputType, node.Interval);
            return new CastExpressionNode(converted, node.OutputType, converter, node.Interval);
        }
        else
            return converted;
    }

    private static IExpressionNode ThrowNotAnExpression(ISyntaxNode node)
        => throw Errors.NotAnExpression(node);

    private IExpressionNode ReadNode(ISyntaxNode node)
        => node.Accept(this);

    public IExpressionNode Visit(BinOperatorSyntaxNode node) =>
        VisitOperator(node, node.ResolvedSignature ?? _functions.GetOrNull(node.Id, 2));

    public IExpressionNode Visit(UnaryOperatorSyntaxNode node) =>
        VisitOperator(node, node.ResolvedSignature ?? _functions.GetOrNull(node.Id, 1));

    private IExpressionNode VisitOperator(IFunCallSyntaxNode node, IFunctionSignature someFunc) {
        if (someFunc is IConcreteFunction f)
            return CreateFunctionCall(node, f);

        if (someFunc is IGenericFunction genericFunction) {
            var genericTypes = _typeInferenceResults.GetGenericCallArguments(node.OrderNumber);
            FunnyType[] genericArgs;
            if (genericTypes == null) {
                // Recursive operator call — extremely rare but handle for safety
                var recSig = _typeInferenceResults.GetRecursiveCallOrNull(node.OrderNumber);
                if (recSig == null)
                    throw new NFunImpossibleException($"MJ78op. Operator {node.Accept(new ShortDescriptionVisitor())} was not found");
                var scopeFunc = _functions.GetOrNull(node.Accept(new ShortDescriptionVisitor()), ((IFunCallSyntaxNode)node).Args.Length);
                if (scopeFunc is IGenericFunction userGeneric && scopeFunc != genericFunction)
                    genericFunction = userGeneric;
                genericArgs = genericFunction.CalcGenericArgTypeList(
                    _typesConverter.Convert(recSig).FunTypeSpecification);
            } else {
                genericArgs = new FunnyType[genericTypes.Length];
                for (int i = 0; i < genericTypes.Length; i++)
                    genericArgs[i] = _typesConverter.Convert(genericTypes[i]);
            }
            return CreateFunctionCall(node, genericFunction.CreateConcrete(genericArgs, _dialect));
        }

        throw new NFunImpossibleException($"MJ101op. Operator type is unknown");
    }
}
