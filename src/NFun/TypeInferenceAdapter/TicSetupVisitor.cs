using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly SetupTiState _state;
        private readonly GraphBuilder _ticTypeGraph;
        private readonly IFunctionDicitionary _dictionary;
        private readonly TypeInferenceResultsBuilder _resultsBuilder;

        public static bool Run(
            IEnumerable<ISyntaxNode> nodes, 
            GraphBuilder builder, 
            IFunctionDicitionary dictionary,
            TypeInferenceResultsBuilder resultsBuilder)
        {
            var visitor = new TicSetupVisitor(new SetupTiState(builder), dictionary, resultsBuilder);
            foreach (var syntaxNode in nodes)
            {
                if (!syntaxNode.Accept(visitor))
                    return false;
            }
            return true;
        }
        internal TicSetupVisitor(SetupTiState state, IFunctionDicitionary dictionary,
            TypeInferenceResultsBuilder resultsBuilder)
        {
            _state = state;
            _dictionary = dictionary;
            _resultsBuilder = resultsBuilder;
            _ticTypeGraph = state.CurrentSolver;
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

            IType returnType = null;
            if (node.ReturnType != VarType.Empty)
                returnType = (IType) node.ReturnType.ConvertToTiType();

            TraceLog.WriteLine(
                $"Enter {node.OrderNumber}. UFun {node.Id}({string.Join(",", argNames)})->{node.Body.OrderNumber}:{returnType?.ToString() ?? "empty"}");
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

            var elementIds = node.Expressions.Select(e => e.OrderNumber).ToArray();
            Trace(node, $"[{string.Join(",", elementIds)}]");
            _ticTypeGraph.SetSoftArrayInit(
                node.OrderNumber,
                node.Expressions.Select(e => e.OrderNumber).ToArray()
            );
            return true;
        }
        public bool Visit(SuperAnonymFunctionSyntaxNode node)  => throw new NotImplementedException();

        public bool Visit(ArrowAnonymFunctionSyntaxNode node)
        {
            _state.EnterScope(node.OrderNumber);
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
                else if (syntaxNode is VariableSyntaxNode varNode)
                {
                    originName = varNode.Id;
                    anonymName = MakeAnonVariableName(node, originName);
                }
                else
                    throw ErrorFactory.AnonymousFunArgumentIsIncorrect(syntaxNode);

                _state.AddVariableAliase(originName, anonymName);
            }

            VisitChildren(node);
            
            var argNames = new string[node.ArgumentsDefenition.Length];
            for (var i = 0; i < node.ArgumentsDefenition.Length; i++)
            {
                var syntaxNode = node.ArgumentsDefenition[i];
                if (syntaxNode is TypedVarDefSyntaxNode typed)
                    argNames[i] = _state.GetActualName(typed.Id);
                else if (syntaxNode is VariableSyntaxNode varNode)
                    argNames[i] = _state.GetActualName(varNode.Id);
            }

            Trace(node, $"f({string.Join(" ", argNames)}):{node.OutputType}= {{{node.OrderNumber}}}");

            if (node.OutputType == VarType.Empty)
                _ticTypeGraph.CreateLambda(node.Body.OrderNumber, node.OrderNumber, argNames);
            else
            {
                var retType = (IType)node.OutputType.ConvertToTiType();
                _ticTypeGraph.CreateLambda(
                    node.Body.OrderNumber,
                    node.OrderNumber,
                    retType,
                    argNames);
            }

            _state.ExitScope();
            return true;
        }

        public bool Visit(EquationSyntaxNode node)
        {
            VisitChildren(node);

            Trace(node, $"{node.Id}:{node.OutputType} = {node.Expression.OrderNumber}");

            if (node.OutputTypeSpecified)
            {
                var type = node.OutputType.ConvertToTiType();
                _ticTypeGraph.SetVarType(node.Id, type);
            }

            _ticTypeGraph.SetDef(node.Id, node.Expression.OrderNumber);
            return true;
        }

        private VarType _parentFunctionVarType = VarType.Empty;
        public bool Visit(FunCallSyntaxNode node)
        {
            var signature = _dictionary.GetOrNull(node.Id, node.Args.Length);
            if (signature is GenericMetafunction)
            {
                //If it is Metafunction - need to transform origin node to metafunction
                var firstArg = node.Args[0] as VariableSyntaxNode;
                if (firstArg == null)
                    throw FunParseException.ErrorStubToDo("first arg should be variable");
                node.TransformToMetafunction(firstArg);
            }

            for (int i = 0; i < node.Args.Length; i++)
            {
                if (signature != null)
                    _parentFunctionVarType = signature.ArgTypes[i];
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
                Trace(node, $"Call UF{node.Id}({string.Join(",", ids)})");
                _ticTypeGraph.SetCall(userFunction, ids);
                //in the case of generic user function  - we dont know generic arg types yet 
                //we need to remember generic TIC signature to used it at the end of interpritation
                _resultsBuilder.RememberRecursiveCall(node.OrderNumber, userFunction);
                return true;
            }


            if (signature == null)
            {
                //Functional variable
                Trace(node, $"Call hi order {node.Id}({string.Join(",", ids)})");
                _ticTypeGraph.SetCall(node.Id, ids);
                return true;
            }
            //Normal function call
            Trace(node, $"Call {node.Id}({string.Join(",", ids)})");

            RefTo[] genericTypes;
            if (signature is GenericFunctionBase t)
            {
                //Optimization
                //Remember generic arguments to use it again at the built time
                genericTypes = InitializeGenericTypes(t.GenericDefenitions);
                _resultsBuilder.RememberGenericCallArguments(node.OrderNumber, genericTypes);
            }
            else genericTypes = new RefTo[0];

            var types = new IState[signature.ArgTypes.Length + 1];
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

            var conditions = node.Ifs.Select(i => i.Condition.OrderNumber).ToArray();
            var expressions = node.Ifs.Select(i => i.Expression.OrderNumber).Append(node.ElseExpr.OrderNumber)
                .ToArray();
            Trace(node, $"if({string.Join(",", conditions)}): {string.Join(",", expressions)}");
            _ticTypeGraph.SetIfElse(
                conditions,
                expressions,
                node.OrderNumber);
            return true;
        }


        public bool Visit(ConstantSyntaxNode node)
        {
            Trace(node, $"Constant {node.Value}:{node.ClrTypeName}");
            var type = LangTiHelper.ConvertToTiType(node.OutputType);

            if (type is Primitive p)
                _ticTypeGraph.SetConst(node.OrderNumber, p);
            else if (type is Tic.SolvingStates.Array a && a.Element is Primitive primitiveElement)
                _ticTypeGraph.SetArrayConst(node.OrderNumber, primitiveElement);
            else
                throw new InvalidOperationException("Complex constant type is not supported");
            return true;
        }

        public bool Visit(GenericIntSyntaxNode node)
        {
            Trace(node, $"IntConst {node.Value}:{(node.IsHexOrBin ? "hex" : "int")}");

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
                            _ticTypeGraph.SetIntConst(node.OrderNumber, Primitive.I16, Primitive.I64,
                                Primitive.I32);
                        else if (l >= Int32.MinValue)
                            _ticTypeGraph.SetIntConst(node.OrderNumber, Primitive.I32, Primitive.I64,
                                Primitive.I32);
                        else _ticTypeGraph.SetConst(node.OrderNumber, Primitive.I64);
                        return true;
                    }
                }
                else if (node.Value is ulong u)
                    actualValue = u;
                else
                    throw new ImpossibleException("Generic token has to be ulong or long");

                //positive constant
                if (actualValue <= byte.MaxValue)
                    _ticTypeGraph.SetIntConst(node.OrderNumber, Primitive.U8, Primitive.I96, Primitive.I32);
                else if (actualValue <= (ulong)Int16.MaxValue)
                    _ticTypeGraph.SetIntConst(node.OrderNumber, Primitive.U12, Primitive.I96, Primitive.I32);
                else if (actualValue <= (ulong)UInt16.MaxValue)
                    _ticTypeGraph.SetIntConst(node.OrderNumber, Primitive.U16, Primitive.I96, Primitive.I32);
                else if (actualValue <= (ulong)Int32.MaxValue)
                    _ticTypeGraph.SetIntConst(node.OrderNumber, Primitive.U24, Primitive.I96, Primitive.I32);
                else if (actualValue <= (ulong)UInt32.MaxValue)
                    _ticTypeGraph.SetIntConst(node.OrderNumber, Primitive.U32, Primitive.I96, Primitive.I64);
                else if (actualValue <= (ulong)Int64.MaxValue)
                    _ticTypeGraph.SetIntConst(node.OrderNumber, Primitive.U48, Primitive.I96, Primitive.I64);
                else
                    _ticTypeGraph.SetConst(node.OrderNumber, Primitive.U64);
            }
            else
            {
                //1,2,3
                //Can be u8:<c:<real
                Primitive descedant;
                ulong actualValue;
                if (node.Value is long l)
                {
                    if (l > 0) actualValue = (ulong)l;
                    else
                    {
                        //negative constant
                        if (l >= Int16.MinValue) descedant = Primitive.I16;
                        else if (l >= Int32.MinValue) descedant = Primitive.I32;
                        else descedant = Primitive.I64;
                        _ticTypeGraph.SetIntConst(node.OrderNumber, descedant);
                        return true;
                    }
                }
                else if (node.Value is ulong u)
                    actualValue = u;
                else
                    throw new ImpossibleException("Generic token has to be ulong or long");

                //positive constant
                if (actualValue <= byte.MaxValue) descedant = Primitive.U8;
                else if (actualValue <= (ulong)Int16.MaxValue) descedant = Primitive.U12;
                else if (actualValue <= (ulong)UInt16.MaxValue) descedant = Primitive.U16;
                else if (actualValue <= (ulong)Int32.MaxValue) descedant = Primitive.U24;
                else if (actualValue <= (ulong)UInt32.MaxValue) descedant = Primitive.U32;
                else if (actualValue <= (ulong)Int64.MaxValue) descedant = Primitive.U48;
                else descedant = Primitive.U64;
                _ticTypeGraph.SetIntConst(node.OrderNumber, descedant);

            }

            return true;
        }

        public bool Visit(SyntaxTree node)
        {
            throw new NotImplementedException();
        }

        public bool Visit(TypedVarDefSyntaxNode node)
        {
            VisitChildren(node);

            Trace(node, $"Tvar {node.Id}:{node.VarType}  ");
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

            Trace(node, $"VarDef {node.Id}:{node.VarType}  ");
            var type = node.VarType.ConvertToTiType();
            _ticTypeGraph.SetVarType(node.Id, type);
            return true;
        }

        public bool Visit(VariableSyntaxNode node)
        {
            Trace(node, $"VAR {node.Id} ");

            //nfun syntax allows multiple variables to have the same name depending on whether they are functions or not
            //need to know what type of argument is expected - is it variableId, or functionId?
            //if it is function - how many arguments are expected ? 
            var argType = _parentFunctionVarType;
            if (argType.BaseType == BaseVarType.Fun)// functional argument is expected
            {
                var argsCount = argType.FunTypeSpecification.Inputs.Length;
                var signature = _dictionary.GetOrNull(node.Id, argsCount);
                if (signature != null)
                {
                    if (signature is GenericFunctionBase genericFunction)
                    {
                        var generics = InitializeGenericTypes(genericFunction.GenericDefenitions);
                        _resultsBuilder.RememberGenericCallArguments(node.OrderNumber, generics);

                        _ticTypeGraph.SetVarType($"g'{argsCount}'{node.Id}",
                            genericFunction.GetTicFunType(generics));
                        _ticTypeGraph.SetVar($"g'{argsCount}'{node.Id}", node.OrderNumber);

                    }
                    else
                    {
                        _ticTypeGraph.SetVarType($"f'{argsCount}'{node.Id}", signature.GetTicFunType());
                        _ticTypeGraph.SetVar($"f'{argsCount}'{node.Id}", node.OrderNumber);
                    }

                    _resultsBuilder.RememberFunctionalVariable(node.OrderNumber, signature);
                    return true;
                }
            }

            // usual argument is expected
            var localId = _state.GetActualName(node.Id);
            _ticTypeGraph.SetVar(localId, node.OrderNumber);
            return true;
        }
        public bool Visit(MetaInfoSyntaxNode node) => true;
        public bool Visit(IfCaseSyntaxNode node) => VisitChildren(node);
        public bool Visit(ListOfExpressionsSyntaxNode node) => VisitChildren(node);

        #region privates
        private RefTo[] InitializeGenericTypes(GenericConstrains[] constrains)
        {
            var genericTypes = new RefTo[constrains.Length];
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
        #endregion

        private void Trace(ISyntaxNode node, string text)
        {
            if (TraceLog.IsEnabled)
                TraceLog.WriteLine($"Exit:{node.OrderNumber}. {text} ");
        }
        private static string MakeAnonVariableName(ISyntaxNode node, string id)
            => LangTiHelper.GetArgAlias("anonymous_" + node.OrderNumber, id);
        private bool VisitChildren(ISyntaxNode node) 
            => node.Children.All(child => child.Accept(this));
    }


    class SetupTiState
    {
        private readonly AliasTable _aliasTable;

        public SetupTiState(GraphBuilder globalSolver)
        {
            CurrentSolver = globalSolver;
            _aliasTable = new AliasTable();
        }

        public GraphBuilder CurrentSolver { get; }

        public string GetActualName(string varName)
            => _aliasTable.GetVariableAlias(varName);

        public void EnterScope(int nodeId)
            => _aliasTable.InitVariableScope(nodeId, new List<string>());

        public void ExitScope()
            => _aliasTable.ExitVariableScope();

        public bool AddVariableAliase(string originName, string alias)
            => _aliasTable.AddVariableAlias(originName, alias);
    }
}