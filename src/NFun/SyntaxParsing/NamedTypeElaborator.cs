using System;
using System.Collections.Generic;
using System.Linq;
using NFun.ParseErrors;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tokenization;

namespace NFun.SyntaxParsing;

/// <summary>
/// Stores a named type definition extracted from TypeDeclarationSyntaxNode.
/// </summary>
internal class NamedTypeDefinition {
    public string Name { get; }
    /// <summary>Non-null for struct types</summary>
    public IReadOnlyList<TypeFieldDefinition> Fields { get; }
    /// <summary>Non-null for type aliases</summary>
    public TypeSyntax AliasTypeSyntax { get; }
    public bool IsAlias => AliasTypeSyntax is not null and not TypeSyntax.EmptyType;

    public NamedTypeDefinition(string name, IReadOnlyList<TypeFieldDefinition> fields) {
        Name = name;
        Fields = fields;
    }

    public NamedTypeDefinition(string name, TypeSyntax aliasType) {
        Name = name;
        AliasTypeSyntax = aliasType;
    }
}

/// <summary>
/// Pre-TIC elaboration pass that:
/// 1. Collects type declarations from the syntax tree
/// 2. Expands NamedTypeConstructorSyntaxNode into StructInitSyntaxNode (filling defaults)
/// 3. Removes TypeDeclarationSyntaxNode from the tree
///
/// After this pass, TIC sees only anonymous structs — zero TIC changes needed.
/// </summary>
internal static class NamedTypeElaborator {

    /// <summary>
    /// Run elaboration on the syntax tree. Mutates the tree in place.
    /// Also outputs type definitions for use in type annotation resolution.
    /// </summary>
    public static SyntaxTree Elaborate(SyntaxTree tree, out Dictionary<string, NamedTypeDefinition> typeDefinitions) {
        // Phase 1: collect type declarations
        var typeRegistry = new Dictionary<string, NamedTypeDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in tree.Nodes)
        {
            if (node is TypeDeclarationSyntaxNode typeDecl)
            {
                if (IsPrimitiveTypeName(typeDecl.TypeName))
                    throw Errors.NamedTypeAlreadyDefined(typeDecl.TypeName, typeDecl.Interval);
                if (typeRegistry.ContainsKey(typeDecl.TypeName))
                    throw Errors.NamedTypeAlreadyDefined(typeDecl.TypeName, typeDecl.Interval);
                if (typeDecl.IsAlias)
                {
                    // Type alias: type name = typeSyntax
                    typeRegistry[typeDecl.TypeName] = new NamedTypeDefinition(typeDecl.TypeName, typeDecl.AliasTypeSyntax);
                }
                else
                {
                    // Struct type: validate defaults, register
                    foreach (var field in typeDecl.FieldDefinitions)
                        if (field.HasDefault)
                            ValidateConstantExpression(field.DefaultValue, typeDecl.TypeName, field.Name);
                    typeRegistry[typeDecl.TypeName] = new NamedTypeDefinition(typeDecl.TypeName, typeDecl.FieldDefinitions);
                }
            }
        }

        // Validate no non-optional recursive cycles (struct types only)
        foreach (var typeDef in typeRegistry.Values)
            if (!typeDef.IsAlias)
                ValidateNoNonOptionalCycle(typeDef.Name, typeRegistry);

        // Validate no circular alias chains (a -> b -> c -> a)
        foreach (var typeDef in typeRegistry.Values)
            if (typeDef.IsAlias)
                ValidateNoCircularAlias(typeDef.Name, typeRegistry);

        // Validate function default parameters: same constant-expression rule
        foreach (var node in tree.Nodes)
            if (node is UserFunctionDefinitionSyntaxNode func)
                foreach (var arg in func.Args)
                    if (arg.HasDefault)
                        ValidateConstantExpression(arg.DefaultValue, $"function '{func.Id}'", arg.Id);

        // Fast path: if no type declarations and no constructor nodes could exist, skip
        // We still need to check because constructor nodes without type defs should produce errors
        typeDefinitions = typeRegistry;
        bool hasTypeDecls = typeRegistry.Count > 0;
        bool hasAnyConstructors = false;
        if (!hasTypeDecls) {
            foreach (var node in tree.Nodes) {
                if (ContainsConstructor(node)) {
                    hasAnyConstructors = true;
                    break;
                }
            }
            if (!hasAnyConstructors)
                return tree;
        }

