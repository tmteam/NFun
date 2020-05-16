using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using Array = System.Array;

namespace NFun.TypeInferenceAdapter
{
    public class TicSetupDfsVisitor: DfsVisitorBase
    {
        private readonly SetupTiState _state;
        private readonly IFunctionDicitionary _dictionary;
        private readonly TypeInferenceResultsBuilder _resultsBuilder;
        private ISyntaxNode ParentNode { get; set; }
        private int CurrentChildNumber { get; set; }

        public static bool Run(IEnumerable<ISyntaxNode> nodes, GraphBuilder builder, IFunctionDicitionary dictionary,
            TypeInferenceResultsBuilder resultsBuilder)
        { 
            var visitor = new TicSetupDfsVisitor(new SetupTiState(builder), dictionary, resultsBuilder);
            foreach (var syntaxNode in nodes)
            {
                if (!Dfs(visitor, syntaxNode))
                    return false;
            }

            return true;
        }

        private static bool Dfs(TicSetupDfsVisitor visitor, ISyntaxNode node)
        {
            var res = node.Accept(visitor as ISyntaxNodeVisitor<VisitorEnterResult>);
            if(res == VisitorEnterResult.Failed)
                return false;
            if(res == VisitorEnterResult.Skip)
                return true;
            int childNum = 0;
            foreach (var child in node.Children)
            {
                visitor.OnEnterNode(node, childNum);
                if (!Dfs(visitor, child))
                    return false;
                visitor.OnExitNode();
                childNum++;
            }
            return node.Accept(visitor as ISyntaxNodeVisitor<bool>);
        }

        public void OnEnterNode(ISyntaxNode parent, int childNum)
        {
            ParentNode = parent;
            CurrentChildNumber = childNum;
        }

        public void OnExitNode()
        {
        }
        internal TicSetupDfsVisitor(SetupTiState state, IFunctionDicitionary dictionary,
            TypeInferenceResultsBuilder resultsBuilder)
        {
            _state = state;
            _dictionary = dictionary;
            _resultsBuilder = resultsBuilder;
        }

        #region MetaInfoSyntaxNode
        protected override VisitorEnterResult EnterVisit(MetaInfoSyntaxNode node) => VisitorEnterResult.Skip;
        #endregion

        #region UserFunctionDefenitionSyntaxNode
        protected override VisitorEnterResult EnterVisit(UserFunctionDefenitionSyntaxNode node)
        {
            var argNames = new string[node.Args.Count];
            int i = 0;
            foreach (var arg in node.Args)
            {
                argNames[i] = arg.Id;
                i++;
                if (arg.VarType != VarType.Empty)
                    _state.CurrentSolver.SetVarType(arg.Id, arg.VarType.ConvertToTiType());
            }

            IType returnType = null;
            if (node.ReturnType != VarType.Empty)
                returnType = (IType)node.ReturnType.ConvertToTiType();

            TraceLog.WriteLine($"Enter {node.OrderNumber}. UFun {node.Id}({string.Join(",", argNames)})->{node.Body.OrderNumber}:{returnType?.ToString() ?? "empty"}");
            var fun = _state.CurrentSolver.SetFunDef(
                name: node.Id + "'" + node.Args.Count,
                returnId: node.Body.OrderNumber,
                returnType: returnType,
                varNames: argNames);
            _resultsBuilder.RememberUserFunctionSignature(node.Id, fun);
            return VisitorEnterResult.Continue;
        }
        #endregion

        #region ArraySyntaxNode
        protected override bool ExitVisit(ArraySyntaxNode node)
        {
            var elementIds = node.Expressions.Select(e => e.OrderNumber).ToArray();
            Trace(node, $"[{string.Join(",", elementIds)}]");
            _state.CurrentSolver.SetSoftArrayInit(
                node.OrderNumber,
                node.Expressions.Select(e => e.OrderNumber).ToArray()
            );
            return true;
        }
        #endregion

        #region SuperAnonymFunctionSyntaxNode
        protected override VisitorEnterResult EnterVisit(SuperAnonymFunctionSyntaxNode node) 
            => throw new NotImplementedException();

        protected override bool ExitVisit(SuperAnonymFunctionSyntaxNode node)
            => throw new NotImplementedException();
        #endregion

        #region ArrowAnonymFunctionSyntaxNode
        protected override VisitorEnterResult EnterVisit(ArrowAnonymFunctionSyntaxNode arrowAnonymFunNode)
        {
            _state.EnterScope(arrowAnonymFunNode.OrderNumber);
            foreach (var syntaxNode in arrowAnonymFunNode.ArgumentsDefenition)
            {
                string originName;
                string anonymName;
                if (syntaxNode is TypedVarDefSyntaxNode typed)
                {
                    originName = typed.Id;
                    anonymName = MakeAnonVariableName(arrowAnonymFunNode, originName);
                    if (!typed.VarType.Equals(VarType.Empty))
                    {
                        var ticType = typed.VarType.ConvertToTiType();
                        _state.CurrentSolver.SetVarType(anonymName, ticType);
                    }
                }
                else if (syntaxNode is VariableSyntaxNode varNode)
                {
                    originName = varNode.Id;
                    anonymName = MakeAnonVariableName(arrowAnonymFunNode, originName);
                }
                else
                    throw ErrorFactory.AnonymousFunArgumentIsIncorrect(syntaxNode);

                _state.AddVariableAliase(originName, anonymName);
            }

            return VisitorEnterResult.Continue;
        }

