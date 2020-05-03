using System;
using System.Linq;
using NFun.BuiltInFunctions;
using NFun.Interpritation;
using NFun.Interpritation.Functions;
using NFun.ParseErrors;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
using NFun.Tic;
using NFun.Tic.SolvingStates;
using NFun.Types;
using Array = NFun.Tic.SolvingStates.Array;

namespace NFun.TypeInferenceAdapter
{
    public sealed class SetupTiExitVisitor: ExitVisitorBase
    {
        private readonly SetupTiState _state;
        private readonly IFunctionDicitionary _dictionary;
        private readonly TypeInferenceResultsBuilder _resultsBuilder;
        public SetupTiExitVisitor(SetupTiState state, IFunctionDicitionary dictionary, TypeInferenceResultsBuilder resultsBuilder)
        {
            _state = state;
            _dictionary = dictionary;
            _resultsBuilder = resultsBuilder;
        }

        public override bool Visit(ArraySyntaxNode node)
        {
            var elementIds = node.Expressions.Select(e => e.OrderNumber).ToArray();
            Trace(node, $"[{string.Join(",", elementIds)}]");
            _state.CurrentSolver.SetArrayInit(
                node.OrderNumber, 
                node.Expressions.Select(e => e.OrderNumber).ToArray()
                );
            return true;
            //var res =  _state.CurrentSolver.SetArrayInit(node.OrderNumber,
            //    node.Expressions.Select(e => e.OrderNumber).ToArray());
            //if (res.IsSuccesfully)
            //    return true;
            //if (res.FailedNodeId == node.OrderNumber)
            //    throw ErrorFactory.TypesNotSolved(node);
            //var failedItem = node.Children.First(c => c.OrderNumber == res.FailedNodeId);
            //throw ErrorFactory.VariousArrayElementTypes(failedItem);
        }

        /// <summary>
        /// User fuctions are not supported by the visitor
        /// </summary>
        public override bool Visit(UserFunctionDefenitionSyntaxNode node) => false;

        public override bool Visit(ProcArrayInit node)
        {
            throw new NotImplementedException();
            //if (node.Step == null)
            //    return _state.CurrentSolver.SetProcArrayInit(node.OrderNumber, node.From.OrderNumber, node.To.OrderNumber);
            //else
            //    return _state.CurrentSolver.SetProcArrayInit(node.OrderNumber, node.From.OrderNumber, node.To.OrderNumber,node.Step.OrderNumber);
        }

        public override bool Visit(AnonymCallSyntaxNode node)
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

            Trace(node,$"f({string.Join(" ",argNames)}):{node.OutputType}= {{{node.OrderNumber}}}");

            if (node.OutputType == VarType.Empty)
                _state.CurrentSolver.CreateLambda(node.Body.OrderNumber,node.OrderNumber, argNames);
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

        public override bool Visit(EquationSyntaxNode node)
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

        public override bool Visit(FunCallSyntaxNode node)
        {
            Trace(node, $"Call {node.Id}({string.Join(",", node.Args.Select(a=>a.OrderNumber))})");

            var signature = _resultsBuilder.GetSignatureOrNull(node.OrderNumber);

            RefTo[] genericTypes;
            if (signature is GenericFunctionBase t)
            {
                genericTypes = InitializeGenericTypes(t.GenericDefenitions);
                _resultsBuilder.SetGenericTypes(node.OrderNumber, genericTypes);
            }
            else genericTypes = new RefTo[0];
            
            var types = new IState[signature.ArgTypes.Length + 1];
            var ids = new int[signature.ArgTypes.Length + 1];
            for (int i = 0; i < signature.ArgTypes.Length; i++)
            {
                types[i] = signature.ArgTypes[i].ConvertToTiType(genericTypes);
                ids[i] = node.Args[i].OrderNumber;
            }
            types[types.Length - 1] = signature.ReturnType.ConvertToTiType(genericTypes);
            ids[types.Length - 1]   = node.OrderNumber;

            _state.CurrentSolver.SetCall(types, ids);
            return true;
        }

        public override bool Visit(IfThenElseSyntaxNode node)
        {
            var conditions  = node.Ifs.Select(i => i.Condition.OrderNumber).ToArray();
            var expressions = node.Ifs.Select(i => i.Expression.OrderNumber).Append(node.ElseExpr.OrderNumber).ToArray();
            Trace(node,$"if({string.Join(",",conditions)}): {string.Join(",",expressions)}");
            _state.CurrentSolver.SetIfElse(
                conditions,
                expressions,
                node.OrderNumber);
            return true;
        }