        // Phase 2: remove type declarations, expand constructors
        var newNodes = new List<ISyntaxNode>(tree.Nodes.Length);
        foreach (var node in tree.Nodes)
        {
            if (node is TypeDeclarationSyntaxNode)
                continue; // remove from tree

            newNodes.Add(ElaborateNode(node, typeRegistry));
        }

        return new SyntaxTree(newNodes.ToArray());
    }

    private static ISyntaxNode ElaborateNode(ISyntaxNode node, Dictionary<string, NamedTypeDefinition> types, HashSet<string> expanding = null) {
        return node switch {
            NamedTypeConstructorSyntaxNode ctor => ExpandConstructor(ctor, types, expanding),
            EquationSyntaxNode eq => ElaborateEquation(eq, types, expanding),
            _ => ElaborateChildren(node, types, expanding)
        };
    }

    private static EquationSyntaxNode ElaborateEquation(EquationSyntaxNode eq, Dictionary<string, NamedTypeDefinition> types, HashSet<string> expanding) {
        var newExpr = ElaborateNode(eq.Expression, types, expanding);
        var typeSpec = eq.TypeSpecificationOrNull;

        // If the expression was a named type constructor AND the equation has no type annotation,
        // inject the named type as annotation so TIC propagates field type constraints.
        // NamedStruct references resolve lazily through the registry with cycle detection,
        // so this works for all recursive types (including array-based recursion).
        if (typeSpec == null && eq.Expression is NamedTypeConstructorSyntaxNode ctor
            && types.TryGetValue(ctor.TypeName, out var ctorTypeDef))
        {
            typeSpec = new TypedVarDefSyntaxNode(
                eq.Id,
                new TypeSyntax.Named(ctor.TypeName, eq.Interval),
                eq.Interval);
        }

        if (ReferenceEquals(newExpr, eq.Expression) && typeSpec == eq.TypeSpecificationOrNull)
            return eq;
        return new EquationSyntaxNode(eq.Id, eq.Interval.Start, newExpr, eq.Attributes) {
            TypeSpecificationOrNull = typeSpec
        };
    }

    private static ISyntaxNode ElaborateChildren(ISyntaxNode node, Dictionary<string, NamedTypeDefinition> types, HashSet<string> expanding) {
        // For nodes that have children (arrays, function calls, if-else, etc.),
        // we need to recurse. Since the syntax nodes are mostly immutable,
        // we handle the common container types.
        switch (node) {
            case ArraySyntaxNode arr: {
                bool changed = false;
                var newElements = new List<ISyntaxNode>(arr.Expressions.Count);
                foreach (var el in arr.Expressions) {
                    var newEl = ElaborateNode(el, types, expanding);
                    if (!ReferenceEquals(newEl, el)) changed = true;
                    newElements.Add(newEl);
                }
                if (!changed) return node;
                return SyntaxNodeFactory.Array(newElements, arr.Interval.Start, arr.Interval.Finish);
            }

            case ListOfExpressionsSyntaxNode lof: {
                // Produced by parenthesized comma-list `(a, b)` — not a valid NFun expression
                // (no tuple syntax). The error is raised later by ExpressionBuilderVisitor as
                // FU603 'is not an expression'. But we still must recurse here so that named-type
                // ctors inside `(t{...}, t{...})` are elaborated before TIC sees them — otherwise
                // the unhandled NamedTypeConstructorSyntaxNode trips an "impossible" internal
                // exception leak
                bool changed = false;
                var newElements = new List<ISyntaxNode>(lof.Expressions.Count);
                foreach (var el in lof.Expressions) {
                    var newEl = ElaborateNode(el, types, expanding);
                    if (!ReferenceEquals(newEl, el)) changed = true;
                    newElements.Add(newEl);
                }
                if (!changed) return node;
                return SyntaxNodeFactory.ListOf(newElements, lof.Interval, lof.ParenthesesCount);
            }

            case FunCallSyntaxNode fun: {
                bool changed = false;
                var newArgs = new ISyntaxNode[fun.Args.Length];
                for (int i = 0; i < fun.Args.Length; i++) {
                    newArgs[i] = ElaborateNode(fun.Args[i], types, expanding);
                    if (!ReferenceEquals(newArgs[i], fun.Args[i])) changed = true;
                }
                if (!changed) return node;
                return new FunCallSyntaxNode(fun.Id, newArgs, fun.Interval, fun.IsPipeForward, fun.IsOperator);
            }

            case IfThenElseSyntaxNode ite: {
                bool changed = false;
                var newCases = new IfCaseSyntaxNode[ite.Ifs.Length];
                for (int i = 0; i < ite.Ifs.Length; i++) {
                    var c = ite.Ifs[i];
                    var newCond = ElaborateNode(c.Condition, types, expanding);
                    var newExpr = ElaborateNode(c.Expression, types, expanding);
                    if (!ReferenceEquals(newCond, c.Condition) || !ReferenceEquals(newExpr, c.Expression)) {
                        changed = true;
                        newCases[i] = SyntaxNodeFactory.IfCase(newCond, newExpr, c.Interval.Start, c.Interval.Finish);
                    } else {
                        newCases[i] = c;
                    }
                }
                var newElse = ElaborateNode(ite.ElseExpr, types, expanding);
                if (!ReferenceEquals(newElse, ite.ElseExpr)) changed = true;
                if (!changed) return node;
                return SyntaxNodeFactory.IfElse(newCases, newElse, ite.Interval.Start, ite.Interval.Finish);
            }

            case StructInitSyntaxNode si: {
                bool changed = false;
                var newEquations = new List<EquationSyntaxNode>(si.Fields.Count);
                foreach (var field in si.Fields) {
                    var newBody = ElaborateNode(field.Node, types, expanding);
                    if (!ReferenceEquals(newBody, field.Node)) {
                        changed = true;
                        newEquations.Add(new EquationSyntaxNode(field.Name, field.Node.Interval.Start, newBody, Array.Empty<FunnyAttribute>()));
                    } else {
                        newEquations.Add(new EquationSyntaxNode(field.Name, field.Node.Interval.Start, field.Node, Array.Empty<FunnyAttribute>()));
                    }
                }
                if (!changed) return node;
                return SyntaxNodeFactory.Struct(newEquations, si.Interval);
            }

            case StructFieldAccessSyntaxNode sfa: {
                var newSource = ElaborateNode(sfa.Source, types, expanding);
                if (ReferenceEquals(newSource, sfa.Source)) return node;
                return new StructFieldAccessSyntaxNode(newSource, sfa.FieldName,
                    new Interval(newSource.Interval.Start, sfa.Interval.Finish), sfa.IsSafeAccess);
            }

            case ResultFunCallSyntaxNode rfc: {
                bool changed = false;
                var newResult = ElaborateNode(rfc.ResultExpression, types, expanding);
                if (!ReferenceEquals(newResult, rfc.ResultExpression)) changed = true;
                var newArgs = new ISyntaxNode[rfc.Args.Length];
                for (int i = 0; i < rfc.Args.Length; i++) {
                    newArgs[i] = ElaborateNode(rfc.Args[i], types, expanding);
                    if (!ReferenceEquals(newArgs[i], rfc.Args[i])) changed = true;
                }
                if (!changed) return node;
                return new ResultFunCallSyntaxNode(newResult, newArgs, rfc.Interval);
            }

            case ComparisonChainSyntaxNode cmp: {
                bool changed = false;
                var newOperands = new List<ISyntaxNode>(cmp.Operands.Count);
                foreach (var op in cmp.Operands) {
                    var newOp = ElaborateNode(op, types, expanding);
                    if (!ReferenceEquals(newOp, op)) changed = true;
                    newOperands.Add(newOp);
                }
                if (!changed) return node;
                return new ComparisonChainSyntaxNode(newOperands, cmp.Operators);
            }

            case SuperAnonymFunctionSyntaxNode saf: {
                var newBody = ElaborateNode(saf.Body, types, expanding);
                if (ReferenceEquals(newBody, saf.Body)) return node;
                return new SuperAnonymFunctionSyntaxNode(newBody);
            }

            case BinOperatorSyntaxNode bin: {
                var newLeft = ElaborateNode(bin.Left, types, expanding);
                var newRight = ElaborateNode(bin.Right, types, expanding);
                if (ReferenceEquals(newLeft, bin.Left) && ReferenceEquals(newRight, bin.Right))
                    return node;
                return new BinOperatorSyntaxNode(bin.Op, newLeft, newRight, bin.Interval);
            }

            case UnaryOperatorSyntaxNode un: {
                var newOperand = ElaborateNode(un.Operand, types, expanding);
                if (ReferenceEquals(newOperand, un.Operand)) return node;
                return new UnaryOperatorSyntaxNode(un.Op, newOperand, un.Interval);
            }

            case TryCatchSyntaxNode tc: {
                var newTry = ElaborateNode(tc.TryExpr, types, expanding);
                var newCatch = ElaborateNode(tc.CatchExpr, types, expanding);
                if (ReferenceEquals(newTry, tc.TryExpr) && ReferenceEquals(newCatch, tc.CatchExpr))
                    return node;
                return new TryCatchSyntaxNode(newTry, newCatch, tc.ErrorVariableName, tc.Interval);
            }

            case UserFunctionDefinitionSyntaxNode func: {
                var newBody = ElaborateNode(func.Body, types, expanding);
                // Also elaborate default-value expressions for parameters — a default
                // like `a: p = p{x=0, y=0}` carries a NamedTypeConstructorSyntaxNode
                // that must be expanded here (BugHunt-stmt #52); otherwise it
                // survives to ExpressionBuilderVisitor and throws "should be
                // removed during elaboration".
                var newArgs = ElaborateArgsDefaults(func.Args, types, expanding, out bool argsChanged);
                if (!argsChanged && ReferenceEquals(newBody, func.Body)) return node;
                return new UserFunctionDefinitionSyntaxNode(newArgs, func.Head, newBody, func.ReturnTypeSyntax);
            }

            case AnonymFunctionSyntaxNode anon: {
                var newBody = ElaborateNode(anon.Body, types, expanding);
                if (ReferenceEquals(newBody, anon.Body)) return node;
                return SyntaxNodeFactory.AnonymFunction(anon.Definition, anon.ReturnTypeSyntax, newBody);
            }

            default:
                return node;
        }
    }

    /// <summary>
    /// Walk an arg list and elaborate each DefaultValue. Returns the same list
    /// instance when nothing changed. Preserves order, type spec, params/keyword
    /// flags.
    /// </summary>
    private static System.Collections.Generic.IList<TypedVarDefSyntaxNode> ElaborateArgsDefaults(
        System.Collections.Generic.IList<TypedVarDefSyntaxNode> args,
        Dictionary<string, NamedTypeDefinition> types,
        HashSet<string> expanding,
        out bool changed) {
        changed = false;
        System.Collections.Generic.List<TypedVarDefSyntaxNode> result = null;
        for (int i = 0; i < args.Count; i++) {
            var a = args[i];
            if (a.HasDefault) {
                var newDef = ElaborateNode(a.DefaultValue, types, expanding);
                if (!ReferenceEquals(newDef, a.DefaultValue)) {
                    changed = true;
                    if (result == null) {
                        result = new System.Collections.Generic.List<TypedVarDefSyntaxNode>(args.Count);
                        for (int j = 0; j < i; j++) result.Add(args[j]);
                    }
                    result.Add(new TypedVarDefSyntaxNode(a.Id, a.TypeSyntax, a.Interval, newDef,
                        isParams: a.IsParams, isKeywordOnly: a.IsKeywordOnly));
                    continue;
                }
            }
            result?.Add(a);
        }
        return changed ? result : args;
    }

    /// <summary>
    /// Expand a NamedTypeConstructorSyntaxNode into a StructInitSyntaxNode,
    /// filling in default values for missing fields.
    /// </summary>
    private static ISyntaxNode ExpandConstructor(
        NamedTypeConstructorSyntaxNode ctor,
        Dictionary<string, NamedTypeDefinition> types,
        HashSet<string> expanding) {

        if (!types.TryGetValue(ctor.TypeName, out var typeDef))
            throw Errors.NamedTypeNotDefined(ctor.TypeName, ctor.TypeNameInterval);

        // Follow alias chain to the actual struct type (bounded to prevent cycles)
        var resolvedName = ctor.TypeName;
        for (int depth = 0; typeDef.IsAlias; depth++) {
            if (depth > 100
                || typeDef.AliasTypeSyntax is not TypeSyntax.Named aliasNamed
                || !types.TryGetValue(aliasNamed.Name, out typeDef))
                throw Errors.NamedTypeNotDefined(resolvedName, ctor.TypeNameInterval);
            resolvedName = aliasNamed.Name;
        }

        // Build a set of provided field names
        var providedFields = new Dictionary<string, ISyntaxNode>(StringComparer.OrdinalIgnoreCase);
        foreach (var eq in ctor.ProvidedFields) {
            // Check field exists in type
            if (!typeDef.Fields.Any(f => string.Equals(f.Name, eq.Id, StringComparison.OrdinalIgnoreCase)))
                throw Errors.NamedTypeUnknownField(ctor.TypeName, eq.Id, eq.Interval);
            // Check for duplicate field
            if (providedFields.ContainsKey(eq.Id))
                throw Errors.NamedTypeDuplicateField(ctor.TypeName, eq.Id, eq.Interval);
            // Elaborate the provided expression (it might contain nested constructors)
            providedFields[eq.Id] = ElaborateNode(eq.Expression, types, expanding);
        }

        // Build the full field list
        var equations = new List<EquationSyntaxNode>(typeDef.Fields.Count);
        foreach (var fieldDef in typeDef.Fields) {
            ISyntaxNode fieldExpr;
            if (providedFields.TryGetValue(fieldDef.Name, out var provided)) {
                fieldExpr = provided;
            } else if (fieldDef.HasDefault) {
                // Use the default expression, elaborating any nested constructors.
                // Guard: detect recursive defaults (`type node = {next:node? = node{v=0}}`)
                // to prevent stack overflow. The expanding-set is created lazily on first
                // default elaboration and threaded through ElaborateNode's recursion.
                expanding ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (!expanding.Add(resolvedName))
                    throw Errors.NamedTypeRecursiveDefault(resolvedName, ctor.Interval);
                try {
                    fieldExpr = ElaborateNode(fieldDef.DefaultValue, types, expanding);
                } finally {
                    expanding.Remove(resolvedName);
                }
            } else {
                throw Errors.NamedTypeMissingRequiredField(ctor.TypeName, fieldDef.Name, ctor.Interval);
            }

            equations.Add(new EquationSyntaxNode(fieldDef.Name, fieldExpr.Interval.Start, fieldExpr, Array.Empty<FunnyAttribute>()));
        }

        var structInit = (StructInitSyntaxNode)SyntaxNodeFactory.Struct(equations, ctor.Interval);
        // Set OutputType so TIC knows this struct is a named type instance.
        // This provides field type constraints (especially Optional for defaults)
        // at any nesting depth without depth-limited expansion.
        structInit.OutputType = FunnyType.NamedStructOf(resolvedName);
        return structInit;
    }

    private static bool ContainsConstructor(ISyntaxNode node) {
        if (node is NamedTypeConstructorSyntaxNode)
            return true;
        foreach (var child in node.Children)
            if (ContainsConstructor(child))
                return true;
        return false;
    }

    /// <summary>
    /// Validate that a default expression is a constant: literals, operators on literals,
    /// array/struct literals. No variable references, no function calls.
    /// </summary>
    private static void ValidateConstantExpression(ISyntaxNode node, string typeName, string fieldName) {
        switch (node) {
            // Literals — always OK
            case ConstantSyntaxNode:
            case GenericIntSyntaxNode:
            case DefaultValueSyntaxNode:
            case IpAddressConstantSyntaxNode:
                return;
            // Operators on constants — recurse into operands
            case BinOperatorSyntaxNode bin:
                ValidateConstantExpression(bin.Left, typeName, fieldName);
                ValidateConstantExpression(bin.Right, typeName, fieldName);
                return;
            case UnaryOperatorSyntaxNode un:
                ValidateConstantExpression(un.Operand, typeName, fieldName);
                return;
            // Array literal — recurse into elements
            case ArraySyntaxNode arr:
                foreach (var el in arr.Expressions)
                    ValidateConstantExpression(el, typeName, fieldName);
                return;
            // Struct literal — recurse into field values
            case StructInitSyntaxNode si:
                foreach (var field in si.Fields)
                    ValidateConstantExpression(field.Node, typeName, fieldName);
                return;
            // Named type constructor in default — recurse
            case NamedTypeConstructorSyntaxNode ctor:
                foreach (var eq in ctor.ProvidedFields)
                    ValidateConstantExpression(eq.Expression, typeName, fieldName);
                return;
            // Variable reference — FORBIDDEN
            case NamedIdSyntaxNode id:
                throw Errors.NamedTypeDefaultCannotReferenceVariable(typeName, fieldName, id.Id, node.Interval);
            // Function call — FORBIDDEN
            case FunCallSyntaxNode:
            case ResultFunCallSyntaxNode:
                throw Errors.NamedTypeDefaultCannotCallFunction(typeName, fieldName, node.Interval);
            // Anything else — FORBIDDEN
            default:
                throw Errors.NamedTypeDefaultMustBeConstant(typeName, fieldName, node.Interval);
        }
    }

    /// <summary>
    /// Check that a named type has no non-optional recursive cycle.
    /// A cycle through optional fields (T?) is valid — none breaks the recursion.
    /// A cycle through non-optional fields is infinite size — error.
    /// </summary>
    private static void ValidateNoNonOptionalCycle(
        string startTypeName,
        Dictionary<string, NamedTypeDefinition> allTypes) {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CheckCycle(startTypeName, visited, allTypes);
    }

    private static void CheckCycle(
        string typeName,
        HashSet<string> visited,
        Dictionary<string, NamedTypeDefinition> allTypes) {
        if (!visited.Add(typeName))
            throw Errors.NamedTypeRecursiveCycle(typeName);
        if (!allTypes.TryGetValue(typeName, out var typeDef))
            return;
        if (typeDef.IsAlias || typeDef.Fields == null)
            return; // aliases don't have struct fields to check
        foreach (var field in typeDef.Fields) {
            // Extract the referenced type name from the field's type syntax.
            // Only non-optional references create mandatory cycles.
            var referencedType = GetNonOptionalTypeReference(field.TypeSyntax);
            if (referencedType != null)
                CheckCycle(referencedType, new HashSet<string>(visited, StringComparer.OrdinalIgnoreCase), allTypes);
        }
    }

    /// <summary>
    /// Returns the type name if the syntax refers to a named type directly (non-optional).
    /// Returns null for optional types (T?), arrays (T[]), primitives, or no type.
    /// </summary>
    private static string GetNonOptionalTypeReference(TypeSyntax syntax) {
        // Direct named reference: field:typename — non-optional, creates mandatory cycle
        if (syntax is TypeSyntax.Named named)
            return named.Name;
        // Optional (T?), Array (T[]) — break the cycle at value level
        return null;
    }

    /// <summary>
    /// Validate that a type alias does not form a circular chain.
    /// Follows alias references (a -> b -> c -> ...) and detects if we revisit a type already in the chain.
    /// </summary>
    private static void ValidateNoCircularAlias(
        string startName,
        Dictionary<string, NamedTypeDefinition> allTypes) {
        var chain = new List<string> { startName };
        var current = startName;
        while (true) {
            if (!allTypes.TryGetValue(current, out var def))
                return; // references a built-in or unknown type — not a cycle
            if (!def.IsAlias)
                return; // resolved to a struct type — not a cycle
            var referencedName = GetAliasTargetName(def.AliasTypeSyntax);
            if (referencedName == null)
                return; // alias resolves to a primitive/array/etc — not a cycle
            if (string.Equals(referencedName, startName, StringComparison.OrdinalIgnoreCase)) {
                chain.Add(referencedName);
                throw Errors.CircularTypeAlias(chain.ToArray());
            }
            // Check if we revisit any type in the chain (for longer cycles detected mid-chain)
            foreach (var visited in chain)
                if (string.Equals(referencedName, visited, StringComparison.OrdinalIgnoreCase)) {
                    chain.Add(referencedName);
                    throw Errors.CircularTypeAlias(chain.ToArray());
                }
            chain.Add(referencedName);
            current = referencedName;
        }
    }

    /// <summary>
    /// Extract a named type reference from a non-struct alias body.
    /// Walks through Optional and Array wrappers — those don't break recursion at the
    /// alias level (only struct-field recursion may break through Optional/Array per spec
    /// §"Recursive types" line 86; alias-level recursion requires a Function-arrow break).
    /// Stops at FunOf (function arrow is the legal contractive break) and StructOf
    /// (struct aliases follow the field-level recursion rules, not the alias-cycle rule).
    /// </summary>
    /// <summary>
    /// Reject named type declarations whose name (case-insensitively) collides with a primitive
    /// type identifier. Per spec §"Rules" line 120: type names are case-insensitive, so `Text`
    /// shadows `text`. Without this guard, the type is accepted at declaration but produces
    /// misleading errors at constructor call sites.
    /// </summary>
    private static bool IsPrimitiveTypeName(string name) => name.ToLowerInvariant() switch {
        "int16" or "int" or "int32" or "int64"
            or "byte" or "uint8" or "uint16" or "uint" or "uint32" or "uint64"
            or "real" or "bool" or "char" or "text" or "any" or "ip" => true,
        _ => false
    };

    private static string GetAliasTargetName(TypeSyntax syntax) {
        while (true) {
            switch (syntax) {
                case TypeSyntax.Named named:
                    return named.Name;
                case TypeSyntax.OptionalOf opt:
                    syntax = opt.Element;
                    continue;
                case TypeSyntax.ArrayOf arr:
                    syntax = arr.Element;
                    continue;
                // FunOf — function arrow is the legal contractive break for non-struct aliases
                // StructOf — struct aliases follow field-level recursion rules (Optional/Array breaks)
                default:
                    return null;
            }
        }
    }
}
