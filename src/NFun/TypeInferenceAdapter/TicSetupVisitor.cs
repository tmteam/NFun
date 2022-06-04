using System;
using System.Linq;
using System.Runtime.CompilerServices;
using NFun.Exceptions;
using NFun.Interpretation;
using NFun.Interpretation.Functions;
using NFun.ParseErrors;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
using NFun.Tic;
using NFun.Tic.SolvingStates;
using NFun.Types;

namespace NFun.TypeInferenceAdapter; 

public class TicSetupVisitor : ISyntaxNodeVisitor<bool> {
    private readonly VariableScopeAliasTable _aliasScope;
    private readonly GraphBuilder _ticTypeGraph;
    private readonly IFunctionDictionary _dictionary;
    private readonly IConstantList _constants;
    private readonly TypeInferenceResultsBuilder _resultsBuilder;
    private readonly DialectSettings _dialect;

    public static bool SetupTicForBody(
        SyntaxTree tree,
        GraphBuilder ticGraph,
        TypeInferenceResultsBuilder results, TypeBehaviour typeBehaviour) => SetupTicForBody(
        tree, ticGraph,
        BaseFunctions.GetFunctions(typeBehaviour),
        EmptyConstantList.Instance,
        EmptyAprioriTypesMap.Instance, 
        results, Dialects.Origin);

    internal static bool SetupTicForBody(
        SyntaxTree tree,
        GraphBuilder ticGraph,
        IFunctionDictionary functions,
        IConstantList constants,
        IAprioriTypesMap aprioriTypes,
        TypeInferenceResultsBuilder results,
        DialectSettings dialect) {
        var visitor = new TicSetupVisitor(ticGraph, functions, constants, results, aprioriTypes, dialect);

        foreach (var syntaxNode in tree.Children)
        {
            if (syntaxNode is UserFunctionDefinitionSyntaxNode)
                continue;

            if (!syntaxNode.Accept(visitor))
                return false;
        }

        return true;
    }

    internal static bool SetupTicForUserFunction(
        UserFunctionDefinitionSyntaxNode userFunctionNode,
        GraphBuilder ticGraph,
        IFunctionDictionary functions,
        IConstantList constants,
        TypeInferenceResultsBuilder results,
        DialectSettings dialect) {
        var visitor = new TicSetupVisitor(ticGraph, functions, constants, results, EmptyAprioriTypesMap.Instance, dialect);
        return userFunctionNode.Accept(visitor);
    }

    private TicSetupVisitor(
        GraphBuilder ticTypeGraph,
        IFunctionDictionary dictionary,
        IConstantList constants,
        TypeInferenceResultsBuilder resultsBuilder,
        IAprioriTypesMap aprioriTypesMap,
        DialectSettings dialect) {
        _aliasScope = new VariableScopeAliasTable();
        _dictionary = dictionary;
        _constants = constants;
        _resultsBuilder = resultsBuilder;
        _dialect = dialect;
        _ticTypeGraph = ticTypeGraph;

        foreach (var apriori in aprioriTypesMap)
            _ticTypeGraph.SetVarType(apriori.Key, apriori.Value.ConvertToTiType());
    }

    public bool Visit(SyntaxTree node) => VisitChildren(node);

    public bool Visit(EquationSyntaxNode node) {
        VisitChildren(node);
#if DEBUG
        Trace(node, $"{node.Id}:{node.OutputType} = {node.Expression.OrderNumber}");
#endif
        if (node.OutputTypeSpecified)
        {
            var type = node.OutputType.ConvertToTiType();
            if (!_ticTypeGraph.TrySetVarType(node.Id, type))
                throw Errors.VariableIsAlreadyDeclared(node.Id, node.Interval);
        }

        _ticTypeGraph.SetDef(node.Id, node.Expression.OrderNumber);
        return true;
    }

