using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using NFun.Exceptions;
using NFun.Interpritation;
using NFun.Interpritation.Functions;
using NFun.ParseErrors;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
using NFun.Tic;
using NFun.Tic.SolvingStates;
using NFun.TypeInferenceCalculator;
using NFun.Types;

namespace NFun.TypeInferenceAdapter
{
    public class TicSetupVisitor : ISyntaxNodeVisitor<bool>
    {
        private readonly VariableScopeAliasTable _aliasScope;
        private readonly GraphBuilder _ticTypeGraph;
        private readonly IFunctionDictionary _dictionary;
        private readonly IConstantList _constants;
        private readonly TypeInferenceResultsBuilder _resultsBuilder;

        public static bool Run(
            IEnumerable<ISyntaxNode> nodes, 
            GraphBuilder ticGraph, 
            IFunctionDictionary functions,
            IConstantList constants,
            TypeInferenceResultsBuilder results)
        {
            var visitor = new TicSetupVisitor(ticGraph, functions, constants, results);
            foreach (var syntaxNode in nodes)
            {
                if (!syntaxNode.Accept(visitor))
                    return false;
            }
            return true;
        }

        private TicSetupVisitor(
            GraphBuilder ticTypeGraph,  
            IFunctionDictionary dictionary,
            IConstantList constants,
            TypeInferenceResultsBuilder resultsBuilder)
        {
            _aliasScope = new VariableScopeAliasTable();
            _dictionary = dictionary;
            _constants = constants;
            _resultsBuilder = resultsBuilder;
            _ticTypeGraph = ticTypeGraph;
        }