        protected override bool ExitVisit(ArrowAnonymFunctionSyntaxNode node)
        {
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
                _state.CurrentSolver.CreateLambda(node.Body.OrderNumber, node.OrderNumber, argNames);
            else
            {
                var retType = (IType)node.OutputType.ConvertToTiType();
                _state.CurrentSolver.CreateLambda(
                    node.Body.OrderNumber,
                    node.OrderNumber,
                    retType,
                    argNames);
            }

            _state.ExitScope();
            return true;
        }
        #endregion

        #region EquationSyntaxNode
        protected override bool ExitVisit(EquationSyntaxNode node)
        {
            Trace(node, $"{node.Id}:{node.OutputType} = {node.Expression.OrderNumber}");

            if (node.OutputTypeSpecified)
            {
                var type = node.OutputType.ConvertToTiType();
                _state.CurrentSolver.SetVarType(node.Id, type);
            }

            _state.CurrentSolver.SetDef(node.Id, node.Expression.OrderNumber);
            return true;
        }
        #endregion

        #region FunCallSyntaxNode
        protected override VisitorEnterResult EnterVisit(FunCallSyntaxNode node)
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

            if (signature != null)
                _resultsBuilder.RememberFunctionCall(node.OrderNumber, signature);

            return VisitorEnterResult.Continue;
        }
        
        protected override bool ExitVisit(FunCallSyntaxNode node)
        {
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
                _state.CurrentSolver.SetCall(userFunction, ids);
                //in the case of generic user function  - we dont know generic arg types yet 
                //we need to remember generic TIC signature to used it at the end of interpritation
                _resultsBuilder.RememberRecursiveCall(node.OrderNumber, userFunction);
                return true;
            }


            var signature = _resultsBuilder.GetSignatureOrNull(node.OrderNumber);
            if (signature == null)
            {
                //Functional variable
                Trace(node, $"Call hi order {node.Id}({string.Join(",", ids)})");
                _state.CurrentSolver.SetCall(node.Id, ids);
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
            {
                types[i] = signature.ArgTypes[i].ConvertToTiType(genericTypes);
            }

            types[types.Length - 1] = signature.ReturnType.ConvertToTiType(genericTypes);

            _state.CurrentSolver.SetCall(types, ids);
            return true;
        }
        #endregion

        #region ResultFunCallSyntaxNode
        protected override bool ExitVisit(ResultFunCallSyntaxNode node)
        {
            var ids = new int[node.Args.Length + 1];
            for (int i = 0; i < node.Args.Length; i++)
                ids[i] = node.Args[i].OrderNumber;
            ids[ids.Length - 1] = node.OrderNumber;

            _state.CurrentSolver.SetCall(node.ResultExpression.OrderNumber, ids);
            return true;
        }
        #endregion

        #region IfThenElseSyntaxNode
        protected override bool ExitVisit(IfThenElseSyntaxNode node)
        {
            var conditions = node.Ifs.Select(i => i.Condition.OrderNumber).ToArray();
            var expressions = node.Ifs.Select(i => i.Expression.OrderNumber).Append(node.ElseExpr.OrderNumber)
                .ToArray();
            Trace(node, $"if({string.Join(",", conditions)}): {string.Join(",", expressions)}");
            _state.CurrentSolver.SetIfElse(
                conditions,
                expressions,
                node.OrderNumber);
            return true;
        }
        #endregion

        #region ConstantSyntaxNode

        protected override bool ExitVisit(ConstantSyntaxNode node)
        {
            Trace(node, $"Constant {node.Value}:{node.ClrTypeName}");
            var type = LangTiHelper.ConvertToTiType(node.OutputType);

            if (type is Primitive p)
                _state.CurrentSolver.SetConst(node.OrderNumber, p);
            else if (type is Tic.SolvingStates.Array a && a.Element is Primitive primitiveElement)
                _state.CurrentSolver.SetArrayConst(node.OrderNumber, primitiveElement);
            else
                throw new InvalidOperationException("Complex constant type is not supported");
            return true;
        }
        #endregion