        public override bool Visit(ConstantSyntaxNode node)
        {
            Trace(node, $"Constant {node.Value}"+ (node.StrictType?"!":""));
            if (node.StrictType)
            {
                var type = LangTiHelper.ConvertToTiType(node.OutputType);
                if (type is Primitive p)
                    _state.CurrentSolver.SetConst(node.OrderNumber, p);
                else if (type is Array a && a.Element is Primitive primitiveElement)
                    _state.CurrentSolver.SetArrayConst(node.OrderNumber, primitiveElement);
                else
                    throw new InvalidOperationException("Complex constant type is not supported");
                
                return true;
            }

            object val = node.Value;

            if (val is int i32) val = (long) i32;
            else if (val is uint u32) val = (long) u32;

            if (val is ulong )
            {
                _state.CurrentSolver.SetIntConst(node.OrderNumber, Primitive.U64);
            }
            else if (val is long value)
            {
                if (value > 0)
                {
                    if (value < 256)
                        _state.CurrentSolver.SetIntConst(node.OrderNumber, Primitive.U8);
                    else if (value <= short.MaxValue)
                        _state.CurrentSolver.SetIntConst(node.OrderNumber, Primitive.U12);
                    else if (value <= ushort.MaxValue)
                        _state.CurrentSolver.SetIntConst(node.OrderNumber, Primitive.U16);
                    else if (value <= int.MaxValue)
                        _state.CurrentSolver.SetIntConst(node.OrderNumber, Primitive.U24);
                    else if (value <= uint.MaxValue)
                        _state.CurrentSolver.SetIntConst(node.OrderNumber, Primitive.U32);
                    else
                        _state.CurrentSolver.SetIntConst(node.OrderNumber, Primitive.U48);
                }
                else
                {
                    if (value > short.MinValue)
                        _state.CurrentSolver.SetIntConst(node.OrderNumber, Primitive.I16);
                    else if (value > int.MinValue)
                        _state.CurrentSolver.SetIntConst(node.OrderNumber, Primitive.I32);
                    else
                        _state.CurrentSolver.SetIntConst(node.OrderNumber, Primitive.I64);
                }
            }

            return true;
        }

        public override  bool Visit(TypedVarDefSyntaxNode node)
        {
            Trace(node, $"Tvar {node.Id}:{node.VarType}  ");

            var type = LangTiHelper.ConvertToTiType(node.VarType);
            _state.CurrentSolver.SetVarType(node.Id, type);
            return true;
        }

        public override  bool Visit(VarDefenitionSyntaxNode node)
        {
            Trace(node, $"VarDef {node.Id}:{node.VarType}  ");
            var type = LangTiHelper.ConvertToTiType(node.VarType);
            _state.CurrentSolver.SetVarType(node.Id, type);
            return true;
        }

        public override bool Visit(VariableSyntaxNode node)
        {
            Trace(node,$"VAR {node.Id} ");
            //Нужно узнать у Tic - что именно ожидается - переменная или функция
            //Если функция - то сколько в ней аргументов
            VarType argType = VarType.Empty;
            if (Parent is FunCallSyntaxNode parentCall)
            {
                var parentSignature = _resultsBuilder.GetSignatureOrNull(parentCall.OrderNumber);
                if(parentSignature!=null)
                    argType = parentSignature.ArgTypes[CurrentChildNumber];
            }
            
            if (argType.BaseType== BaseVarType.Fun)
            {
                //В качестве аргумента ожидается функция
                var argsCount = argType.FunTypeSpecification.Inputs.Length;
                var signature = _dictionary.GetOrNull(node.Id, argsCount);
                if (signature == null)
                    throw ErrorFactory.FunctionOverloadNotFound(node, _dictionary);
                if (signature is GenericFunctionBase generic)
                {
                    var generics =InitializeGenericTypes(generic.GenericDefenitions);
                    _resultsBuilder.SetGenericTypes(node.OrderNumber, generics);

                    _state.CurrentSolver.SetVarType($"g'{argsCount}'{node.Id}", generic.GetTicFunType(generics));
                    _state.CurrentSolver.SetVar($"g'{argsCount}'{node.Id}", node.OrderNumber);
                    
                }
                else
                {
                    _state.CurrentSolver.SetVarType($"f'{argsCount}'{node.Id}", signature.GetTicFunType());
                    _state.CurrentSolver.SetVar($"f'{argsCount}'{node.Id}",node.OrderNumber);
                }

                _resultsBuilder.SetFunctionalVariable(node.OrderNumber, signature);
                return true;
            }
            //ищем обычную переменную
            var localId = _state.GetActualName(node.Id);
            _state.CurrentSolver.SetVar(localId, node.OrderNumber);
            return true;
        }

        private void Trace(ISyntaxNode node, string text) =>
            Console.WriteLine($"Exit:{node.OrderNumber}. {text} ");
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
    }

}