    public bool Visit(UserFunctionDefinitionSyntaxNode node) {
        var argNames = new string[node.Args.Count];
        int i = 0;
        foreach (var arg in node.Args)
        {
            argNames[i] = arg.Id;
            i++;
            if (arg.FunnyType != FunnyType.Empty)
                _ticTypeGraph.SetVarType(arg.Id, arg.FunnyType.ConvertToTiType());
        }

        ITypeState returnType = null;
        if (node.ReturnType != FunnyType.Empty)
            returnType = (ITypeState)node.ReturnType.ConvertToTiType();

#if DEBUG
        TraceLog.WriteLine(
            $"Enter {node.OrderNumber}. UFun {node.Id}({string.Join(",", argNames)})->{node.Body.OrderNumber}:{returnType?.ToString() ?? "empty"}");
#endif
        var fun = _ticTypeGraph.SetFunDef(
            name: $"{node.Id}'{node.Args.Count}",
            returnId: node.Body.OrderNumber,
            returnType: returnType,
            varNames: argNames);
        _resultsBuilder.RememberUserFunctionSignature(node.Id, fun);

        return VisitChildren(node);
    }

    public bool Visit(ArraySyntaxNode node) {
        VisitChildren(node);
        var elementIds = new int[node.Expressions.Count];
        for (int i = 0; i < node.Expressions.Count; i++)
            elementIds[i] = node.Expressions[i].OrderNumber;
#if DEBUG
        Trace(node, $"[{string.Join(",", elementIds)}]");
#endif
        _ticTypeGraph.SetSoftArrayInit(node.OrderNumber, elementIds);
        return true;
    }

    public bool Visit(SuperAnonymFunctionSyntaxNode node) {
        _aliasScope.EnterScope(node.OrderNumber);

        var argType = _parentFunctionArgType.FunTypeSpecification;
        string[] originArgNames;
        string[] aliasArgNames;

        if (argType == null || argType.Inputs.Length == 1)
            originArgNames = new[] { "it" };
        else
        {
            originArgNames = new string[argType.Inputs.Length];
            for (int i = 0; i < argType.Inputs.Length; i++)
                originArgNames[i] = $"it{i + 1}";
        }

        aliasArgNames = new string[originArgNames.Length];

        for (var i = 0; i < originArgNames.Length; i++)
        {
            var originName = originArgNames[i];
            var aliasName = MakeAnonVariableName(node, originName);
            _aliasScope.AddVariableAlias(originName, aliasName);
            aliasArgNames[i] = aliasName;
        }

        VisitChildren(node);
#if DEBUG
        Trace(node, $"f({string.Join(" ", originArgNames)}):{node.OutputType}= {{{node.OrderNumber}}}");
#endif
        _ticTypeGraph.CreateLambda(node.Body.OrderNumber, node.OrderNumber, aliasArgNames);

        _aliasScope.ExitScope();
        return true;
    }

    public bool Visit(StructFieldAccessSyntaxNode node) {
        if (!node.Source.Accept(this))
            return false;
        _ticTypeGraph.SetFieldAccess(node.Source.OrderNumber, node.OrderNumber, node.FieldName.ToLower());
        return true;
    }

    public bool Visit(StructInitSyntaxNode node) {
        if (!VisitChildren(node))
            return false;
        _ticTypeGraph.SetStructInit(
            node.Fields.Select(f => f.Name.ToLower()).ToArray(),
            node.Fields.Select(f => f.Node.OrderNumber).ToArray(), node.OrderNumber);
        return true;
    }
    
    public bool Visit(DefaultValueSyntaxNode node) {
        _ticTypeGraph.SetGenericConst(node.OrderNumber);
        return true;
    }