        #region GenericIntSyntaxNode
        protected override bool ExitVisit(GenericIntSyntaxNode node)
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
                            _state.CurrentSolver.SetIntConst(node.OrderNumber, Primitive.I16, Primitive.I64,
                                Primitive.I32);
                        else if (l >= Int32.MinValue)
                            _state.CurrentSolver.SetIntConst(node.OrderNumber, Primitive.I32, Primitive.I64,
                                Primitive.I32);
                        else _state.CurrentSolver.SetConst(node.OrderNumber, Primitive.I64);
                        return true;
                    }
                }
                else if (node.Value is ulong u)
                    actualValue = u;
                else
                    throw new ImpossibleException("Generic token has to be ulong or long");

                //positive constant
                if (actualValue <= byte.MaxValue)
                    _state.CurrentSolver.SetIntConst(node.OrderNumber, Primitive.U8, Primitive.I96, Primitive.I32);
                else if (actualValue <= (ulong)Int16.MaxValue)
                    _state.CurrentSolver.SetIntConst(node.OrderNumber, Primitive.U12, Primitive.I96, Primitive.I32);
                else if (actualValue <= (ulong)UInt16.MaxValue)
                    _state.CurrentSolver.SetIntConst(node.OrderNumber, Primitive.U16, Primitive.I96, Primitive.I32);
                else if (actualValue <= (ulong)Int32.MaxValue)
                    _state.CurrentSolver.SetIntConst(node.OrderNumber, Primitive.U24, Primitive.I96, Primitive.I32);
                else if (actualValue <= (ulong)UInt32.MaxValue)
                    _state.CurrentSolver.SetIntConst(node.OrderNumber, Primitive.U32, Primitive.I96, Primitive.I64);
                else if (actualValue <= (ulong)Int64.MaxValue)
                    _state.CurrentSolver.SetIntConst(node.OrderNumber, Primitive.U48, Primitive.I96, Primitive.I64);
                else
                    _state.CurrentSolver.SetConst(node.OrderNumber, Primitive.U64);
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
                        _state.CurrentSolver.SetIntConst(node.OrderNumber, descedant);
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
                _state.CurrentSolver.SetIntConst(node.OrderNumber, descedant);

            }

            return true;
        }
        #endregion

        #region TypedVarDefSyntaxNode
        protected override bool ExitVisit(TypedVarDefSyntaxNode node)
        {
            Trace(node, $"Tvar {node.Id}:{node.VarType}  ");
            if (node.VarType != VarType.Empty)
            {
                var type = LangTiHelper.ConvertToTiType(node.VarType);
                _state.CurrentSolver.SetVarType(node.Id, type);
            }

            return true;
        }
        #endregion

        #region VarDefenitionSyntaxNode
        protected override bool ExitVisit(VarDefenitionSyntaxNode node)
        {
            Trace(node, $"VarDef {node.Id}:{node.VarType}  ");
            var type = LangTiHelper.ConvertToTiType(node.VarType);
            _state.CurrentSolver.SetVarType(node.Id, type);
            return true;
        }
        #endregion
       
        #region VariableSyntaxNode
        protected override bool ExitVisit(VariableSyntaxNode node)
        {
            Trace(node, $"VAR {node.Id} ");

            //nfun syntax allows multiple variables to have the same name depending on whether they are functions or not
            //need to know what type of argument is expected - is it variableId, or functionId?
            //if it is function - how many arguments are expected ? 
            var argType = VarType.Empty;
            if (ParentNode is FunCallSyntaxNode parentCall)
            {
                var parentSignature = _resultsBuilder.GetSignatureOrNull(parentCall.OrderNumber);
                if (parentSignature != null)
                    argType = parentSignature.ArgTypes[CurrentChildNumber];
            }

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

                        _state.CurrentSolver.SetVarType($"g'{argsCount}'{node.Id}",
                            genericFunction.GetTicFunType(generics));
                        _state.CurrentSolver.SetVar($"g'{argsCount}'{node.Id}", node.OrderNumber);

                    }
                    else
                    {
                        _state.CurrentSolver.SetVarType($"f'{argsCount}'{node.Id}", signature.GetTicFunType());
                        _state.CurrentSolver.SetVar($"f'{argsCount}'{node.Id}", node.OrderNumber);
                    }

                    _resultsBuilder.RememberFunctionalVariable(node.OrderNumber, signature);
                    return true;
                }
            }

            // usual argument is expected
            var localId = _state.GetActualName(node.Id);
            _state.CurrentSolver.SetVar(localId, node.OrderNumber);
            return true;
        }
        #endregion

        #region  privates
        private RefTo[] InitializeGenericTypes(GenericConstrains[] constrains)
        {
            var genericTypes = new RefTo[constrains.Length];
            for (int i = 0; i < constrains.Length; i++)
            {
                var def = constrains[i];
                genericTypes[i] = _state.CurrentSolver.InitializeVarNode(
                    def.Descendant,
                    def.Ancestor,
                    def.IsComparable);
            }

            return genericTypes;
        }

        private void Trace(ISyntaxNode node, string text)
        {
            if (TraceLog.IsEnabled)
                TraceLog.WriteLine($"Exit:{node.OrderNumber}. {text} ");
        }
        private static string MakeAnonVariableName(ISyntaxNode node, string id)
            => LangTiHelper.GetArgAlias("anonymous_" + node.OrderNumber, id);
        #endregion

      
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

        public bool HasAlias(string inputAlias)
            => _aliasTable.HasVariable(inputAlias);
    }
}
