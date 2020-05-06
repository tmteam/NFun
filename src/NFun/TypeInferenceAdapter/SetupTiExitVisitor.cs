using System;
using System.Linq;
using NFun.BuiltInFunctions;
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

        public override bool Visit(UserFunctionDefenitionSyntaxNode node) => true;

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
            var userFunction = _resultsBuilder.GetUserFunctionSignature(node.Id, node.Args.Length);
            if (userFunction != null) {
                //Это вызов пользовательской функции. Например в случае рекурсии
                Trace(node, $"Call UF{node.Id}({string.Join(",", node.Args.Select(a => a.OrderNumber))})");
                _state.CurrentSolver.SetCall(userFunction, node.Args.Select(a => a.OrderNumber).Append(node.OrderNumber).ToArray());
                //Если функция является дженериковой и рекурсивной, то мы пока не знаем ограничения дженериков
                //В таком случае - единственное что мы можем - это запомнить тип рекурсивного вызова
                _resultsBuilder.RememberRecursiveCall(node.OrderNumber, userFunction);
                return true;
            }

            //Вызов обычной функции
            Trace(node, $"Call {node.Id}({string.Join(",", node.Args.Select(a => a.OrderNumber))})");

            var signature = _resultsBuilder.GetSignatureOrNull(node.OrderNumber);

            RefTo[] genericTypes;
            if (signature is GenericFunctionBase t)
            {
                //Если это дженерик функция - то нужно сохранить типы дженериков с которыми она вызывается
                //что бы не вычислять эти типы заново на этапе построения
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
            Trace(node, $"Constant {node.Value}:{node.ClrTypeName}");
            var type = LangTiHelper.ConvertToTiType(node.OutputType);
            
            if (type is Primitive p)
                    _state.CurrentSolver.SetConst(node.OrderNumber, p);
            else if (type is Array a && a.Element is Primitive primitiveElement)
                    _state.CurrentSolver.SetArrayConst(node.OrderNumber, primitiveElement);
            else
                throw new InvalidOperationException("Complex constant type is not supported");
            return true;
        }

        public override bool Visit(GenericIntSyntaxNode node)
        {
            Trace(node, $"IntConst {node.Value}:{(node.IsHexOrBin ? "hex" : "int")}");

            if (node.IsHexOrBin)
            {
               //hex or bin constant
               //can be u8:< c:< i96
                ulong actualValue;
                if (node.Value is long l)
                {
                    if (l > 0) actualValue = (ulong) l;
                    else
                    {
                        //negative constant
                        if (l >= Int16.MinValue)      _state.CurrentSolver.SetIntConst(node.OrderNumber, Primitive.I16, Primitive.I64, Primitive.I32);
                        else if (l >= Int32.MinValue) _state.CurrentSolver.SetIntConst(node.OrderNumber, Primitive.I32, Primitive.I64, Primitive.I32);
                        else                          _state.CurrentSolver.SetConst(node.OrderNumber, Primitive.I64);
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
                else if (actualValue <= (ulong) Int16.MaxValue)
                    _state.CurrentSolver.SetIntConst(node.OrderNumber, Primitive.U12, Primitive.I96, Primitive.I32);
                else if (actualValue <= (ulong) UInt16.MaxValue)
                    _state.CurrentSolver.SetIntConst(node.OrderNumber, Primitive.U16, Primitive.I96, Primitive.I32);
                else if (actualValue <= (ulong) Int32.MaxValue)
                    _state.CurrentSolver.SetIntConst(node.OrderNumber, Primitive.U24, Primitive.I96, Primitive.I32);
                else if (actualValue <= (ulong) UInt32.MaxValue)
                    _state.CurrentSolver.SetIntConst(node.OrderNumber, Primitive.U32, Primitive.I96, Primitive.I64);
                else if (actualValue <= (ulong) Int64.MaxValue)
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
                        if (l >= Int16.MinValue)      descedant= Primitive.I16;
                        else if (l >= Int32.MinValue) descedant= Primitive.I32;
                        else                          descedant= Primitive.I64;
                        _state.CurrentSolver.SetIntConst(node.OrderNumber, descedant);
                        return true;
                    }
                }
                else if (node.Value is ulong u)
                    actualValue = u;
                else
                    throw new ImpossibleException("Generic token has to be ulong or long");

                //positive constant
                if (actualValue <= byte.MaxValue)               descedant = Primitive.U8;
                else if (actualValue <= (ulong)Int16.MaxValue)  descedant = Primitive.U12;
                else if (actualValue <= (ulong)UInt16.MaxValue) descedant = Primitive.U16;
                else if (actualValue <= (ulong)Int32.MaxValue)  descedant = Primitive.U24;
                else if (actualValue <= (ulong)UInt32.MaxValue) descedant = Primitive.U32;
                else if (actualValue <= (ulong)Int64.MaxValue)  descedant = Primitive.U48;
                else                                            descedant = Primitive.U64;
                _state.CurrentSolver.SetIntConst(node.OrderNumber, descedant);

            }

            return true;
        }

        public override  bool Visit(TypedVarDefSyntaxNode node)
        {
            Trace(node, $"Tvar {node.Id}:{node.VarType}  ");
            if (node.VarType != VarType.Empty)
            {
                var type = LangTiHelper.ConvertToTiType(node.VarType);
                _state.CurrentSolver.SetVarType(node.Id, type);
            }
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
                if (signature != null)
                {
                    if (signature is GenericFunctionBase genericFunction)
                    {
                        var generics = InitializeGenericTypes(genericFunction.GenericDefenitions);
                        _resultsBuilder.SetGenericTypes(node.OrderNumber, generics);

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
            //ставим обычную переменную
            var localId = _state.GetActualName(node.Id);
            _state.CurrentSolver.SetVar(localId, node.OrderNumber);
            return true;
        }

        private void Trace(ISyntaxNode node, string text) =>
            TraceLog.Write($"Exit:{node.OrderNumber}. {text} ");
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