    public bool Visit(AnonymFunctionSyntaxNode node) {
        _aliasScope.EnterScope(node.OrderNumber);
        foreach (var syntaxNode in node.ArgumentsDefinition)
        {
            //setup arguments.
            string originName;
            string anonymName;
            switch (syntaxNode)
            {
                case TypedVarDefSyntaxNode typed:
                {
                    //argument has type definition on it
                    originName = typed.Id;
                    anonymName = MakeAnonVariableName(node, originName);
                    if (!typed.FunnyType.Equals(FunnyType.Empty))
                    {
                        var ticType = typed.FunnyType.ConvertToTiType();
                        _ticTypeGraph.SetVarType(anonymName, ticType);
                    }

                    break;
                }
                case NamedIdSyntaxNode varNode:
                    //argument has no type definition on it
                    originName = varNode.Id;
                    anonymName = MakeAnonVariableName(node, originName);
                    break;
                default:
                    throw Errors.AnonymousFunArgumentIsIncorrect(syntaxNode);
            }

            _aliasScope.AddVariableAlias(originName, anonymName);
        }

        VisitChildren(node);

        var aliasNames = new string[node.ArgumentsDefinition.Length];
        for (var i = 0; i < node.ArgumentsDefinition.Length; i++)
        {
            var syntaxNode = node.ArgumentsDefinition[i];
            if (syntaxNode is TypedVarDefSyntaxNode typed)
                aliasNames[i] = _aliasScope.GetVariableAlias(typed.Id);
            else if (syntaxNode is NamedIdSyntaxNode varNode)
                aliasNames[i] = _aliasScope.GetVariableAlias(varNode.Id);
        }

#if DEBUG
        Trace(node, $"f({string.Join(" ", aliasNames)}):{node.OutputType}= {{{node.OrderNumber}}}");
#endif
        if (node.ReturnType == FunnyType.Empty)
            _ticTypeGraph.CreateLambda(node.Body.OrderNumber, node.OrderNumber, aliasNames);
        else
        {
            var retType = (ITypeState)node.ReturnType.ConvertToTiType();
            _ticTypeGraph.CreateLambda(
                node.Body.OrderNumber,
                node.OrderNumber,
                retType,
                aliasNames);
        }

        _aliasScope.ExitScope();
        return true;
    }

    /// <summary>
    /// If we handle function call -
    /// it shows type of argument that currently handling
    /// if it is known
    /// </summary>
    private FunnyType _parentFunctionArgType = FunnyType.Empty;

    public bool Visit(FunCallSyntaxNode node) {
        var signature = _dictionary.GetOrNull(node.Id, node.Args.Length);
        node.FunctionSignature = signature;
        //Apply visitor to child types
        for (int i = 0; i < node.Args.Length; i++)
        {
            if (signature != null)
                _parentFunctionArgType = signature.ArgTypes[i];
            node.Args[i].Accept(this);
        }

        //Setup ids arrays
        var ids = new int[node.Args.Length + 1];
        for (int i = 0; i < node.Args.Length; i++)
            ids[i] = node.Args[i].OrderNumber;
        ids[^1] = node.OrderNumber;

        var userFunction = _resultsBuilder.GetUserFunctionSignature(node.Id, node.Args.Length);
        if (userFunction != null)
        {
            //Call user-function if it is being built at the same time as the current expression is being built
            //for example: recursive calls, or if function relates to global variables
#if DEBUG
            Trace(node, $"Call UF{node.Id}({string.Join(",", ids)})");
#endif
            _ticTypeGraph.SetCall(userFunction, ids);
            //in the case of generic user function  - we dont know generic arg types yet 
            //we need to remember generic TIC signature to used it at the end of interpritation
            _resultsBuilder.RememberRecursiveCall(node.OrderNumber, userFunction);
            return true;
        }

        if (signature == null)
        {
            //Functional variable
#if DEBUG
            Trace(node, $"Call hi order {node.Id}({string.Join(",", ids)})");
#endif
            _ticTypeGraph.SetCall(node.Id, ids);
            return true;
        }
        //Normal function call
#if DEBUG
        Trace(node, $"Call {node.Id}({string.Join(",", ids)})");
#endif
        if (signature is PureGenericFunctionBase pure)
        {
            // Сase of (T,T):T signatures
            // This case is most common, so the call is optimized
            var genericType = InitializeGenericType(pure.Constrains[0]);
            _resultsBuilder.RememberGenericCallArguments(node.OrderNumber, new[] { genericType });
            _ticTypeGraph.SetCall(genericType, ids);
            return true;
        }

        StateRefTo[] genericTypes;
        if (signature is GenericFunctionBase t)
        {
            // Optimization
            // Remember generic arguments to use it again at the built time
            genericTypes = InitializeGenericTypes(t.Constrains);
            // save refernces to generic types, for use at 'apply tic results' step 
            _resultsBuilder.RememberGenericCallArguments(node.OrderNumber, genericTypes);
        }
        else genericTypes = Array.Empty<StateRefTo>();

        var types = new ITicNodeState[signature.ArgTypes.Length + 1];
        for (int i = 0; i < signature.ArgTypes.Length; i++)
            types[i] = signature.ArgTypes[i].ConvertToTiType(genericTypes);
        types[^1] = signature.ReturnType.ConvertToTiType(genericTypes);

        _ticTypeGraph.SetCall(types, ids);
        return true;
    }

