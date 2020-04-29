using System;
using System.Linq;
using NFun.BuiltInFunctions;
using NFun.Interpritation;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
using NFun.Tic;
using NFun.Tic.SolvingStates;
using NFun.TypeInference;
using NFun.Types;

namespace NFun.TypeInferenceAdapter
{
    public sealed class SetupTiExitVisitor: ExitVisitorBase
    {
        private readonly SetupTiState _state;
        private readonly FunctionsDictionary _dictionary;
        public SetupTiExitVisitor(SetupTiState state, FunctionsDictionary dictionary)
        {
            _state = state;
            _dictionary = dictionary;
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
            if (node.OutputType == VarType.Empty)
                _state.CurrentSolver.CreateLambda(
                    node.Body.OrderNumber,
                    node.OrderNumber,
                    node.ArgumentsDefenition.Select(a => a.ToString()).ToArray());
            else
            {
                var retType = (IType)node.OutputType.ConvertToTiType();
                _state.CurrentSolver.CreateLambda(
                    node.Body.OrderNumber,
                    node.OrderNumber,
                    retType,
                    node.ArgumentsDefenition.Select(a => a.ToString()).ToArray());
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
            Trace(node, $"Call {node.Id}({string.Join(",", node.Args.Select(a=>a.OrderNumber))}");

            if (node.IsOperator)
            {
                //todo - operator is usual functions
                var res = HandleOperatorFunction(node);
                if (res)
                    return true;
                throw new InvalidOperationException("Operator "+ node.Id+" is not supported");

            }
            throw new InvalidOperationException("Only operator call supported");

            //
            //node.SignatureOfOverload
            //if (node.IsOperator && HandleOperatorFunction(node, out var result))
            //{
            //    if (!result.IsSuccesfully)
            //        ThrowInvalidOperatorCall(node, result);
            //    return true;
            //}
            //var argsCount = node.Args.Length;

            ////check for recursion call
            //var funAlias = LangTiHelper.GetFunAlias(node.Id, argsCount) ;

            //var funType = _state.CurrentSolver.GetOrNull(funAlias);
            //if (funType != null && funType.Name.Id == TiTypeName.FunId 
            //                    && funType.Arguments.Length-1 == node.Args.Length)
            //{
            //    //Recursive function call. We don't know its signature yet. That's why we set "functional variable",
            //    //instead of usual function call
            //    var res =  _state.CurrentSolver.SetInvoke(node.OrderNumber, funAlias,
            //        node.Args.Select(a => a.OrderNumber).ToArray());
            //    return res;
            //} 

            //var candidates = _dictionary.GetNonGeneric(node.Id).Where(n=>n.ArgTypes.Length == argsCount).ToList();

            //if (candidates.Count == 0)
            //{
            //    var genericCandidate = _dictionary.GetGenericOrNull(node.Id, argsCount);
            //    if (genericCandidate == null)
            //        throw ErrorFactory.FunctionOverloadNotFound(node, _dictionary);

            //    var callDef = ToCallDef(node, genericCandidate);
            //    if (!_state.CurrentSolver.SetCall(callDef))
            //        throw ErrorFactory.TypesNotSolved(node);
            //    return true;
            //}

            //if (candidates.Count == 1)
            //{
            //    if (_state.CurrentSolver.SetCall(ToCallDef(node, candidates[0])))
            //        return true;

            //    throw ErrorFactory.TypesNotSolved(node);
            //}

            ////User functions get priority
            //var userFunctions = candidates.Where(c => c is ConcreteUserFunctionPrototype).ToList();
            //if (userFunctions.Count == 1)
            //    return _state.CurrentSolver.SetCall(ToCallDef(node, userFunctions[0]));

            //return _state.CurrentSolver.SetOverloadCall(candidates.Select(ToFunSignature).ToArray(), node.OrderNumber,
            //    node.Args.Select(a => a.OrderNumber).ToArray());
        }

        //private void ThrowInvalidOperatorCall(FunCallSyntaxNode node, SetTypeResult result)
        //{
        //    if (result.FailedNodeId == node.OrderNumber)
        //        throw ErrorFactory.TypesNotSolved(node);
        //    var failedArg = node.Args.First(a => a.OrderNumber == result.FailedNodeId);
        //    throw ErrorFactory.OperatorOverloadNotFound(node, failedArg);
        //}

        public override bool Visit(IfThenElseSyntaxNode node)
        {
            var conditions = node.Ifs.Select(i => i.Condition.OrderNumber).ToArray();
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
                _state.CurrentSolver.SetConst(node.OrderNumber, type as Primitive);
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

       

        public override  bool Visit(VariableSyntaxNode node)
        {
            Trace(node,$"VAR {node.Id} ");

            var originId = node.Id;
            var localId = _state.GetActualName(node.Id);
            _state.CurrentSolver.SetVar(localId, node.OrderNumber);
            return true;

            //var originId = node.Id;

            //var localId = _state.GetActualName(node.Id);
            //if (_state .CurrentSolver.HasVariable(localId))
            //    return _state.CurrentSolver.SetVar(node.OrderNumber, localId);

            //if (_state.CurrentSolver.HasVariable(originId))
            //    return _state.CurrentSolver.SetVar(node.OrderNumber, originId);

            //var userFunctions 
            //    = _dictionary.GetNonGeneric(originId).OfType<ConcreteUserFunctionPrototype>().ToList();

            ////ambiguous function reference
            ////Several functions fits
            //if (userFunctions.Count > 1)
            //    throw ErrorFactory.AmbiguousFunctionChoise(node);

            ////if there is no functions - set variable with local name
            //if (userFunctions.Count == 0)
            //    return _state.CurrentSolver.SetVar(node.OrderNumber, localId);

            ////Make fun variable:
            //_state.CurrentSolver.SetVarType(
            //    originId,
            //    userFunctions[0].GetHmFunctionalType());
            //return _state.CurrentSolver.SetVar(node.OrderNumber, originId);
        }


        //private static TiFunctionSignature ToFunSignature(FunctionBase fun) 
        //    => new TiFunctionSignature( fun.ReturnType.ConvertToTiType(), 
        //            fun.ArgTypes.Select(LangTiHelper.ConvertToTiType).ToArray());

        //private static CallDefinition ToCallDef(FunCallSyntaxNode node, FunctionBase fun)
        //{
        //    var ids = new[] {node.OrderNumber}.Concat(node.Args.Select(a => a.OrderNumber)).ToArray();
        //    var types = new[] {fun.ReturnType}.Concat(fun.ArgTypes).Select(LangTiHelper.ConvertToTiType).ToArray();

        //    var callDef = new CallDefinition(types, ids);
        //    return callDef;
        //}
        //private static CallDefinition ToCallDef(FunCallSyntaxNode node, GenericFunctionBase fun)
        //{
        //    var ids = new[] {node.OrderNumber}.Concat(node.Args.Select(a => a.OrderNumber)).ToArray();
        //    var types = new[] {fun.ReturnType}.Concat(fun.ArgTypes).Select(LangTiHelper.ConvertToTiType).ToArray();

        //    var callDef = new CallDefinition(types, ids);
        //    return callDef;
        //}
        private void Trace(ISyntaxNode node, string text) =>
            Console.WriteLine($"Exit:{node.OrderNumber}. {text} ");
        private bool HandleOperatorFunction(FunCallSyntaxNode node)
        {
            switch (node.Id)
            {
                case CoreFunNames.Negate:
                {
                    _state.CurrentSolver.SetNegateCall(
                        node.Args[0].OrderNumber, node.OrderNumber);
                    return true;
                }
                case CoreFunNames.Multiply:
                case CoreFunNames.Add:
                case CoreFunNames.Substract:
                case CoreFunNames.Remainder:
                {
                    _state.CurrentSolver.SetArith(
                        node.Args[0].OrderNumber,
                        node.Args[1].OrderNumber,
                        node.OrderNumber);
                    return true;
                }
                case CoreFunNames.Divide:
                {
                    _state.CurrentSolver.SetCall(Primitive.Real, 
                        node.Args[0].OrderNumber,
                        node.Args[1].OrderNumber, 
                        node.OrderNumber);
                    return true;
                }
                case CoreFunNames.BitShiftLeft:
                case CoreFunNames.BitShiftRight:
                {
                    _state.CurrentSolver.SetBitShift(
                        node.Args[0].OrderNumber,
                        node.Args[1].OrderNumber, node.OrderNumber);
                    return true;
                }

                case CoreFunNames.LessOrEqual:
                case CoreFunNames.Less:
                case CoreFunNames.MoreOrEqual:
                case CoreFunNames.More:
                {
                    _state.CurrentSolver.SetComparable(
                        node.Args[0].OrderNumber,
                        node.Args[1].OrderNumber,
                        node.Args[1].OrderNumber);
                    return true;
                }
            }
            throw  new InvalidOperationException();
        }

    }

}