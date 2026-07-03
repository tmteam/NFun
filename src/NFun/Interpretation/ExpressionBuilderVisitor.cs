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
    private readonly INamedTypeFieldRegistry _namedTypes;
    /// <summary>
    /// Declared return type of the function whose body this visitor is building.
    /// Used by <see cref="Visit(ReturnSyntaxNode)"/> to cast the return expression
    /// at the boundary — TIC may infer a return-type LCA (e.g. Bool) that doesn't
    /// match a `return x` branch's runtime type (e.g. Int32 via x:int), and
    /// without a return-time cast the raw value lands in the typed slot.
    /// (StmtBug75b.) Empty for top-level scripts (no enclosing function).
    /// </summary>
    private readonly FunnyType _functionReturnType;


    private static IExpressionNode BuildExpression(
        ISyntaxNode node,
        IFunctionRegistry functions,
        VariableDictionary variables,
        TypeInferenceResults typeInferenceResults,
        TicTypesConverter typesConverter,
        DialectSettings dialect,
        INamedTypeFieldRegistry namedTypes = null) {
        var visitor = new ExpressionBuilderVisitor(functions, variables, typeInferenceResults, typesConverter, dialect, namedTypes);
        return node.Accept(visitor);
    }

    internal static IExpressionNode BuildExpression(
        ISyntaxNode node,
        IFunctionRegistry functions,
        FunnyType outputType,
        VariableDictionary variables,
        TypeInferenceResults typeInferenceResults,
        TicTypesConverter typesConverter,
        DialectSettings dialect,
        INamedTypeFieldRegistry namedTypes = null) {
        var visitor = new ExpressionBuilderVisitor(
            functions, variables, typeInferenceResults, typesConverter, dialect, namedTypes,
            functionReturnType: outputType);
        var result = node.Accept(visitor);
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
        DialectSettings dialect,
        INamedTypeFieldRegistry namedTypes = null,
        FunnyType functionReturnType = default) {
        _functions = functions;
        _variables = variables;
        _typeInferenceResults = typeInferenceResults;
        _typesConverter = typesConverter;
        _dialect = dialect;
        _namedTypes = namedTypes;
        _functionReturnType = functionReturnType;
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
            node.OutputType.GetDefaultFunnyValue(_namedTypes),
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
        // `Kind` is set by `TicSetupVisitor` based on the dialect — the single
        // source of truth. The shape of element casting is identical; only the
        // resulting container differs.
        var isList = node.Kind == ArrayLiteralKind.List;
        // Stage C — OutputType can resolve to fixedArray<T> via
        // Concretest(FixedArray)=FixedArray. Treat as ee-mode ArrayOf for the
        // literal builder (lang-mode is still List as per node.Kind).
        FunnyType expectedElementType;
        if (isList)
            expectedElementType = node.OutputType.ListTypeSpecification.FunnyType;
        else if (node.OutputType.BaseType == BaseFunnyType.FixedArray)
            expectedElementType = node.OutputType.FixedArrayTypeSpecification.FunnyType;
        else
            expectedElementType = node.OutputType.ArrayTypeSpecification.FunnyType;
        var elements = new IExpressionNode[node.Expressions.Count];
        for (int i = 0; i < node.Expressions.Count; i++)
        {
            var elementNode = ReadNode(node.Expressions[i]);
            elements[i] = CastExpressionNode.GetConvertedOrOriginOrThrow(elementNode, expectedElementType,
                _dialect.Converter.TypeBehaviour);
        }

        if (isList)
            return new ListExpressionNode(elements, node.Interval, node.OutputType);
        return new ArrayExpressionNode(elements, node.Interval, node.OutputType);
    }

    public IExpressionNode Visit(FunCallSyntaxNode node) {
        var id = node.Id;
        var args = _typeInferenceResults.GetResolvedCallArgsOrNull(node.OrderNumber) ?? node.Args;

        // ! (force unwrap) — TIC special form: (opt(T)) → T.
        // Runtime: throw if source is None, else return source value (possibly cast to outputType).
        // Mirrors SafeGetElement / NullCoalesce TIC-special-form runtime handling.
        // Bug hunt round 5 #10 family.
        if (id == CoreFunNames.ForceUnwrap && args.Length == 1)
        {
            if (_dialect.OptionalTypesSupport != OptionalTypesSupport.Enabled)
                throw Errors.OptionalTypesNotSupported(id, node.Interval);
            var source = ReadNode(args[0]);
            var outputType = node.OutputType;
            Func<object, object> converter = null;
            var srcInnerType = source.Type.BaseType == BaseFunnyType.Optional
                ? source.Type.OptionalTypeSpecification.ElementType
                : source.Type;
            if (srcInnerType != outputType)
                converter = VarTypeConverter.GetConverterOrNull(
                    _dialect.Converter.TypeBehaviour, srcInnerType, outputType);
            return new ForceUnwrapExpressionNode(source, outputType, converter, node.Interval);
        }

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
            // Algebraic rule: (opt(U), V) → LCA(U, V). TIC's solved type is authoritative.
            // (A historical strip here rewrote node.OutputType when TIC over-approximated
            // IsOptional; the leak was fixed at its root — the Pull Apply(CS, None) cell
            // now respects the negative-skolem U of `??` — and the strip is gone. N1.)
            var outputType = node.OutputType;
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
                // For NamedStruct (cycle-rendering case for recursive named types — BugHunt-stmt
                // #35) the field's type is not readable from StructTypeSpecification (NamedStruct
                // only carries the name). Reconstruct the field's function type from the call's
                // TIC-resolved output type and the remaining argument types — same shape the
                // resolved struct's field would have.
                var callArgsSafe = new ISyntaxNode[args.Length - 1];
                Array.Copy(args, 1, callArgsSafe, 0, callArgsSafe.Length);
                var childrenSafe = callArgsSafe.SelectToArray(ReadNode);
                StructFieldAccessExpressionNode fieldAccessSafe;
                if (unwrappedType.BaseType == BaseFunnyType.NamedStruct)
                {
                    var inputTypes = childrenSafe.SelectToArray(c => c.Type);
                    var fieldFunType = FunnyType.FunOf(node.OutputType, inputTypes);
                    fieldAccessSafe = new StructFieldAccessExpressionNode(id, cachedSource, node.Interval, fieldFunType);
                }
                else
                {
                    fieldAccessSafe = new StructFieldAccessExpressionNode(id, cachedSource, node.Interval);
                }
                if (fieldAccessSafe.Type.FunTypeSpecification == null)
                    throw Errors.FieldIsNotCallable(id, fieldAccessSafe.Type, node.Interval);
                var hiOrderFuncSafe = ConcreteHiOrderFunctionWithSyntaxNode.Create(fieldAccessSafe);
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

            IConcreteFunction function;
            try {
                function = genericFunction.CreateConcrete(genericArgs, _dialect);
            }
            catch (NFunImpossibleException e) when (e.Message == "Unsupported type for this function") {
                // CreateConcrete's default-case panic fires when TIC resolved a
                // generic param to a type the function's typeclass constraint
                // never accepts — e.g. `[1,none,3].sum()` resolves T=int?
                // (the Arithmetical constraint should reject Optional but
                // doesn't always at TIC-level). Surface as a typed parse error.
                // Bug hunt round 3 #14.
                throw new NFun.Exceptions.FunnyParseException(
                    783,
                    $"'{id}' is not defined for argument type{(genericArgs.Length == 1 ? "" : "s")}: " +
                    string.Join(", ", genericArgs.Select(g => g.ToString())),
                    node.Interval);
            }

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
            var functionName = ExpressionParser.GetOperatorFunctionName(op.Type)
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

        // Scoping rule: a variable is guaranteed-assigned after if-elif-else
        // ONLY if every branch (including the else) assigned it (per
        // Statements.md §Scoping). Branch-specific vars must NOT leak — using
        // them after the if would read the type's default (BugHunt-stmt #67).
        // When there is NO real else (auto-inserted DefaultValueSyntaxNode),
        // nothing is guaranteed-assigned — scope away everything.
        bool hasRealElse = node.ElseExpr is not DefaultValueSyntaxNode;
        var preIf = SnapshotVariableNames();

        // Compute per-branch ASSIGNED names by walking syntax: if every branch
        // (including else) assigns `x`, it's guaranteed-assigned downstream.
        // We do this against the syntax tree because runtime VariableSources
        // are shared across branches once any branch adds the name, so per-
        // branch _variables.added would be empty after the first branch.
        System.Collections.Generic.HashSet<string> guaranteed = null;
        if (hasRealElse)
        {
            for (int i = 0; i < node.Ifs.Length; i++) {
                var branchAssigned = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
                CollectBodyLocalEquationNames(node.Ifs[i].Expression, branchAssigned);
                guaranteed = guaranteed == null
                    ? branchAssigned
                    : Intersect(guaranteed, branchAssigned);
            }
            var elseAssigned = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            CollectBodyLocalEquationNames(node.ElseExpr, elseAssigned);
            guaranteed = guaranteed == null ? elseAssigned : Intersect(guaranteed, elseAssigned);
        }

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

        if (!hasRealElse)
        {
            RemoveVariablesAddedSince(preIf);
        }
        else
        {
            // Strip every var added since preIf that isn't guaranteed-assigned.
            var toRemove = new System.Collections.Generic.List<string>();
            foreach (var v in _variables.GetAll()) {
                if (preIf.Contains(v.Name)) continue;
                if (!v.IsOutput) continue;
                if (guaranteed != null && guaranteed.Contains(v.Name)) continue;
                toRemove.Add(v.Name);
            }
            foreach (var name in toRemove)
                _variables.TryRemove(name);
        }

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
                _dialect.Converter.TypeBehaviour.CoerceParsedRealLiteral(
                    _dialect.Converter.TypeBehaviour.ParseOrNull(d) ?? throw Errors.CannotParseDecimalNumber(node.Interval),
                    primitive),
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

        // Collect body-local equation names (multi-line lambda's `y = ...`) — these
        // must stay private to the lambda (BugHunt-stmt #36). Closures over outer scope
        // are recognised by NOT being defined as equations in the body.
        var bodyLocalNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CollectBodyLocalEquationNames(body, bodyLocalNames);

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

        //Add closured vars to outer-scope dictionary so the outer compiler sees them
        //(needed for unbound-input inference and outer-error analysis). Exclude
        //body-local equation names — those are private to the lambda's body scope
        //and must not surface as top-level outputs.
        foreach (var newVar in closured)
            if (!bodyLocalNames.Contains(newVar.Name))
                _variables.TryAdd(newVar);

        var fun = ConcreteUserFunction.Create(
            isRecursive: false,
            name: "rule",
            variables: arguments,
            expression: expr,
            isLambda: true);
        // Captures = every VariableSource the body reads that is not one of the
        // rule's own arguments. At Calc() these get snapshotted so the returned
        // closure is independent of subsequent writes to the enclosing scope
        // (Specs/Rules.md "Capturing variables").
        var captures = CollectCaptures(expr, arguments);
        return new FunRuleExpressionNode(fun, captures, interval);
    }

    /// <summary>
    /// Collects names of equation-introduced locals inside a lambda body (multi-line
    /// `fun(x): y = x+1; …`). These must NOT leak to the enclosing scope after the
    /// lambda is built. Recurses into nested blocks (if/while/for bodies) but stops at
    /// inner lambda definitions — those have their own scope.
    /// </summary>
    private static void CollectBodyLocalEquationNames(ISyntaxNode node, HashSet<string> sink) {
        if (node == null) return;
        // Stop at nested lambda/function definitions — they introduce their own scope.
        if (node is AnonymFunctionSyntaxNode or SuperAnonymFunctionSyntaxNode
                or UserFunctionDefinitionSyntaxNode)
            return;
        if (node is EquationSyntaxNode eq)
            sink.Add(eq.Id);
        foreach (var child in node.Children)
            CollectBodyLocalEquationNames(child, sink);
    }

    private static HashSet<string> Intersect(HashSet<string> a, HashSet<string> b) {
        var result = new HashSet<string>(a, System.StringComparer.OrdinalIgnoreCase);
        result.IntersectWith(b);
        return result;
    }

    private static VariableSource[] CollectCaptures(IExpressionNode body, VariableSource[] ruleArguments) {
        var unique = new HashSet<VariableSource>();
        // Variables that are LOCAL to the rule body (loop iterators, catch-error
        // bindings) must NOT be treated as captures — they're rebound at
        // runtime by the enclosing loop / try-catch node. Snapshotting them
        // into the closure breaks for-loop iteration inside `fun(args):`
        // lambdas (BugHunt-stmt #63: body reads stale snapshot instead of
        // the live iterator).
        var bodyLocal = new HashSet<VariableSource>();
        CollectBodyLocalSources(body, bodyLocal);
        CollectVariableSources(body, unique);
        if (unique.Count == 0)
            return Array.Empty<VariableSource>();
        for (int i = 0; i < ruleArguments.Length; i++)
            unique.Remove(ruleArguments[i]);
        unique.ExceptWith(bodyLocal);
        if (unique.Count == 0)
            return Array.Empty<VariableSource>();
        var result = new VariableSource[unique.Count];
        unique.CopyTo(result);
        return result;
    }

    private static void CollectBodyLocalSources(IRuntimeNode node, HashSet<VariableSource> sink) {
        switch (node) {
            case Nodes.ForExpressionNode forNode:
                if (forNode.IteratorVar != null) sink.Add(forNode.IteratorVar);
                break;
            case FunRuleExpressionNode:
                // Nested rule has its own scope — its iterators are its concern.
                return;
        }
        foreach (var child in node.Children)
            CollectBodyLocalSources(child, sink);
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
        else if (type.BaseType == BaseFunnyType.List)
            CheckGenericDepths(type.ListTypeSpecification.FunnyType, genericIndex, depth + 1,
                ref hasDepth0, ref hasNonZeroDepth);
        else if (type.BaseType == BaseFunnyType.MutableArray)
            CheckGenericDepths(type.MutableArrayTypeSpecification.FunnyType, genericIndex, depth + 1,
                ref hasDepth0, ref hasNonZeroDepth);
        else if (type.BaseType == BaseFunnyType.FixedArray)
            CheckGenericDepths(type.FixedArrayTypeSpecification.FunnyType, genericIndex, depth + 1,
                ref hasDepth0, ref hasNonZeroDepth);
        else if (type.BaseType == BaseFunnyType.Set)
            CheckGenericDepths(type.SetTypeSpecification.FunnyType, genericIndex, depth + 1,
                ref hasDepth0, ref hasNonZeroDepth);
        else if (type.BaseType == BaseFunnyType.Enumerable)
            CheckGenericDepths(type.EnumerableTypeSpecification.FunnyType, genericIndex, depth + 1,
                ref hasDepth0, ref hasNonZeroDepth);
        else if (type.BaseType == BaseFunnyType.Map) {
            CheckGenericDepths(type.MapTypeSpecification.KeyType, genericIndex, depth + 1,
                ref hasDepth0, ref hasNonZeroDepth);
            CheckGenericDepths(type.MapTypeSpecification.ValueType, genericIndex, depth + 1,
                ref hasDepth0, ref hasNonZeroDepth);
        }
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

    public IExpressionNode Visit(BlockSyntaxNode node) {
        var statements = node.Statements;
        var exprNodes = new IExpressionNode[statements.Count];
        var assignments = new Nodes.VariableAssignment[statements.Count];

        for (int i = 0; i < statements.Count; i++) {
            var stmt = statements[i];
            if (stmt is EquationSyntaxNode eq) {
                // Build the expression for the equation body
                var bodyExpr = ReadNode(eq.Expression);

                // Reuse existing variable source if variable already exists (reassignment).
                // This ensures that loop bodies write to the same source that the outer scope reads,
                // enabling patterns like: total = 0; for item in arr: total = total + item
                var existing = _variables.GetOrNull(eq.Id);
                Runtime.VariableSource source;
                if (existing != null)
                {
                    ReassignmentGuard.ThrowIfIncompatibleVariableReassignment(
                        eq.Id, existing, bodyExpr.Type, eq.Interval);
                    source = existing;
                }
                else
                {
                    var varType = eq.OutputType != FunnyType.Empty ? eq.OutputType : bodyExpr.Type;
                    // Carry the type-annotation interval through to VariableSource so the
                    // reassignment Any-widening check above can tell user-declared `x:any`
                    // (intentional) from TIC-inferred `Any` (incompatible reassignment).
                    if (eq.OutputTypeSpecified)
                        source = Runtime.VariableSource.CreateWithStrictTypeLabel(
                            eq.Id, varType, eq.TypeSpecificationOrNull.Interval,
                            Runtime.FunnyVarAccess.Output, _dialect.Converter);
                    else
                        source = Runtime.VariableSource.CreateWithoutStrictTypeLabel(
                            eq.Id, varType, Runtime.FunnyVarAccess.Output, _dialect.Converter);
                    _variables.AddOrReplace(source);
                }

                // Cast expression if types differ (e.g., int literal to real variable)
                var castExpr = Nodes.CastExpressionNode.GetConvertedOrOriginOrThrow(
                    bodyExpr, source.Type, _dialect.Converter.TypeBehaviour);

                exprNodes[i] = castExpr;
                assignments[i] = new Nodes.VariableAssignment(source);
            }
            else {
                exprNodes[i] = ReadNode(stmt);
            }
        }

        return new Nodes.BlockExpressionNode(exprNodes, assignments, node.OutputType, node.Interval);
    }

    public IExpressionNode Visit(ReturnSyntaxNode node) {
        IExpressionNode expr = null;
        if (node.Expression != null) {
            expr = ReadNode(node.Expression);
            // Cast at the return-statement boundary when the expression's
            // runtime type differs from the function's declared return type.
            // TIC's return-type LCA may "accept" a branch through a C-style
            // conversion (e.g. LCA(Bool, [U8..Re]) leaves x:int but forces
            // body usage to Bool); without this cast the raw value bypasses
            // the conversion and lands in a typed slot it doesn't match.
            // (StmtBug75b.)
            if (_functionReturnType.BaseType != BaseFunnyType.Empty
                && expr.Type != _functionReturnType)
            {
                expr = CastExpressionNode.GetConvertedOrOriginOrThrow(
                    expr, _functionReturnType, _dialect.Converter.TypeBehaviour);
            }
        }
        return new Nodes.ReturnExpressionNode(expr, node.OutputType, node.Interval);
    }

    public IExpressionNode Visit(ForSyntaxNode node) {
        var collectionExpr = ReadNode(node.Collection);

        // Determine iterator element type from the collection type.
        // TIC already inferred this — we just dispatch over every single-element
        // collection kind to pull the FunnyType out of the right specification.
        // Map's pair-struct iteration is intentionally out of scope here; the
        // pair-struct synthesis lives in TIC's CompCs cross-Apply.
        // Single source of truth for "what does iterating this type yield" —
        // lives on FunnyType so the parser, runtime and TIC adapter stay aligned.
        var collectionType = collectionExpr.Type;
        var elementType = collectionType.GetEnumerableElementTypeOrNull() ?? FunnyType.Any;

        // Snapshot scope BEFORE adding iterator: iterator + body locals all
        // belong to the loop scope and must not leak after the loop ends.
        var preBody = SnapshotVariableNames();

        // Iterator shadows any outer variable of the same name for the duration
        // of the body, but the outer must be restored afterwards (Statements.md
        // §Scoping line 32: "The iterator of `for x in xs:` is bound for the
        // body only."). Save → replace → restore (BugHunt-stmt #64).
        var shadowed = _variables.GetOrNull(node.IteratorName);

        // Create iterator variable
        var iteratorVar = Runtime.VariableSource.CreateWithoutStrictTypeLabel(
            node.IteratorName, elementType, Runtime.FunnyVarAccess.Output, _dialect.Converter);
        _variables.AddOrReplace(iteratorVar);

        var bodyExpr = ReadNode(node.Body);
        RemoveVariablesAddedSince(preBody);
        // Whether or not the iterator name was in `preBody`, `AddOrReplace`
        // overwrote any prior entry. Restore the shadowed binding so post-loop
        // code reads the original VariableSource (and its prior value).
        if (shadowed != null)
            _variables.AddOrReplace(shadowed);
        else
            _variables.TryRemove(node.IteratorName);

        return new Nodes.ForExpressionNode(collectionExpr, bodyExpr, iteratorVar, node.OutputType, node.Interval);
    }

    public IExpressionNode Visit(WhileSyntaxNode node) {
        var conditionExpr = ReadNode(node.Condition);
        var bodyExpr = ReadScopedBody(node.Body);
        return new Nodes.WhileExpressionNode(conditionExpr, bodyExpr, node.OutputType, node.Interval);
    }

    public IExpressionNode Visit(WhenSyntaxNode node) {
        IExpressionNode subjectExpr = null;
        if (node.Subject != null)
            subjectExpr = ReadNode(node.Subject);

        var arms = new (IExpressionNode condition, IExpressionNode body)[node.Arms.Length];
        for (int i = 0; i < node.Arms.Length; i++) {
            var armCondition = ReadNode(node.Arms[i].Condition);
            // Each arm body opens its own scope — locals don't leak across arms.
            var armBody = ReadScopedBody(node.Arms[i].Body);
            // Cast arm body to output type if needed (for expression form with else)
            if (node.ElseBody != null)
                armBody = Nodes.CastExpressionNode.GetConvertedOrOriginOrThrow(
                    armBody, node.OutputType, _dialect.Converter.TypeBehaviour);
            arms[i] = (armCondition, armBody);
        }

        IExpressionNode elseExpr = null;
        if (node.ElseBody != null) {
            elseExpr = ReadScopedBody(node.ElseBody);
            elseExpr = Nodes.CastExpressionNode.GetConvertedOrOriginOrThrow(
                elseExpr, node.OutputType, _dialect.Converter.TypeBehaviour);
        }

        return new Nodes.WhenExpressionNode(subjectExpr, arms, elseExpr, node.OutputType, node.Interval);
    }

    /// <summary>
    /// Snapshot the set of variable names currently registered. Use with
    /// <see cref="RemoveVariablesAddedSince"/> to scope a block — variables
    /// introduced inside the block are removed when the block exits and
    /// don't leak to the enclosing scope.
    /// </summary>
    private System.Collections.Generic.HashSet<string> SnapshotVariableNames() {
        var snap = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var v in _variables.GetAll())
            snap.Add(v.Name);
        return snap;
    }

    private System.Collections.Generic.HashSet<string> NamesAddedSince(System.Collections.Generic.HashSet<string> snapshot) {
        var added = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var v in _variables.GetAll())
            if (!snapshot.Contains(v.Name)) added.Add(v.Name);
        return added;
    }

    private void RemoveVariablesAddedSince(System.Collections.Generic.HashSet<string> snapshot) {
        // Only remove block-local OUTPUT variables (assignments inside the
        // block). INPUTs created during the block's body resolve to script-
        // level inputs and must NOT be scoped away — otherwise sibling
        // branches encountering the same input would each get a separate
        // VariableSource (e.g. `if(x==1) 1 else if(x==2) 2` would have two
        // different `x`-sources, one in each branch).
        var toRemove = new System.Collections.Generic.List<string>();
        foreach (var v in _variables.GetAll())
            if (!snapshot.Contains(v.Name) && v.IsOutput) toRemove.Add(v.Name);
        foreach (var name in toRemove)
            _variables.TryRemove(name);
    }

    /// <summary>
    /// Build a block expression inside a fresh lexical scope. Any variables
    /// introduced inside <paramref name="body"/> are removed from
    /// <c>_variables</c> after the build so they don't leak. Function-body
    /// blocks (called from BuildConcrete with a fresh variables dict) do
    /// NOT use this — their locals are tracked for the function's runtime
    /// stack-frame semantics.
    /// </summary>
    private IExpressionNode ReadScopedBody(ISyntaxNode body) {
        var snap = SnapshotVariableNames();
        var expr = ReadNode(body);
        RemoveVariablesAddedSince(snap);
        return expr;
    }

    public IExpressionNode Visit(WhenArmSyntaxNode node)
        => ThrowNotAnExpression(node);

    public IExpressionNode Visit(BreakSyntaxNode node)
        => new Nodes.BreakExpressionNode(node.OutputType, node.Interval);

    public IExpressionNode Visit(ContinueSyntaxNode node)
        => new Nodes.ContinueExpressionNode(node.OutputType, node.Interval);

    public IExpressionNode Visit(PrintSyntaxNode node) {
        // Print evaluates its expression. The actual print() call is wired at the parser level
        // as a FunCallSyntaxNode. This handler exists for the statement-form `print expr`.
        var expr = ReadNode(node.Expression);
        return expr;
    }

    public IExpressionNode Visit(TryBlockSyntaxNode node) {
        var tryExpr = ReadScopedBody(node.TryBody);

        // Pre-create the error variable before visiting catch body, so Visit(NamedIdSyntaxNode)
        // finds it as a script-scoped local rather than emitting an unbound input. The variable
        // is removed from _variables once the catch body is built — it must not leak to the
        // script-level variable list.
        VariableSource errorVar = null;
        VariableSource shadowedOuter = null;
        if (node.CatchBody != null && node.ErrorVariableName != null) {
            var aliasName = node.OrderNumber + "~" + node.ErrorVariableName;
            var aliasType = _typeInferenceResults.GetVariableTypeOrNull(aliasName);
            var errorType = aliasType != null
                ? _typesConverter.Convert(aliasType)
                : FunnyType.Any;
            errorVar = VariableSource.CreateWithoutStrictTypeLabel(
                node.ErrorVariableName, errorType, FunnyVarAccess.NoInfo, _dialect.Converter);
            // Save and replace any outer variable of the same name so the catch
            // body sees the error binding instead of a stale outer var of the
            // wrong type — without this, reading `e.message` over an outer
            // `e: Int32` crashed with NullReferenceException
            // (BugHunt-stmt #65). Per Statements.md §Scoping line 32:
            // "`catch e:` binds `e` only inside the catch body."
            shadowedOuter = _variables.GetOrNull(node.ErrorVariableName);
            _variables.AddOrReplace(errorVar);
        }

        // Catch body opens a scope that includes the error variable. ReadScopedBody
        // removes both the error var and any catch-local bindings when done.
        IExpressionNode catchExpr = null;
        if (node.CatchBody != null) {
            catchExpr = ReadScopedBody(node.CatchBody);
            // Cast catch body to output type if needed
            catchExpr = Nodes.CastExpressionNode.GetConvertedOrOriginOrThrow(
                catchExpr, node.OutputType, _dialect.Converter.TypeBehaviour);
        }
        // The scoped cleanup above removes errorVar too; restore any outer
        // binding that was shadowed for the catch body's duration.
        if (errorVar != null) {
            _variables.TryRemove(node.ErrorVariableName);
            if (shadowedOuter != null)
                _variables.AddOrReplace(shadowedOuter);
        }

        IExpressionNode anywayExpr = null;
        if (node.AnywayBody != null)
            anywayExpr = ReadScopedBody(node.AnywayBody);

        IExpressionNode result;
        if (catchExpr != null) {
            // Cast try body to output type for expression form
            var tryBody = Nodes.CastExpressionNode.GetConvertedOrOriginOrThrow(
                tryExpr, node.OutputType, _dialect.Converter.TypeBehaviour);
            result = new Nodes.TryCatchExpressionNode(
                tryBody, catchExpr, errorVar, node.Interval, node.OutputType);
        } else {
            // Try with only anyway (no catch)
            result = tryExpr;
        }

        if (anywayExpr != null)
            result = new Nodes.TryAnywayExpressionNode(result, anywayExpr, node.OutputType, node.Interval);

        return result;
    }

    public IExpressionNode Visit(IfBlockSyntaxNode node) {
        // Build like IfElse: parallel arrays of conditions and bodies.
        // Each branch body opens its own scope — locals don't leak across
        // branches or out to the enclosing block.
        var conditions = new IExpressionNode[node.Ifs.Length];
        var bodies = new IExpressionNode[node.Ifs.Length];
        for (int i = 0; i < node.Ifs.Length; i++) {
            conditions[i] = ReadNode(node.Ifs[i].Condition);
            bodies[i] = ReadScopedBody(node.Ifs[i].Expression);
        }

        IExpressionNode elseExpr;
        if (node.ElseBody != null)
            elseExpr = ReadScopedBody(node.ElseBody);
        else
            elseExpr = new Nodes.ConstantExpressionNode(
                Types.FunnyNone.Instance, FunnyType.None, node.Interval);

        return new Nodes.IfElseExpressionNode(bodies, conditions, elseExpr, node.Interval, node.OutputType);
    }

    public IExpressionNode Visit(IndexedAssignmentSyntaxNode node) {
        var targetExpr = ReadNode(node.Target);
        var indexExpr = ReadNode(node.Index);
        var valueExpr = ReadNode(node.Value);

        // Bug hunt round 10 #53. The ee-mode legacy `T[]` (BaseFunnyType.ArrayOf)
        // is immutable. `text` is aliased to ArrayOf(Char) (FunnyType.cs:25), so
        // `t:text = 'hello'; t[0] = /'H'` would route through this builder with a
        // solved StateArray target — and TIC's CompCsApply later asserts
        // "Node is already solved" because the indexed-write tries to rebind
        // the element of a fully-resolved immutable composite. Reject at the
        // expression-builder boundary with a typed error matching what `.add()`
        // already produces on `:text`. Per `specs_lang/Texts.md` §5: "Text is
        // immutable - this means that after creating the text, it cannot be
        // changed - only create a new one based on the previous one." Lang-mode
        // mutable kinds (list<T>, array<T>) continue to support indexed write.
        if (targetExpr.Type.BaseType == BaseFunnyType.ArrayOf)
            throw Errors.IndexedWriteOnImmutableArray(targetExpr.Type, node.Interval);

        // Cast value to the container's element type so e.g. `a:array<real>;
        // a[0] = 1` widens int → real before storage. Both list<T> and
        // array<T> need the same shape.
        //
        // Strictness must match variable initialization: `y:int = 1.5` is FU740
        // (Real ≰ Int), so `x:int[]; x[0] = 1.5` must also reject. TIC skips
        // binding indexed-write (workaround #7), so the check lives here.
        // VarTypeConverter.CanBeConverted is the strict implicit-conversion
        // table (no Real→Int truncation, no Int32→UInt8 narrowing) — matches
        // what TIC would enforce if the assignment were bound. (Bug hunt #35+#36.)
        FunnyType elemType = targetExpr.Type.BaseType switch {
            BaseFunnyType.List => targetExpr.Type.ListTypeSpecification.FunnyType,
            BaseFunnyType.MutableArray => targetExpr.Type.MutableArrayTypeSpecification.FunnyType,
            _ => default
        };
        if (elemType.BaseType != BaseFunnyType.Empty) {
            if (!VarTypeConverter.CanBeConverted(valueExpr.Type, elemType))
                throw Errors.ImpossibleCast(valueExpr.Type, elemType, node.Value.Interval);
            valueExpr = Nodes.CastExpressionNode.GetConvertedOrOriginOrThrow(
                valueExpr, elemType, _dialect.Converter.TypeBehaviour);
        }

        return new Nodes.IndexedAssignExpressionNode(
            targetExpr, indexExpr, valueExpr, node.Interval, targetExpr.Type);
    }

    public IExpressionNode Visit(FieldAssignmentSyntaxNode node) {
        var sourceExpr = ReadNode(node.Source);
        var valueExpr = ReadNode(node.Value);

        // BugHunt-stmt #59 pin — see ReassignmentGuard for semantics and the
        // declared-`any` named-type exemption.
        ReassignmentGuard.ThrowIfIncompatibleFieldAssignment(
            node.FieldName, sourceExpr.Type, valueExpr.Type, _namedTypes, node.Interval);

        return new Nodes.FieldAssignExpressionNode(
            node.FieldName, sourceExpr, valueExpr, node.Interval, sourceExpr.Type,
            variableName: node.VariableName);
    }
}