    public bool Visit(ResultFunCallSyntaxNode node) {
        VisitChildren(node);

        var ids = new int[node.Args.Length + 1];
        for (int i = 0; i < node.Args.Length; i++)
            ids[i] = node.Args[i].OrderNumber;
        ids[^1] = node.OrderNumber;

        _ticTypeGraph.SetCall(node.ResultExpression.OrderNumber, ids);
        return true;
    }

    public bool Visit(IfThenElseSyntaxNode node) {
        VisitChildren(node);

        var conditions = node.Ifs.SelectToArray(i => i.Condition.OrderNumber);
        var expressions = node.Ifs.SelectToArrayAndAppendTail(
            tail: node.ElseExpr.OrderNumber,
            mapFunc: i => i.Expression.OrderNumber);

#if DEBUG
        Trace(node, $"if({string.Join(",", conditions)}): {string.Join(",", expressions)}");
#endif

        _ticTypeGraph.SetIfElse(
            conditions,
            expressions,
            node.OrderNumber);
        return true;
    }

    public bool Visit(IfCaseSyntaxNode node) => VisitChildren(node);

    public bool Visit(ConstantSyntaxNode node) {
#if DEBUG
        Trace(node, $"Constant {node.Value}:{node.ClrTypeName}");
#endif
        var type = node.OutputType.ConvertToTiType();

        if (type is StatePrimitive p)
            _ticTypeGraph.SetConst(node.OrderNumber, p);
        else if (type is StateArray a && a.Element is StatePrimitive primitiveElement)
            _ticTypeGraph.SetArrayConst(node.OrderNumber, primitiveElement);
        else
            throw new InvalidOperationException("Complex constant type is not supported");
        return true;
    }

