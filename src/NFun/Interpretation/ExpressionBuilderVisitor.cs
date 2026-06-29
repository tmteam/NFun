using System;
using System.Collections.Generic;
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
        _functions = functions;
        _variables = variables;
        _typeInferenceResults = typeInferenceResults;
        _typesConverter = typesConverter;
        _dialect = dialect;
    }

    /// <summary>
    /// Look up a function in the registry, respecting extension separation. Routes to the
    /// extension dict on piped calls, the direct dict on bare calls when separation is
    /// enabled; falls back to the union when separation is disabled.
    /// </summary>
    private IFunctionSignature GetFunctionOrNull(string name, int argCount, bool isPiped) {
        if (_dialect.ExtensionFunctionsSeparation == ExtensionFunctionsSeparation.Enabled)
            return _functions.GetOrNull(name, argCount, isPiped ? CallStyle.Extension : CallStyle.Direct);
        return _functions.GetOrNull(name, argCount);
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

        // NamedStruct types don't expose StructTypeSpecification at this layer;
        // field-existence check is deferred to runtime via registry.
        bool isNamedStruct = structNode.Type.BaseType == BaseFunnyType.NamedStruct
            || (structNode.Type.BaseType == BaseFunnyType.Optional
                && structNode.Type.OptionalTypeSpecification.ElementType.BaseType == BaseFunnyType.NamedStruct);

        if (node.IsSafeAccess)
        {
            if (_dialect.OptionalTypesSupport != OptionalTypesSupport.Enabled)
                throw Errors.SafeAccessNotSupported(node.Interval);
            if (structNode.Type.BaseType == BaseFunnyType.None)
                // none?.field = none — safe access on None always returns None
                return new SafeFieldAccessExpressionNode(node.FieldName, structNode, node.Interval);
            // Permissive: ?. on a non-optional struct/named-struct receiver is just
            // regular field access — the safety check is a no-op (receiver can't be
            // none) but the syntax is still useful when chaining onto an inline
            // constructor or when the field itself is Optional. Reject ?. on
            // non-struct receivers (Int, Real, Array, …) — semantically nonsense.
            bool receiverIsStructLike =
                structNode.Type.BaseType == BaseFunnyType.Optional
                || structNode.Type.BaseType == BaseFunnyType.Struct
                || structNode.Type.BaseType == BaseFunnyType.NamedStruct;
            if (!receiverIsStructLike)
                throw Errors.SafeAccessOnNonOptional(node.Interval);
            if (structNode.Type.BaseType == BaseFunnyType.Optional)
            {
                var innerType = structNode.Type.OptionalTypeSpecification.ElementType;
                if (innerType.BaseType == BaseFunnyType.Struct
                    && !innerType.StructTypeSpecification.ContainsKey(node.FieldName))
                    throw Errors.FieldNotExists(node.FieldName, node.Interval);
            }
            else if (structNode.Type.BaseType == BaseFunnyType.Struct
                     && !structNode.Type.StructTypeSpecification.ContainsKey(node.FieldName))
                throw Errors.FieldNotExists(node.FieldName, node.Interval);
            return new SafeFieldAccessExpressionNode(node.FieldName, structNode, node.Interval);
        }

        if (structNode.Type.BaseType == BaseFunnyType.Optional)
        {
            var innerType = structNode.Type.OptionalTypeSpecification.ElementType;
            if (innerType.BaseType == BaseFunnyType.Struct
                && !innerType.StructTypeSpecification.ContainsKey(node.FieldName))
                throw Errors.FieldNotExists(node.FieldName, node.Interval);
            return new SafeFieldAccessExpressionNode(node.FieldName, structNode, node.Interval);
        }

        // NamedStruct path — field check via registry happens at runtime.
        if (isNamedStruct)
            return new StructFieldAccessExpressionNode(node.FieldName, structNode, node.Interval, node.OutputType);

        // Funtic allows default values for not specified types
        // so call:
        //  y = {}.missingField
        // is allowed, but it semantically incorrect

        if (!structNode.Type.StructTypeSpecification.ContainsKey(node.FieldName))
            throw Errors.FieldNotExists(node.FieldName, node.Interval);

        // If TIC resolved this field as Optional (e.g., from named type ancestor declaring
        // the field as Optional), use the TIC output type for the result.
        // This handles inline constructor access: a{b=val}.b where b is declared Optional.
        if (node.OutputType.BaseType == BaseFunnyType.Optional
            && structNode.Type.BaseType == BaseFunnyType.Struct) {
            // The struct may store the field in a narrower type than TIC resolved
            // (e.g., field is UInt8? but TIC says Int32? due to preferred type propagation).
            // Add converter to widen the runtime value to match the declared type.
            var actualFieldType = structNode.Type.StructTypeSpecification[node.FieldName];
            var converter = actualFieldType != node.OutputType
                ? VarTypeConverter.GetConverterOrNull(_dialect.Converter.TypeBehaviour, actualFieldType, node.OutputType)
                : null;
            return new StructFieldAccessExpressionNode(node.FieldName, structNode, node.Interval,
                overrideType: node.OutputType, converter: converter);
        }

        return NarrowIfNeeded(
            new StructFieldAccessExpressionNode(node.FieldName, structNode, node.Interval), node.OutputType);
    }

    /// <summary>
    /// If expr.Type is opt(T) but outputType is non-Optional (narrowing proved non-None),
    /// unwrap via TypeOverrideNode and cast T→outputType if needed.
    /// </summary>
    private IExpressionNode NarrowIfNeeded(IExpressionNode expr, FunnyType outputType) {
        if (expr.Type != outputType
            && expr.Type.BaseType == BaseFunnyType.Optional
            && outputType.BaseType != BaseFunnyType.Optional)
        {
            var innerType = expr.Type.OptionalTypeSpecification.ElementType;
            IExpressionNode unwrapped = new TypeOverrideNode(expr, innerType);
            if (innerType != outputType)
                unwrapped = CastExpressionNode.GetConvertedOrOriginOrThrow(
                    unwrapped, outputType, _dialect.Converter.TypeBehaviour);
            return unwrapped;
        }
        return expr;
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

        // StructInit always produces a struct. If TIC resolved it as Optional (from merge
        // with Optional in if-else branches), unwrap to get the struct type.
        var structType = node.OutputType;
        while (structType.BaseType == BaseFunnyType.Optional)
            structType = structType.OptionalTypeSpecification.ElementType;
        if (structType.BaseType == BaseFunnyType.Struct)
            foreach (var (key, _) in structType.StructTypeSpecification)
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

        // TIC-solved fun signature: input types live in node.OutputType.FunTypeSpecification.Inputs.
        // Use them when the argument has a type annotation (e.g. `rule(x:age)=...`), because
        // FunArgumentDeclarationRuntimeNode.CreateWith falls back to a customTypes-less resolver
        // and would reject user-defined named types / aliases.
        var funSpec = node.OutputType.BaseType == BaseFunnyType.Fun
            ? node.OutputType.FunTypeSpecification
            : null;

        var arguments = new VariableSource[argumentLexNodes.Count];
        var argIndex = 0;
        foreach (var arg in argumentLexNodes)
        {
            //Convert argument node
            var varNode = FunArgumentDeclarationRuntimeNode.CreateWith(arg, funSpec?.Inputs[argIndex]);
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

        // ?? (null coalesce) — TIC special form: (opt(U), V) → LCA(U, V).
        // Runtime: if left is None → return right, else return left (unwrapped + converted).
        if (id == CoreFunNames.NullCoalesce && args.Length == 2)
        {
            if (_dialect.OptionalTypesSupport != OptionalTypesSupport.Enabled)
                throw Errors.OptionalTypesNotSupported(id, node.Interval);
            var left = ReadNode(args[0]);
            var right = ReadNode(args[1]);
            // Algebraic rule: (opt(U), V) → LCA(U, V).
            // When V is non-optional (and not None), the result MUST be non-optional.
            // TIC may over-approximate (IsOptional leak from shared ?[] nodes) — strip here.
            var outputType = node.OutputType;
            var rightIsOptionalOrNone = right.Type.BaseType is BaseFunnyType.Optional or BaseFunnyType.None;
            if (outputType.BaseType == BaseFunnyType.Optional && !rightIsOptionalOrNone) {
                outputType = outputType.OptionalTypeSpecification.ElementType;
                node.OutputType = outputType;
            }
            // Left unwrap converter: opt(U) → U → outputType (applied only when non-None)
            var innerType = left.Type.BaseType == BaseFunnyType.Optional
                ? left.Type.OptionalTypeSpecification.ElementType
                : left.Type;
            // Per Optionals.md L106 & Algebra.md hierarchy axiom: `??` result = LCA(U, V).
            // For cross-tree types (e.g., int ?? text) LCA = Any. The earlier compatibility
            // check rejected such cases with FU887, but is now redundant: TIC's
            // PullConstraintsFunctions.Apply(StateOptional, CS) eagerly propagates the
            // lifted literal's state to the elemNode, so the LCA correctly widens to Any.
            // Both branches identity-box to Any via GetConverterOrNull(srcType, Any)
            // → NoConvertion. (MR3Bug1.)
            Func<object, object> leftConverter = innerType != outputType
                ? VarTypeConverter.GetConverterOrNull(_dialect.Converter.TypeBehaviour, innerType, outputType)
                : null;
            var castRight = CastExpressionNode.GetConvertedOrOriginOrThrow(
                right, outputType, _dialect.Converter.TypeBehaviour);
            return new CoalesceExpressionNode(left, castRight, leftConverter, outputType, node.Interval);
        }

        // Pipe-forward reinterpreted as struct field access + call: a.f(args) → (a.f)(args)
        if (node is { IsFieldCall: true, IsPipeForward: true } fieldCallNode)
        {
            // First arg is the struct source, remaining are the call args
            var structSource = ReadNode(args[0]);

            if (fieldCallNode.IsSafeAccess && structSource.Type.BaseType == BaseFunnyType.Optional)
            {
                var unwrappedType = structSource.Type.OptionalTypeSpecification.ElementType;
                var cachedSource = new CachedSourceNode(unwrappedType, node.Interval);
                var fieldAccessSafe = new StructFieldAccessExpressionNode(id, cachedSource, node.Interval);
                if (fieldAccessSafe.Type.FunTypeSpecification == null)
                    throw Errors.FieldIsNotCallable(id, fieldAccessSafe.Type, node.Interval);
                var hiOrderFuncSafe = ConcreteHiOrderFunctionWithSyntaxNode.Create(fieldAccessSafe);
                var callArgsSafe = new ISyntaxNode[args.Length - 1];
                Array.Copy(args, 1, callArgsSafe, 0, callArgsSafe.Length);
                var childrenSafe = callArgsSafe.SelectToArray(ReadNode);
                var innerCall = hiOrderFuncSafe.CreateWithConvertionOrThrow(
                    childrenSafe, _dialect.Converter.TypeBehaviour, node.Interval);
                return new SafePipedCallExpressionNode(
                    structSource, cachedSource, innerCall, node.OutputType, node.Interval);
            }

            var fieldAccess = new StructFieldAccessExpressionNode(id, structSource, node.Interval);
            if (fieldAccess.Type.FunTypeSpecification == null)
                throw Errors.FieldIsNotCallable(id, fieldAccess.Type, node.Interval);
            var hiOrderFunc = ConcreteHiOrderFunctionWithSyntaxNode.Create(fieldAccess);
            var callArgs = new ISyntaxNode[args.Length - 1];
            Array.Copy(args, 1, callArgs, 0, callArgs.Length);
            var children = callArgs.SelectToArray(ReadNode);
            return hiOrderFunc.CreateWithConvertionOrThrow(children, _dialect.Converter.TypeBehaviour, node.Interval);
        }

        var someFunc = node.ResolvedSignature
            ?? _typeInferenceResults.GetResolvedCallSignatureOrNull(node.OrderNumber)
            ?? GetFunctionOrNull(id, args.Length, node.IsPipeForward);

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
                var scopeFunc = GetFunctionOrNull(id, args.Length, node.IsPipeForward);
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

            ValidateGenericResolution(genericFunction, genericArgs, node);

            if (_dialect.OptionalTypesSupport != OptionalTypesSupport.Enabled
                && id is CoreFunNames.ForceUnwrap or CoreFunNames.NullCoalesce)
                throw Errors.OptionalTypesNotSupported(id, node.Interval);

            var function = genericFunction.CreateConcrete(genericArgs, _dialect);

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
        if (_dialect.OptionalTypesSupport != OptionalTypesSupport.Enabled
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
        // OutputType already set by ApplyTiResultEnterVisitor or SPS ApplyTypesToSyntaxTree
        var type = node.OutputType.BaseType != BaseFunnyType.Empty
            ? node.OutputType
            : _typesConverter.Convert(_typeInferenceResults.GetSyntaxNodeTypeOrNull(node.OrderNumber));
        // When the literal sits in an Optional-element context (e.g. `1` inside
        // `[1, none]` where TIC inferred element type Int32?), build the
        // constant at the underlying primitive type; the surrounding context
        // (ArraySyntaxNode visit) will apply the Optional cast via VarTypeConverter.
        // Without unwrap, CreateConcrete hits the default ArgOOR branch (MBug1).
        var primitive = type.BaseType == BaseFunnyType.Optional
            ? type.OptionalTypeSpecification.ElementType
            : type;
        IExpressionNode enode = value switch {
            long l => ConstantExpressionNode.CreateConcrete(primitive, l, _dialect.Converter.TypeBehaviour, node.Interval),
            ulong u => ConstantExpressionNode.CreateConcrete(primitive, u, _dialect.Converter.TypeBehaviour, node.Interval),
            string d => new ConstantExpressionNode(
                _dialect.Converter.TypeBehaviour.ParseOrNull(d) ?? throw Errors.CannotParseDecimalNumber(node.Interval),
                primitive, node.Interval),
            _ => null
        };
        if (enode != null && type.BaseType == BaseFunnyType.Optional)
            enode = CastExpressionNode.GetConvertedOrOriginOrThrow(enode, type, _dialect.Converter.TypeBehaviour);
        return (enode, type);
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

        return NarrowIfNeeded(node1, node.OutputType);
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
        // Captures = every VariableSource the body reads that is not one of the
        // rule's own arguments. At Calc() these get snapshotted so the returned
        // closure is independent of subsequent writes to the enclosing scope
        // (Specs/Rules.md "Capturing variables").
        var captures = CollectCaptures(expr, arguments);
        return new FunRuleExpressionNode(fun, captures, interval);
    }

    private static VariableSource[] CollectCaptures(IExpressionNode body, VariableSource[] ruleArguments) {
        var unique = new HashSet<VariableSource>();
        CollectVariableSources(body, unique);
        if (unique.Count == 0)
            return Array.Empty<VariableSource>();
        for (int i = 0; i < ruleArguments.Length; i++)
            unique.Remove(ruleArguments[i]);
        if (unique.Count == 0)
            return Array.Empty<VariableSource>();
        var result = new VariableSource[unique.Count];
        unique.CopyTo(result);
        return result;
    }

    private static void CollectVariableSources(IRuntimeNode node, HashSet<VariableSource> sink) {
        switch (node) {
            case VariableExpressionNode v:
                sink.Add(v.Source);
                return;
            case FunRuleExpressionNode:
                // A nested rule manages its own captures — its body references (including
                // its own args) are not captures of the enclosing rule.
                return;
            case FunOfSingleArgExpressionNode s when s.Fun is ConcreteHiOrderFunction hi:
                sink.Add(hi.Source); break;
            case FunOfTwoArgsExpressionNode t when t.Fun is ConcreteHiOrderFunction hi:
                sink.Add(hi.Source); break;
            case FunOfManyArgsExpressionNode m when m.Fun is ConcreteHiOrderFunction hi:
                sink.Add(hi.Source); break;
        }
        foreach (var child in node.Children)
            CollectVariableSources(child, sink);
    }

    private IExpressionNode CreateFunctionCall(IFunCallSyntaxNode node, IConcreteFunction function) {
        var callArgs = node is FunCallSyntaxNode fc
            ? (_typeInferenceResults.GetResolvedCallArgsOrNull(fc.OrderNumber) ?? fc.Args)
            : node.Args;
        var children = callArgs.SelectToArray(ReadNode);

        // ?.method() — safe piped call: if source is None, return None; else call inner function.
        // CachedSourceNode avoids double-evaluation: SafePipedCall writes value, inner call reads it.
        if (node is FunCallSyntaxNode { IsSafeAccess: true, IsPipeForward: true } && children.Length > 0
            && children[0].Type.BaseType == BaseFunnyType.Optional)
        {
            var sourceExpr = children[0];
            var unwrappedType = sourceExpr.Type.OptionalTypeSpecification.ElementType;
            var cachedSource = new CachedSourceNode(unwrappedType, node.Interval);
            children[0] = cachedSource;
            var innerCall = function.CreateWithConvertionOrThrow(children, _dialect.Converter.TypeBehaviour, node.Interval);
            return new SafePipedCallExpressionNode(sourceExpr, cachedSource, innerCall, node.OutputType, node.Interval);
        }

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
            FunnyType[] genericArgs;
            var genericTypes = _typeInferenceResults.GetGenericCallArguments(node.OrderNumber);
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
                for (int i = 0; i < genericTypes.Length; i++) {
                    genericArgs[i] = _typesConverter.Convert(genericTypes[i]);
                    var named = TicTypesConverter.BuildNamedTypeFromTicState(genericTypes[i]);
                    if (named.HasValue) genericArgs[i] = named.Value;
                }
            }
            ValidateGenericResolution(genericFunction, genericArgs, node);
            return CreateFunctionCall(node, genericFunction.CreateConcrete(genericArgs, _dialect));
        }

        throw new NFunImpossibleException($"MJ101op. Operator type is unknown");
    }

    /// <summary>
    /// FU711: Validates that generic type resolutions are not vacuous.
    /// When a generic T resolves to Any and the function uses T at different
    /// structural depths in its INPUT arguments (e.g., bare T in one arg and T[] in another),
    /// the Any resolution came from merging structurally incompatible constraints
    /// (e.g., 'h' in 'hello' where T would need to be both char[] and char).
    /// Only checks input argument types (not return type) because T=Any in the return
    /// position is legitimate for get-element from any[], fold of untyped array, etc.
    /// The proper fix belongs in the TIC solver.
    /// </summary>
    private static void ValidateGenericResolution(
        IGenericFunction function, FunnyType[] genericArgs, IFunCallSyntaxNode node) {
        // Skip validation when ALL generic args are Any AND any actual call argument type
        // is also Any — this happens inside generic user function bodies (CreateSomeConcrete)
        // where types are placeholder-concretized. When call args are all concrete (e.g., char
        // and char[]), the validation should still fire.
        bool allGenericAny = true;
        for (int i = 0; i < genericArgs.Length; i++)
            if (genericArgs[i].BaseType != BaseFunnyType.Any) { allGenericAny = false; break; }
        if (allGenericAny) {
            bool anyArgIsAny = false;
            foreach (var arg in node.Args)
                if (arg.OutputType.BaseType == BaseFunnyType.Any) { anyArgIsAny = true; break; }
            if (anyArgIsAny) return;
        }

        for (int i = 0; i < genericArgs.Length; i++) {
            if (genericArgs[i].BaseType != BaseFunnyType.Any)
                continue;
            // Generic resolved to Any — check if it appears at multiple structural
            // depths across input arguments only.
            bool hasDepth0 = false;
            bool hasNonZeroDepth = false;
            foreach (var argType in function.ArgTypes)
                CheckGenericDepths(argType, i, 0, ref hasDepth0, ref hasNonZeroDepth);
            if (hasDepth0 && hasNonZeroDepth)
                throw Errors.IncompatibleGenericResolution(function, i, node);
        }
    }

    private static void CheckGenericDepths(
        FunnyType type, int genericIndex, int depth,
        ref bool hasDepth0, ref bool hasNonZeroDepth) {
        if (type.BaseType == BaseFunnyType.Generic && type.GenericId == genericIndex) {
            if (depth == 0)
                hasDepth0 = true;
            else
                hasNonZeroDepth = true;
            return;
        }
        if (type.BaseType == BaseFunnyType.ArrayOf)
            CheckGenericDepths(type.ArrayTypeSpecification.FunnyType, genericIndex, depth + 1,
                ref hasDepth0, ref hasNonZeroDepth);
        else if (type.BaseType == BaseFunnyType.Fun) {
            foreach (var input in type.FunTypeSpecification.Inputs)
                CheckGenericDepths(input, genericIndex, depth + 1, ref hasDepth0, ref hasNonZeroDepth);
            CheckGenericDepths(type.FunTypeSpecification.Output, genericIndex, depth + 1,
                ref hasDepth0, ref hasNonZeroDepth);
        }
        else if (type.BaseType == BaseFunnyType.Optional)
            CheckGenericDepths(type.OptionalTypeSpecification.ElementType, genericIndex, depth + 1,
                ref hasDepth0, ref hasNonZeroDepth);
    }

    public IExpressionNode Visit(TypeDeclarationSyntaxNode node) =>
        throw new NFunImpossibleException("TypeDeclarationSyntaxNode should be removed during elaboration");

    public IExpressionNode Visit(NamedTypeConstructorSyntaxNode node) =>
        throw new NFunImpossibleException("NamedTypeConstructorSyntaxNode should be removed during elaboration");

    public IExpressionNode Visit(TryCatchSyntaxNode node) {
        if (_dialect.TryCatchSupport == TryCatchSupport.Disabled)
            throw Errors.TryCatchIsDenied(node.Interval);

        var tryNode = CastExpressionNode.GetConvertedOrOriginOrThrow(
            ReadNode(node.TryExpr), node.OutputType, _dialect.Converter.TypeBehaviour);

        // Pre-create the error variable before visiting catch body, so Visit(NamedIdSyntaxNode)
        // finds it and doesn't create a new Input variable. This prevents the catch-scoped
        // variable from leaking as a script-level input.
        VariableSource errorVar = null;
        if (node.ErrorVariableName != null) {
            var errorType = _typesConverter.Convert(
                _typeInferenceResults.GetSyntaxNodeTypeOrNull(node.CatchExpr.OrderNumber))
                ;
            // Get the type from the TIC alias node
            var aliasName = node.OrderNumber + "~" + node.ErrorVariableName;
            var aliasType = _typeInferenceResults.GetVariableTypeOrNull(aliasName);
            if (aliasType != null)
                errorType = _typesConverter.Convert(aliasType);

            errorVar = VariableSource.CreateWithoutStrictTypeLabel(
                node.ErrorVariableName, errorType, FunnyVarAccess.NoInfo, _dialect.Converter);
            _variables.TryAdd(errorVar);
        }

        var catchNode = CastExpressionNode.GetConvertedOrOriginOrThrow(
            ReadNode(node.CatchExpr), node.OutputType, _dialect.Converter.TypeBehaviour);

        // Remove the error variable from the dictionary after building the catch expression,
        // so it doesn't appear as a script-level variable
        if (node.ErrorVariableName != null)
            _variables.TryRemove(node.ErrorVariableName);

        return new TryCatchExpressionNode(tryNode, catchNode, errorVar, node.Interval, node.OutputType);
    }
}