        public bool Visit(SyntaxTree node) => VisitChildren(node);
        public bool Visit(EquationSyntaxNode node)
        {
            VisitChildren(node);
#if DEBUG
            Trace(node, $"{node.Id}:{node.OutputType} = {node.Expression.OrderNumber}");
#endif
            if (node.OutputTypeSpecified)
            {
                var type = node.OutputType.ConvertToTiType();
                _ticTypeGraph.SetVarType(node.Id, type);
            }

            _ticTypeGraph.SetDef(node.Id, node.Expression.OrderNumber);
            return true;
        }
        public bool Visit(UserFunctionDefenitionSyntaxNode node)
        {
            var argNames = new string[node.Args.Count];
            int i = 0;
            foreach (var arg in node.Args)
            {
                argNames[i] = arg.Id;
                i++;
                if (arg.VarType != VarType.Empty)
                    _ticTypeGraph.SetVarType(arg.Id, arg.VarType.ConvertToTiType());
            }

            ITypeState returnType = null;
            if (node.ReturnType != VarType.Empty)
                returnType = (ITypeState) node.ReturnType.ConvertToTiType();

            #if DEBUG
            TraceLog.WriteLine(
                $"Enter {node.OrderNumber}. UFun {node.Id}({string.Join(",", argNames)})->{node.Body.OrderNumber}:{returnType?.ToString() ?? "empty"}");
            #endif
            var fun = _ticTypeGraph.SetFunDef(
                name: node.Id + "'" + node.Args.Count,
                returnId: node.Body.OrderNumber,
                returnType: returnType,
                varNames: argNames);
            _resultsBuilder.RememberUserFunctionSignature(node.Id, fun);
            
            return VisitChildren(node);
            
        }
        public bool Visit(ArraySyntaxNode node)
        {
            VisitChildren(node);

#if DEBUG
            var elementIds = node.Expressions.SelectToArray(e => e.OrderNumber);
            Trace(node, $"[{string.Join(",", elementIds)}]");
#endif
            _ticTypeGraph.SetSoftArrayInit(
                node.OrderNumber,
                node.Expressions.Select(e => e.OrderNumber)
            );
            return true;
        }
        public bool Visit(SuperAnonymFunctionSyntaxNode node)
        {
            _aliasScope.EnterScope(node.OrderNumber);

            var argType = _parentFunctionArgType.FunTypeSpecification;
            string[] originArgNames = null;
            string[] aliasArgNames = null;

            if (argType == null || argType.Inputs.Length==1)
                originArgNames = new[] {"it"};
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
        public bool Visit(ArrowAnonymFunctionSyntaxNode node)
        {
            _aliasScope.EnterScope(node.OrderNumber);
            foreach (var syntaxNode in node.ArgumentsDefenition)
            {
                string originName;
                string anonymName;
                if (syntaxNode is TypedVarDefSyntaxNode typed)
                {
                    originName = typed.Id;
                    anonymName = MakeAnonVariableName(node, originName);
                    if (!typed.VarType.Equals(VarType.Empty))
                    {
                        var ticType = typed.VarType.ConvertToTiType();
                        _ticTypeGraph.SetVarType(anonymName, ticType);
                    }
                }
                else if (syntaxNode is NamedIdSyntaxNode varNode)
                {
                    originName = varNode.Id;
                    anonymName = MakeAnonVariableName(node, originName);
                }
                else
                    throw ErrorFactory.AnonymousFunArgumentIsIncorrect(syntaxNode);

                _aliasScope.AddVariableAlias(originName, anonymName);
            }

            VisitChildren(node);
            
            var aliasNames = new string[node.ArgumentsDefenition.Length];
            for (var i = 0; i < node.ArgumentsDefenition.Length; i++)
            {
                var syntaxNode = node.ArgumentsDefenition[i];
                if (syntaxNode is TypedVarDefSyntaxNode typed)
                    aliasNames[i] = _aliasScope.GetVariableAlias(typed.Id);
                else if (syntaxNode is NamedIdSyntaxNode varNode)
                    aliasNames[i] = _aliasScope.GetVariableAlias(varNode.Id);
            }

#if DEBUG
            Trace(node, $"f({string.Join(" ", aliasNames)}):{node.OutputType}= {{{node.OrderNumber}}}");
#endif
            if (node.OutputType == VarType.Empty)
                _ticTypeGraph.CreateLambda(node.Body.OrderNumber, node.OrderNumber, aliasNames);
            else
            {
                var retType = (ITypeState)node.OutputType.ConvertToTiType();
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
        private VarType _parentFunctionArgType = VarType.Empty;
        public bool Visit(FunCallSyntaxNode node)
        {
            var signature = _dictionary.GetOrNull(node.Id, node.Args.Length);
           
            for (int i = 0; i < node.Args.Length; i++)
            {
                if (signature != null)
                    _parentFunctionArgType = signature.ArgTypes[i];
                node.Args[i].Accept(this);
            }

            var ids = new int[node.Args.Length + 1];
            for (int i = 0; i < node.Args.Length; i++)
                ids[i] = node.Args[i].OrderNumber;
            ids[ids.Length - 1] = node.OrderNumber;

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
            StateRefTo[] genericTypes;
            if (signature is GenericFunctionBase t)
            {
                //Optimization
                //Remember generic arguments to use it again at the built time
                genericTypes = InitializeGenericTypes(t.GenericDefenitions);
                _resultsBuilder.RememberGenericCallArguments(node.OrderNumber, genericTypes);
            }
            else genericTypes = new StateRefTo[0];

            var types = new ITicNodeState[signature.ArgTypes.Length + 1];
            for (int i = 0; i < signature.ArgTypes.Length; i++)
                types[i] = signature.ArgTypes[i].ConvertToTiType(genericTypes);
            types[types.Length - 1] = signature.ReturnType.ConvertToTiType(genericTypes);

            _ticTypeGraph.SetCall(types, ids);
            return true;
        }
        public bool Visit(ResultFunCallSyntaxNode node)
        {
            VisitChildren(node);

            var ids = new int[node.Args.Length + 1];
            for (int i = 0; i < node.Args.Length; i++)
                ids[i] = node.Args[i].OrderNumber;
            ids[ids.Length - 1] = node.OrderNumber;

            _ticTypeGraph.SetCall(node.ResultExpression.OrderNumber, ids);
            return true;
        }
        public bool Visit(IfThenElseSyntaxNode node)
        {
            VisitChildren(node);

            var conditions = node.Ifs.SelectToArray(i => i.Condition.OrderNumber);
            var expressions = node.Ifs.SelectToArrayAndAppendTail(
                tail:    node.ElseExpr.OrderNumber,
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
        public bool Visit(ConstantSyntaxNode node)
        {
#if DEBUG
            Trace(node, $"Constant {node.Value}:{node.ClrTypeName}");
#endif
            var type = LangTiHelper.ConvertToTiType(node.OutputType);

            if (type is StatePrimitive p)
                _ticTypeGraph.SetConst(node.OrderNumber, p);
            else if (type is Tic.SolvingStates.StateArray a && a.Element is StatePrimitive primitiveElement)
                _ticTypeGraph.SetArrayConst(node.OrderNumber, primitiveElement);
            else
                throw new InvalidOperationException("Complex constant type is not supported");
            return true;
        }
        public bool Visit(GenericIntSyntaxNode node)
        {
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
                            _ticTypeGraph.SetIntConst(node.OrderNumber, StatePrimitive.I16, StatePrimitive.I64,
                                StatePrimitive.I32);
                        else if (l >= Int32.MinValue)
                            _ticTypeGraph.SetIntConst(node.OrderNumber, StatePrimitive.I32, StatePrimitive.I64,
                                StatePrimitive.I32);
                        else _ticTypeGraph.SetConst(node.OrderNumber, StatePrimitive.I64);
                        return true;
                    }
                }
                else if (node.Value is ulong u)
                    actualValue = u;
                else
                    throw new ImpossibleException("Generic token has to be ulong or long");

                //positive constant
                if (actualValue <= byte.MaxValue)
                    _ticTypeGraph.SetIntConst(node.OrderNumber, StatePrimitive.U8, StatePrimitive.I96, StatePrimitive.I32);
                else if (actualValue <= (ulong)Int16.MaxValue)
                    _ticTypeGraph.SetIntConst(node.OrderNumber, StatePrimitive.U12, StatePrimitive.I96, StatePrimitive.I32);
                else if (actualValue <= (ulong)UInt16.MaxValue)
                    _ticTypeGraph.SetIntConst(node.OrderNumber, StatePrimitive.U16, StatePrimitive.I96, StatePrimitive.I32);
                else if (actualValue <= (ulong)Int32.MaxValue)
                    _ticTypeGraph.SetIntConst(node.OrderNumber, StatePrimitive.U24, StatePrimitive.I96, StatePrimitive.I32);
                else if (actualValue <= (ulong)UInt32.MaxValue)
                    _ticTypeGraph.SetIntConst(node.OrderNumber, StatePrimitive.U32, StatePrimitive.I96, StatePrimitive.I64);
                else if (actualValue <= (ulong)Int64.MaxValue)
                    _ticTypeGraph.SetIntConst(node.OrderNumber, StatePrimitive.U48, StatePrimitive.I96, StatePrimitive.I64);
                else
                    _ticTypeGraph.SetConst(node.OrderNumber, StatePrimitive.U64);
            }
            else
            {
                //1,2,3
                //Can be u8:<c:<real
                StatePrimitive descedant;
                ulong actualValue;
                if (node.Value is long l)
                {
                    if (l > 0) actualValue = (ulong)l;
                    else
                    {
                        //negative constant
                        if (l >= Int16.MinValue) descedant = StatePrimitive.I16;
                        else if (l >= Int32.MinValue) descedant = StatePrimitive.I32;
                        else descedant = StatePrimitive.I64;
                        _ticTypeGraph.SetIntConst(node.OrderNumber, descedant);
                        return true;
                    }
                }
                else if (node.Value is ulong u)
                    actualValue = u;
                else
                    throw new ImpossibleException("Generic token has to be ulong or long");

                //positive constant
                if (actualValue <= byte.MaxValue) descedant = StatePrimitive.U8;
                else if (actualValue <= (ulong)Int16.MaxValue) descedant = StatePrimitive.U12;
                else if (actualValue <= (ulong)UInt16.MaxValue) descedant = StatePrimitive.U16;
                else if (actualValue <= (ulong)Int32.MaxValue) descedant = StatePrimitive.U24;
                else if (actualValue <= (ulong)UInt32.MaxValue) descedant = StatePrimitive.U32;
                else if (actualValue <= (ulong)Int64.MaxValue) descedant = StatePrimitive.U48;
                else descedant = StatePrimitive.U64;
                _ticTypeGraph.SetIntConst(node.OrderNumber, descedant);

            }

            return true;
        }
        public bool Visit(NamedIdSyntaxNode node)
        {
            var id = node.Id;
#if DEBUG
            Trace(node, $"VAR {id} ");
#endif
            //nfun syntax allows multiple variables to have the same name depending on whether they are functions or not
            //need to know what type of argument is expected - is it variableId, or functionId?
            //if it is function - how many arguments are expected ? 
            var argType = _parentFunctionArgType;
            if (argType.BaseType == BaseVarType.Fun) // functional argument is expected
            {
                var argsCount = argType.FunTypeSpecification.Inputs.Length;
                var signature = _dictionary.GetOrNull(id, argsCount);
                if (signature != null)
                {
                    if (signature is GenericFunctionBase genericFunction)
                    {
                        var generics = InitializeGenericTypes(genericFunction.GenericDefenitions);
                        _resultsBuilder.RememberGenericCallArguments(node.OrderNumber, generics);

                        _ticTypeGraph.SetVarType($"g'{argsCount}'{id}",
                            genericFunction.GetTicFunType(generics));
                        _ticTypeGraph.SetVar($"g'{argsCount}'{id}", node.OrderNumber);

                        node.IdType = NamedIdNodeType.GenericFunction;
                        node.IdContent = new FunctionalVariableCallInfo(signature, generics);
                    }
                    else
                    {
                        _ticTypeGraph.SetVarType($"f'{argsCount}'{id}", signature.GetTicFunType());
                        _ticTypeGraph.SetVar($"f'{argsCount}'{id}", node.OrderNumber);

                        node.IdType = NamedIdNodeType.ConcreteFunction;
                        node.IdContent = new FunctionalVariableCallInfo(signature, null);
                    }

                    _resultsBuilder.RememberFunctionalVariable(node.OrderNumber, signature);
                    return true;
                }
            }
            // At this point we are sure - ID is not a function

            // ID can be constant or variable
            // if ID exists in ticTypeGraph - then ID is Variable
            // else if ID exists in constant list - then ID is constant
            // else ID is variable

            if (!_ticTypeGraph.HasNamedNode(id) && _constants.TryGetConstant(id, out var constant))
            {
                //ID is constant 
                node.IdType = NamedIdNodeType.Constant;
                node.IdContent = constant;

                var titype = constant.Type.ConvertToTiType();
                if(titype is StatePrimitive primitive)
                    _ticTypeGraph.SetConst(node.OrderNumber, primitive);
                else if (titype is StateArray array && array.Element is StatePrimitive primitiveElement)
                    _ticTypeGraph.SetArrayConst(node.OrderNumber, primitiveElement);
                else
                    throw new InvalidOperationException("Type " + constant.Type + " is not supported for constants");
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
        public bool Visit(TypedVarDefSyntaxNode node)
        {
            VisitChildren(node);

#if DEBUG
            Trace(node, $"Tvar {node.Id}:{node.VarType}  ");
#endif
            if (node.VarType != VarType.Empty)
            {
                var type = node.VarType.ConvertToTiType();
                _ticTypeGraph.SetVarType(node.Id, type);
            }

            return true;
        }
        public bool Visit(VarDefenitionSyntaxNode node)
        {
            VisitChildren(node);

#if DEBUG
            Trace(node, $"VarDef {node.Id}:{node.VarType}  ");
#endif
            var type = node.VarType.ConvertToTiType();
            _ticTypeGraph.SetVarType(node.Id, type);
            return true;
        }
        public bool Visit(ListOfExpressionsSyntaxNode node) => VisitChildren(node);

        #region privates
        private StateRefTo[] InitializeGenericTypes(GenericConstrains[] constrains)
        {
            var genericTypes = new StateRefTo[constrains.Length];
            for (int i = 0; i < constrains.Length; i++)
            {
                var def = constrains[i];
                genericTypes[i] = _ticTypeGraph.InitializeVarNode(
                    def.Descendant,
                    def.Ancestor,
                    def.IsComparable);
            }

            return genericTypes;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Trace(ISyntaxNode node, string text)
        {
#if DEBUG
            if (TraceLog.IsEnabled)
                TraceLog.WriteLine($"Exit:{node.OrderNumber}. {text} ");
#endif
        }
        private static string MakeAnonVariableName(ISyntaxNode node, string id)
            => LangTiHelper.GetArgAlias("anonymous_" + node.OrderNumber, id);
        private bool VisitChildren(ISyntaxNode node) 
            => node.Children.All(child => child.Accept(this));
        #endregion

    }
}