    public bool Visit(GenericIntSyntaxNode node) {
#if DEBUG
        Trace(node, $"IntConst {node.Value}:{(node.IsHexOrBin ? "hex" : "int")}");
#endif
        if (node.IsHexOrBin)
        {
            //hex or bin constant
            //can be u8:< c:< i96
            ulong actualValue;
            if (node.Value is long l)
            {
                if (l > 0) actualValue = (ulong)l;
                else
                {
                    //negative constant
                    if (l >= Int16.MinValue)
                        _ticTypeGraph.SetGenericConst(
                            node.OrderNumber, StatePrimitive.I16, StatePrimitive.I64,
                            StatePrimitive.I32);
                    else if (l >= Int32.MinValue)
                        _ticTypeGraph.SetGenericConst(
                            node.OrderNumber, StatePrimitive.I32, StatePrimitive.I64,
                            StatePrimitive.I32);
                    else _ticTypeGraph.SetConst(node.OrderNumber, StatePrimitive.I64);
                    return true;
                }
            }
            else if (node.Value is ulong u)
                actualValue = u;
            else
                throw new NFunImpossibleException("Generic token has to be ulong or long");

            //positive constant
            if (actualValue <= byte.MaxValue)
                _ticTypeGraph.SetGenericConst(
                    node.OrderNumber, StatePrimitive.U8, StatePrimitive.I96,
                    StatePrimitive.I32);
            else if (actualValue <= (ulong)Int16.MaxValue)
                _ticTypeGraph.SetGenericConst(
                    node.OrderNumber, StatePrimitive.U12, StatePrimitive.I96,
                    StatePrimitive.I32);
            else if (actualValue <= (ulong)UInt16.MaxValue)
                _ticTypeGraph.SetGenericConst(
                    node.OrderNumber, StatePrimitive.U16, StatePrimitive.I96,
                    StatePrimitive.I32);
            else if (actualValue <= (ulong)Int32.MaxValue)
                _ticTypeGraph.SetGenericConst(
                    node.OrderNumber, StatePrimitive.U24, StatePrimitive.I96,
                    StatePrimitive.I32);
            else if (actualValue <= (ulong)UInt32.MaxValue)
                _ticTypeGraph.SetGenericConst(
                    node.OrderNumber, StatePrimitive.U32, StatePrimitive.I96,
                    StatePrimitive.I64);
            else if (actualValue <= (ulong)Int64.MaxValue)
                _ticTypeGraph.SetGenericConst(
                    node.OrderNumber, StatePrimitive.U48, StatePrimitive.I96,
                    StatePrimitive.I64);
            else
                _ticTypeGraph.SetConst(node.OrderNumber, StatePrimitive.U64);
        }
        else
        {
            //1,2,3
            //Can be u8:<c:<real
            StatePrimitive descendant;
            ulong actualValue;
            if (node.Value is long l)
            {
                if (l > 0) actualValue = (ulong)l;
                else
                {
                    descendant = l switch {
                                     //negative constant
                                     >= Int16.MinValue => StatePrimitive.I16,
                                     >= Int32.MinValue => StatePrimitive.I32,
                                     _                 => StatePrimitive.I64
                                 };
                    var preferred = GetPreferredIntConstantType();
                    _ticTypeGraph.SetGenericConst(node.OrderNumber, descendant, StatePrimitive.Real, preferred);
                    return true;
                }
            }
            else if (node.Value is ulong u)
                actualValue = u;
            else
                throw new NFunImpossibleException("Generic token has to be ulong or long");

            //positive constant
            if (actualValue <= byte.MaxValue) descendant = StatePrimitive.U8;
            else if (actualValue <= (ulong)Int16.MaxValue) descendant = StatePrimitive.U12;
            else if (actualValue <= (ulong)UInt16.MaxValue) descendant = StatePrimitive.U16;
            else if (actualValue <= (ulong)Int32.MaxValue) descendant = StatePrimitive.U24;
            else if (actualValue <= (ulong)UInt32.MaxValue) descendant = StatePrimitive.U32;
            else if (actualValue <= (ulong)Int64.MaxValue) descendant = StatePrimitive.U48;
            else descendant = StatePrimitive.U64;
            _ticTypeGraph.SetGenericConst(
                node.OrderNumber, descendant, StatePrimitive.Real,
                GetPreferredIntConstantType());
        }

        return true;
    }

    private StatePrimitive GetPreferredIntConstantType() =>
        _dialect.IntegerPreferredType switch {
            IntegerPreferredType.I32  => StatePrimitive.I32,
            IntegerPreferredType.I64  => StatePrimitive.I64,
            IntegerPreferredType.Real => StatePrimitive.Real,
            _                         => null
        };

    public bool Visit(NamedIdSyntaxNode node) {
        var id = node.Id;
#if DEBUG
        Trace(node, $"VAR {id} ");
#endif
        //nfun syntax allows multiple variables to have the same name depending on whether they are functions or not
        //need to know what type of argument is expected - is it variableId, or functionId?
        //if it is function - how many arguments are expected ? 
        var argType = _parentFunctionArgType;
        if (argType.BaseType == BaseFunnyType.Fun) // functional argument is expected
        {
            var argsCount = argType.FunTypeSpecification.Inputs.Length;
            var signature = _dictionary.GetOrNull(id, argsCount);
            if (signature != null)
            {
                if (signature is GenericFunctionBase genericFunction)
                {
                    var generics = InitializeGenericTypes(genericFunction.Constrains);
                    _resultsBuilder.RememberGenericCallArguments(node.OrderNumber, generics);

                    _ticTypeGraph.SetVarType(
                        $"g'{argsCount}'{id}",
                        genericFunction.GetTicFunType(generics));
                    _ticTypeGraph.SetVar($"g'{argsCount}'{id}", node.OrderNumber);

                    node.IdType = NamedIdNodeType.GenericFunction;
                }
                else
                {
                    _ticTypeGraph.SetVarType($"f'{argsCount}'{id}", signature.GetTicFunType());
                    _ticTypeGraph.SetVar($"f'{argsCount}'{id}", node.OrderNumber);

                    node.IdType = NamedIdNodeType.ConcreteFunction;
                }

                _resultsBuilder.RememberFunctionalVariable(node.OrderNumber, signature);
                return true;
            }
        }
        // At this point we are sure - ID is not a function

        // ID can be constant or apriori variable or usual (not apriori) variable 
        // if ID exists in ticTypeGraph - then ID is Variable
        // else if ID exists in constant list - then ID is constant
        // else ID is variable

        if (!_ticTypeGraph.HasNamedNode(id) && _constants.TryGetConstant(id, out var constant))
        {
            //ID is constant 
            node.IdType = NamedIdNodeType.Constant;
            node.IdContent = constant;

            var tiType = constant.Type.ConvertToTiType();
            switch (tiType)
            {
                case StatePrimitive primitive:
                    _ticTypeGraph.SetConst(node.OrderNumber, primitive);
                    break;
                case StateArray { Element: StatePrimitive primitiveElement }:
                    _ticTypeGraph.SetArrayConst(node.OrderNumber, primitiveElement);
                    break;
                case StateStruct @struct:
                {
                    if (@struct.IsSolved) 
                        _ticTypeGraph.SetStructConst(node.OrderNumber, @struct);
                    break;
                }
                default:
                    throw new InvalidOperationException(
                        "Type " +
                        constant.Type +
                        " is not supported for constants");
            }
        }
        else
        {
            //ID is variable
            var localId = _aliasScope.GetVariableAlias(node.Id);
            _ticTypeGraph.SetVar(localId, node.OrderNumber);

            node.IdType = NamedIdNodeType.Variable;
        }

        return true;
    }

    public bool Visit(TypedVarDefSyntaxNode node) {
        VisitChildren(node);
#if DEBUG
        Trace(node, $"Tvar {node.Id}:{node.FunnyType}  ");
#endif
        if (node.FunnyType != FunnyType.Empty)
        {
            var type = node.FunnyType.ConvertToTiType();
            if (!_ticTypeGraph.TrySetVarType(node.Id, type))
                throw Errors.VariableIsAlreadyDeclared(node.Id, node.Interval);
        }

        return true;
    }

    public bool Visit(VarDefinitionSyntaxNode node) {
        VisitChildren(node);

#if DEBUG
        Trace(node, $"VarDef {node.Id}:{node.FunnyType}  ");
#endif
        var type = node.FunnyType.ConvertToTiType();
        if (!_ticTypeGraph.TrySetVarType(node.Id, type))
            throw Errors.VariableIsAlreadyDeclared(node.Id, node.Interval);
        return true;
    }

    public bool Visit(ListOfExpressionsSyntaxNode node) => VisitChildren(node);


    #region privates

    private StateRefTo[] InitializeGenericTypes(GenericConstrains[] constrains) {
        var genericTypes = new StateRefTo[constrains.Length];
        for (int i = 0; i < constrains.Length; i++)
            genericTypes[i] = InitializeGenericType(constrains[i]);

        return genericTypes;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private StateRefTo InitializeGenericType(GenericConstrains constrains) =>
        _ticTypeGraph.InitializeVarNode(
            constrains.Descendant,
            constrains.Ancestor,
            constrains.IsComparable);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Trace(ISyntaxNode node, string text) {
#if DEBUG
        if (TraceLog.IsEnabled)
            TraceLog.WriteLine($"Exit:{node.OrderNumber}. {text} ");
#endif
    }

    private static string MakeAnonVariableName(ISyntaxNode node, string id)
        => LangTiHelper.GetArgAlias("anonymous_" + node.OrderNumber, id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool VisitChildren(ISyntaxNode node) {
        foreach (var child in node.Children)
        {
            if (!child.Accept(this))
                return false;
        }

        return true;
    }

    #endregion
}