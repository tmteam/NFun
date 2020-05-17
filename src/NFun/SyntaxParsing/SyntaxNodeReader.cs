using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using NFun.BuiltInFunctions;
using NFun.Exceptions;
using NFun.ParseErrors;
using NFun.Runtime;
using NFun.Runtime.Arrays;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.SyntaxParsing
{
    /// <summary>
    /// Reads concrete syntax nodes from token flow
    /// </summary>
    public static class SyntaxNodeReader
    {
        static SyntaxNodeReader()
        {
            var priorities = new List<TokType[]>();
            priorities.Add(new []
            {
                TokType.AnonymFun,
                TokType.ArrOBr,
                TokType.PipeForward,
                TokType.Obr
            });
            
            priorities.Add(new[]{TokType.Pow});

            priorities.Add( new []{
                TokType.Mult,
                TokType.Div,
                TokType.Rema,
            });
            priorities.Add(new[]
            {
                TokType.Plus,
                TokType.Minus,
                TokType.BitShiftLeft, 
                TokType.BitShiftRight
            });
            priorities.Add(new[]
            {
                TokType.BitAnd,
                TokType.BitXor,
                TokType.In,
                TokType.Equal,
                TokType.NotEqual,
                TokType.More,
                TokType.Less,
                TokType.MoreOrEqual,
                TokType.LessOrEqual,
            });
            
            priorities.Add( new []{
               TokType.And,
               TokType.ArrConcat
            });
            
            priorities.Add( new [] {
                TokType.Or,
                TokType.Xor,
                TokType.BitOr
            });
            
            for (byte i = 0; i < priorities.Count; i++)
            {
                foreach (var tokType in priorities[i])         
                    Priorities.Add(tokType, i);
            }

            MaxPriority = priorities.Count - 1;
        }

        private static readonly int MaxPriority;

        private static readonly Dictionary<TokType, byte> Priorities
            = new Dictionary<TokType, byte>();

        private static readonly Dictionary<TokType, string> OperatorFunNames
            = new Dictionary<TokType, string>()
            {
                {TokType.Plus, CoreFunNames.Add},
                {TokType.Minus,CoreFunNames.Substract},
                {TokType.Mult, CoreFunNames.Multiply},
                {TokType.Div,  CoreFunNames.Divide},
                {TokType.Rema, CoreFunNames.Remainder},
                {TokType.Pow,  CoreFunNames.Pow},

                {TokType.And,  CoreFunNames.And},
                {TokType.Or,   CoreFunNames.Or},
                {TokType.Xor,  CoreFunNames.Xor},
                
                {TokType.BitAnd,CoreFunNames.BitAnd},
                {TokType.BitOr, CoreFunNames.BitOr},
                {TokType.BitXor,CoreFunNames.BitXor},
                
                {TokType.More,       CoreFunNames.More},
                {TokType.MoreOrEqual,CoreFunNames.MoreOrEqual},
                {TokType.Less,       CoreFunNames.Less},
                {TokType.LessOrEqual,CoreFunNames.LessOrEqual},

                {TokType.Equal,      CoreFunNames.Equal},
                {TokType.NotEqual,   CoreFunNames.NotEqual},

                {TokType.BitShiftLeft, CoreFunNames.BitShiftLeft},
                {TokType.BitShiftRight,CoreFunNames.BitShiftRight},
                {TokType.ArrConcat,    CoreFunNames.ArrConcat},
                {TokType.In,           CoreFunNames.In},
            };
        
        public static ISyntaxNode ReadNodeOrNull(TokFlow flow)
            => ReadNodeOrNull(flow,MaxPriority);
        
        /// <summary>
        /// Reads node with lowest priority
        /// Equiualent to ReadNodeOrNull(0)
        /// num, -num, id, fun, if, (...)
        /// throws FunParseException if underlying syntax is invalid,
        /// or returns null if underlying syntax cannot be represented as atomic node
        /// (EOF for example)
        /// /// </summary>
        public static  ISyntaxNode ReadAtomicNodeOrNull(TokFlow flow)
        {
            flow.SkipNewLines();

            //-num turns to (-1 * num)
            var start = flow.Position;
            if (flow.IsCurrent(TokType.Minus))
            {
                if (flow.IsPrevious(TokType.Minus))
                    throw ErrorFactory.MinusDuplicates(flow.Previous, flow.Current);
                flow.MoveNext();
                
                var nextNode = ReadAtomicNodeOrNull(flow);
                if (nextNode == null)
                    throw ErrorFactory.UnaryArgumentIsMissing(flow.Current);

                var interval = new Interval(start, nextNode.Interval.Finish);
                if (nextNode is ConstantSyntaxNode constant)
                {
                    switch (constant.Value)
                    {
                        case ulong u64 when u64 > long.MaxValue:
                            throw FunParseException.ErrorStubToDo("Token overflow 1");
                        case ulong u64:
                            return new ConstantSyntaxNode(-(long)u64, constant.OutputType, interval);
                        case long i64:
                            return new ConstantSyntaxNode(-i64, constant.OutputType, interval);
                        case double d:
                            return new ConstantSyntaxNode(-d, constant.OutputType, interval);
                    }
                }
                else if (nextNode is GenericIntSyntaxNode g)
                {
                    switch (g.Value)
                    {
                        case ulong u64 when u64 > long.MaxValue:
                            throw FunParseException.ErrorStubToDo("Token overflow 2");
                        case ulong u64:
                            return new GenericIntSyntaxNode(-(long)u64, g.IsHexOrBin, interval);
                        case long i64:
                            return new GenericIntSyntaxNode(-i64, g.IsHexOrBin, interval);
                    }
                }
                return SyntaxNodeFactory.OperatorFun(
                    CoreFunNames.Negate, new[]{nextNode}, start, nextNode.Interval.Finish);
            }

            if (flow.MoveIf(TokType.BitInverse))
            {
                var node = ReadNodeOrNull(flow,1);
                if(node==null)
                    throw ErrorFactory.UnaryArgumentIsMissing(flow.Current);
                return SyntaxNodeFactory.OperatorFun(
                    CoreFunNames.BitInverse, 
                    new []{node}, start, node.Interval.Finish);
            }
            if (flow.MoveIf(TokType.Not))
            {
                var node = ReadNodeOrNull(flow,5);
                if(node==null)
                    throw ErrorFactory.UnaryArgumentIsMissing(flow.Current);
                return SyntaxNodeFactory.OperatorFun(
                    CoreFunNames.Not, 
                    new []{node},start, node.Interval.Finish);
            }
            if (flow.MoveIf(TokType.FiObr))
                return ReadSuperAnonymousFunction(flow);
            if (flow.MoveIf(TokType.True, out var trueTok))
                return SyntaxNodeFactory.Constant(true, VarType.Bool,  trueTok.Interval);
            if (flow.MoveIf(TokType.False, out var falseTok))
                return SyntaxNodeFactory.Constant(false, VarType.Bool,  falseTok.Interval);
            if (flow.MoveIf(TokType.HexOrBinaryNumber, out var binVal)) {//0xff, 0b01
                var val = binVal.Value;
                int dimensions;
                if (val[1] == 'b')      dimensions = 2;
                else if (val[1] == 'x') dimensions = 16;
                else throw new ImpossibleException("Hex or bin constant has invalid format: "+val);
                
                var uval = Convert.ToUInt64(val.Replace("_",null).Substring(2), dimensions);
                return SyntaxNodeFactory.HexOrBinIntConstant(uval, binVal.Interval);
            }
            if (flow.MoveIf(TokType.IntNumber, out var intVal))        //1,2,3
                return SyntaxNodeFactory.IntGenericConstant(ulong.Parse(intVal.Value.Replace("_", String.Empty)), intVal.Interval);
            if (flow.MoveIf(TokType.RealNumber, out var realVal))       //1.0
                return SyntaxNodeFactory.Constant(double.Parse(realVal.Value.Replace("_", String.Empty), CultureInfo.InvariantCulture), VarType.Real, realVal.Interval);
            if (flow.MoveIf(TokType.Text, out var txt))
                return SyntaxNodeFactory.Constant(new TextFunArray(txt.Value),VarType.Text,txt.Interval);
            if (flow.MoveIf(TokType.Id, out var headToken))
            {
                //fun call
                // 'id(1,2)'
                if (flow.IsCurrent(TokType.Obr)) 
                    return ReadFunctionCall(flow, headToken);
                //fun call with super anonymous argument
                // 'id { it*2 }'
                if (flow.IsCurrent(TokType.FiObr))
                    return ReadFunctionCall(flow, headToken);
                // variable with type defenition
                //'id:int'
                if (flow.IsCurrent(TokType.Colon))
                {
                    flow.MoveNext();
                    var type = flow.ReadVarType();
                    return SyntaxNodeFactory.TypedVar(headToken.Value, type, headToken.Start, flow.Position);
                }
                // just variable
                // 'id'
                return SyntaxNodeFactory.Var(headToken);
            }

            if (flow.IsCurrent(TokType.TextOpenInterpolation))
                return ReadInterpolationText(flow);
            if (flow.IsCurrent(TokType.Obr))
                return ReadBrackedNodeList(flow);
            if (flow.IsCurrent(TokType.If))
                return ReadIfThenElseNode(flow);
            // '[' can be used as array index, only if there is new line
            if (flow.IsCurrent(TokType.ArrOBr))
                return ReadInitializeArrayNode(flow);
            if (flow.IsCurrent(TokType.NotAToken))
                throw ErrorFactory.NotAToken(flow.Current);
            return null;
        }


        /// <summary>
        /// Reads node with specified syntax priority
        /// throws FunParseException if underlying syntax is invalid,
        /// or returns null if underlying syntax cannot be represented as node
        /// (EOF for example)
        /// </summary>
        public static ISyntaxNode ReadNodeOrNull(TokFlow flow, int priority)
        {
            //Lower priority is the special case
            if (priority == 0)
                return ReadAtomicNodeOrNull(flow);

            //starting with left Node
            var leftNode = ReadNodeOrNull(flow, priority - 1);

            //building the syntax tree
            while (true)
            {
                flow.SkipNewLines();
                //if flow is done than current node is everything we got
                // example:
                // 1*2+3 {return whole expression }
                if (flow.IsDone)
                    return leftNode;
                
                var opToken = flow.Current;
                //if current token is not an operation
                //than expression is done
                //example:
                // 1*2 \r{return expression} y=...
                if (!Priorities.TryGetValue(opToken.Type, out var opPriority))
                    return leftNode;
                
                //if op has higher priority us
                //than expression is done
                // example:
                //2*3{stops here}-1
                if (opPriority > priority)
                    return leftNode;
                
                if (leftNode == null)
                    throw ErrorFactory.LeftBinaryArgumentIsMissing(opToken);
                
                if (opToken.Type == TokType.ArrOBr )
                {
                    //We can use array slicing, only if there were no new lines before.
                    //there is problem of choose between anonymous array init and array slice 
                    //otherwise
                    if (flow.IsPrevious(TokType.NewLine))
                        return leftNode;

                    leftNode = ReadArraySliceNode(flow,leftNode);
                }
                else if (opToken.Type == TokType.PipeForward)
                {
                    flow.MoveNext();
                    if(!flow.MoveIf(TokType.Id, out var id))
                        throw ErrorFactory.FunctionNameIsMissedAfterPipeForward(opToken);
                    leftNode =  ReadFunctionCall(flow, id, leftNode);       
                }
                else if (opToken.Type == TokType.AnonymFun)
                {
                    flow.MoveNext();
                    var body = ReadNodeOrNull(flow);       
                    leftNode = SyntaxNodeFactory.AnonymFun(leftNode, body);
                }
                else if (opToken.Type == TokType.Obr)
                {
                    if (flow.IsPrevious(TokType.NewLine))
                        return leftNode;
                    if(!flow.IsPrevious(TokType.Cbr) && !flow.IsPrevious(TokType.FiCbr))
                        return leftNode;
                    //call result of previous expression:
                    // (expr)(arg1, ... argN)
                    leftNode = ReadResultCall(flow, leftNode);
                }
                else if (opToken.Type == TokType.FiObr)
                {
                    var anonFun = ReadSuperAnonymousFunction(flow);
                    //super-anonymous-function
                    // x =  { /*body*/ }
                    if (!(leftNode is VariableSyntaxNode idNode))
                        throw FunParseException.ErrorStubToDo("unexpected anonymous function");
                    
                    leftNode = SyntaxNodeFactory.FunCall(idNode.Id, new[] {anonFun}, 
                        start: idNode.Interval.Start, 
                        end:   anonFun.Interval.Finish);
                }
                else
                {
                    flow.MoveNext();

                    var rightNode = ReadNodeOrNull(flow, priority - 1);
                    if (rightNode == null)
                        throw ErrorFactory.RightBinaryArgumentIsMissing(leftNode, opToken);
                    
                    //building the tree from the left                    
                    if (OperatorFunNames.ContainsKey(opToken.Type))
                        leftNode = SyntaxNodeFactory.OperatorFun(
                            OperatorFunNames[opToken.Type],
                            new[] {leftNode, rightNode},
                            leftNode.Interval.Start, rightNode.Interval.Finish);
                    else
                        throw ErrorFactory.OperatorIsUnknown(opToken);

                    //trace:
                    //ReadNodeOrNull(priority: 3 ) // *,/,%,AND
                    //0: {start} 4/2*5+1
                    //1: {l:4} /2*5+1
                    //2: {l:4}{op:/} 2*5+1
                    //3: {l:4}{op:/}{r:2} *5+1
                    //4: {l:(4/2)} *5+1
                    //5: {l:(4/2)}{op:*}5+1
                    //6: {l:(4/2)}{op:*}{r:5}+1
                    //7: {l:((4/2)*5)}{op:+} 1
                    //8: '+' priority is higter than 3: return l:((4/2)*5)
                }
            }
        }

        private static ISyntaxNode ReadSuperAnonymousFunction(TokFlow flow)
        {
            var body = ReadNodeOrNull(flow);
            if (!flow.MoveIf(TokType.FiCbr))
                throw ErrorFactory.SuperAnonymousFunctionIsNotClose(body.Interval.Start, flow.CurrentTokenPosition);
            return new SuperAnonymFunctionSyntaxNode(body);
        }

        public static ISyntaxNode ReadInterpolationText(TokFlow flow)
        {
            var openInterpolationToken = flow.MoveIfOrThrow(TokType.TextOpenInterpolation);
            //interpolation
            var concatinations = new List<ISyntaxNode>();
            //Open interpolation string
            // '...{ 
            concatinations.Add(SyntaxNodeFactory.Constant(
                new TextFunArray(openInterpolationToken.Value), 
                VarType.Text,
                openInterpolationToken.Interval));

            while (true)
            {
                //Read interpolation body
                //{...} 
                var allNext = ReadNodeOrNull(flow);
                if (allNext == null)
                    throw ErrorFactory.InterpolationExpressionIsMissing(concatinations.Last());

                var toText = SyntaxNodeFactory.FunCall(CoreFunNames.ToText, new[] { allNext }, allNext.Interval.Start,
                    allNext.Interval.Finish);
                concatinations.Add(toText);


                //interpolation end
                // }...'
                if (flow.Current.Type is TokType.TextCloseInterpolation)
                {
                    concatinations.Add(SyntaxNodeFactory.Constant(
                        new TextFunArray(flow.Current.Value),
                        VarType.Text,
                        flow.Current.Interval));
                    flow.MoveNext();
                    var arrayOfTexts = SyntaxNodeFactory.Array(concatinations.ToArray(), openInterpolationToken.Start,
                        flow.Current.Finish);

                    return SyntaxNodeFactory.FunCall(CoreFunNames.ConcatTexts, new[] { arrayOfTexts },
                        openInterpolationToken.Start,
                        flow.Current.Finish);
                }
                //interpolation continuation
                // }...{
                else if (flow.Current.Type is TokType.TextMidInterpolation)
                {
                    concatinations.Add(SyntaxNodeFactory.Constant(
                        new TextFunArray(flow.Current.Value),
                        VarType.Text,
                        openInterpolationToken.Interval));
                    flow.MoveNext();
                }
                else
                    throw new ImpossibleException("imp328. Invalid interpolation sequence");
            }
        }

        /// <summary>
        /// Read array index or array slice node
        /// </summary>
        public static ISyntaxNode ReadArraySliceNode(TokFlow flow, ISyntaxNode arrayNode)
        {
            var openBraket = flow.Current;
            flow.MoveNext();
            var index = ReadNodeOrNull(flow);
            
            if (!flow.MoveIf(TokType.Colon, out var colon))
            {
                if (index == null)
                {
                    if(flow.MoveIf(TokType.ArrCBr, out var closeBracket))
                        throw ErrorFactory.ArrayIndexExpected(openBraket,closeBracket);
                    else    
                        throw ErrorFactory.ArrayIndexOrSliceExpected(openBraket);
                }
                if(!flow.MoveIf(TokType.ArrCBr))
                    throw ErrorFactory.ArrayIndexCbrMissed(openBraket,flow.Current);
                    
                return SyntaxNodeFactory.OperatorFun(
                    CoreFunNames.GetElementName, 
                    new[] {arrayNode, index},openBraket.Start, 
                    flow.Position);
            }
            
            index = index ?? SyntaxNodeFactory.Constant(0, VarType.Int32, Interval.New(openBraket.Start, colon.Finish));
            
            var end = ReadNodeOrNull(flow)?? 
                      SyntaxNodeFactory.Constant(int.MaxValue, VarType.Int32, Interval.New(colon.Finish, flow.Position));
            
            if (!flow.MoveIf(TokType.Colon, out _))
            {
                if(!flow.MoveIf(TokType.ArrCBr))
                    throw ErrorFactory.ArraySliceCbrMissed(openBraket,flow.Current, false);
                return SyntaxNodeFactory.OperatorFun(CoreFunNames.SliceName, new[]
                {
                    arrayNode, 
                    index, 
                    end
                }, openBraket.Start, flow.Position);
            }
            
            var step = ReadNodeOrNull(flow);
            if(!flow.MoveIf(TokType.ArrCBr))
                throw ErrorFactory.ArraySliceCbrMissed(openBraket,flow.Current, true);
            if(step==null)
                return SyntaxNodeFactory.OperatorFun(CoreFunNames.SliceName, new[] {
                    arrayNode, index, end
                },openBraket.Start, flow.Position);
            
            return SyntaxNodeFactory.OperatorFun(CoreFunNames.SliceName, new[] {
                arrayNode, index, end, step
            },openBraket.Start, flow.Position);
        }
        
        /// <summary>
        /// Read list of nodes separated by comma
        /// </summary>
        public static IList<ISyntaxNode> ReadNodeList(TokFlow flow)
        {
            var list = new List<ISyntaxNode>();
            int start = flow.Current.Start;
            do
            {
                var exp = ReadNodeOrNull(flow);
                if (exp != null)
                    list.Add(exp);
                else if (list.Count > 0)
                    throw ErrorFactory.ExpressionListMissed(start, flow.Position, list);
                else
                    break;
            } while (flow.MoveIf(TokType.Sep, out _));
            return list;
        }
        /// <summary>
        /// Read array initialization node.
        /// [a..b]
        /// [a..b..c]
        /// [a,b,c,d]
        /// </summary>
        /// <param name="flow"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static ISyntaxNode ReadInitializeArrayNode(TokFlow flow)
        {
            var startTokenNum = flow.CurrentTokenPosition;
            var openBracket = flow.MoveIfOrThrow(TokType.ArrOBr);
            
            if (!TryReadNodeList(flow, out var list))
                throw ErrorFactory.ArrayInitializeByListError(startTokenNum, flow);
            
            if (list.Count == 1 && flow.MoveIf(TokType.TwoDots, out var twoDots))// Range [a..b] or [a..b..c]
            {
                var secondArg = ReadNodeOrNull(flow);
                if (secondArg == null)
                {
                    var lastToken = twoDots;
                    var missedVal = flow.Current;
                    
                    if (flow.Current.Is(TokType.ArrCBr)) {
                        lastToken = flow.Current;
                        missedVal = default;
                    }
                    else if(flow.Current.Is(TokType.TwoDots)) { 
                        lastToken = flow.Current;
                        missedVal = default;                        
                    }
                    throw ErrorFactory.ArrayInitializeSecondIndexMissed(
                        openBracket, lastToken, missedVal);
                }

                if (flow.MoveIf(TokType.TwoDots, out var secondTwoDots))
                {
                    var thirdArg = ReadNodeOrNull(flow);
                    if (thirdArg == null)
                    {
                        var lastToken = secondTwoDots;
                        var missedVal = flow.Current;
                        if (flow.Current.Is(TokType.ArrCBr)) {
                            lastToken = flow.Current;
                            missedVal = default;
                        }
                        else if(flow.Current.Is(TokType.TwoDots)) {
                            lastToken = flow.Current;
                            missedVal = default;                        
                        }
                        throw ErrorFactory.ArrayInitializeStepMissed(
                            openBracket, lastToken, missedVal);
                    }
                    if (!flow.MoveIf(TokType.ArrCBr, out var closeBracket))
                        throw ErrorFactory.ArrayIntervalInitializeCbrMissed(openBracket, flow.Current, true);
                    return SyntaxNodeFactory.OperatorFun(
                        name:     CoreFunNames.RangeName,
                        children: new[] {list[0], secondArg, thirdArg}, 
                        start:    openBracket.Start, 
                        end:      closeBracket.Finish);
                }
                else
                {
                    if (!flow.MoveIf(TokType.ArrCBr,out var closeBracket))
                        throw ErrorFactory.ArrayIntervalInitializeCbrMissed(openBracket, flow.Current, false);
                    return SyntaxNodeFactory.OperatorFun(
                            name: CoreFunNames.RangeName,
                            children: new[] { list[0], secondArg},
                            start: openBracket.Start,
                            end: closeBracket.Finish);
                }
            }
            if (!flow.MoveIf(TokType.ArrCBr,out var closeBr))
                throw ErrorFactory.ArrayInitializeByListError(startTokenNum, flow);
            return SyntaxNodeFactory.Array(list.ToArray(), openBracket.Start, closeBr.Finish);
        }
        /// <summary>
        /// Read nodes enlisted in the brackets
        /// (a,b,c)
        /// </summary>
        public static ISyntaxNode ReadBrackedNodeList(TokFlow flow)
        {
            int start = flow.Current.Start;
            int obrId = flow.CurrentTokenPosition;
            flow.MoveNext();
            var nodeList = ReadNodeList(flow);
            if (nodeList.Count == 0)
                throw ErrorFactory.BracketExpressionMissed(start, flow.Position, nodeList);
            if (!flow.MoveIf(TokType.Cbr, out var cbr))
                throw ErrorFactory.BracketExpressionListError(obrId, flow);
            var interval = new Interval(start, cbr.Finish);
            if (nodeList.Count == 1)
            {
                nodeList[0].Interval = interval;
                nodeList[0].IsInBrackets = true;
                return nodeList[0] ?? throw new NullReferenceException();
            }
            else
                return SyntaxNodeFactory.ListOf(nodeList.ToArray(), interval, true);
        }
        /// <summary>
        /// Read if-then-if-then-else node
        /// </summary>
        public static ISyntaxNode ReadIfThenElseNode(TokFlow flow)
        {
            int ifElseStart = flow.Position;
            var ifThenNodes = new List<IfCaseSyntaxNode>();
            do
            {
                int conditionStart = flow.Current.Start;

                var hasNewLineBefore =flow.IsPrevious(TokType.NewLine);
                
                //if
                if(!flow.MoveIf(TokType.If))
                    throw ErrorFactory.IfKeywordIsMissing(ifElseStart, flow.Position);
                if(ifThenNodes.Any() && !hasNewLineBefore)
                    throw ErrorFactory.NewLineMissedBeforeRepeatedIf(flow.Previous.Interval);

                //(condition)
                if (!flow.MoveIf(TokType.Obr))
                {
                    var failedExpr = ReadNodeOrNull(flow);
                    if(failedExpr!=null)
                        throw ErrorFactory.IfConditionIsNotInBrackets(failedExpr.Interval.Start,failedExpr.Interval.Finish);
                    else    
                        throw ErrorFactory.IfConditionIsNotInBrackets(ifElseStart, flow.Position);
                }
                var condition =  ReadNodeOrNull(flow);
                if (condition == null)
                    throw ErrorFactory.ConditionIsMissing(conditionStart, flow.Position);
                if(!flow.MoveIf(TokType.Cbr))
                    throw ErrorFactory.IfConditionIsNotInBrackets(ifElseStart, flow.Position);

                //then
                var thenResult = ReadNodeOrNull(flow);
                if (thenResult == null)
                    throw ErrorFactory.ThenExpressionIsMissing(conditionStart, flow.Position);
                
                ifThenNodes.Add(SyntaxNodeFactory.IfThen(condition, thenResult, 
                    start: conditionStart, 
                    end: flow.Position));
            } while (!flow.IsCurrent(TokType.Else));
            
            if(!flow.MoveIf(TokType.Else))
                throw ErrorFactory.ElseKeywordIsMissing(ifElseStart, flow.Position);

            var elseResult = ReadNodeOrNull(flow);
            if (elseResult == null)
                throw ErrorFactory.ElseExpressionIsMissing(ifElseStart, flow.Position);

            return SyntaxNodeFactory.IfElse(ifThenNodes.ToArray(), elseResult, ifElseStart, flow.Position);
        }
        
        private static ISyntaxNode ReadFunctionCall(TokFlow flow, Tok head, ISyntaxNode pipedVal = null)
        {
            var obrId = flow.CurrentTokenPosition;
            var start = pipedVal?.Interval.Start ?? head.Start;
            List<ISyntaxNode> arguments = null;
            if (flow.MoveIf(TokType.Obr))
            {
                if (!TryReadNodeList(flow, out arguments)
                    || !flow.MoveIf(TokType.Cbr, out _))
                    throw ErrorFactory.FunctionArgumentError(head.Value, obrId, flow);

                if (flow.MoveIf(TokType.FiObr))
                {
                    //super-anonymous-style-function as last param of function
                    // out =  fold(x,0){ /*body*/ }
                    arguments.Add(ReadSuperAnonymousFunction(flow));
                }
            }
            else if (flow.MoveIf(TokType.FiObr))
            {
                //super-anonymous-style-function instead of params of function
                // out =  myFun{ /*body*/ }
                arguments = new List<ISyntaxNode> { ReadSuperAnonymousFunction(flow)};
            } 
            else
            {
                throw ErrorFactory.FunctionCallObrMissed(
                    start, head.Value, flow.Position, pipedVal);
            }

            if(pipedVal!=null)
                arguments.Insert(0,pipedVal);
            
            return SyntaxNodeFactory.FunCall(head.Value, arguments.ToArray(), start, flow.Position);
        }
        private static ISyntaxNode ReadResultCall(TokFlow flow, ISyntaxNode functionResultNode)
        {
            var obrId = flow.CurrentTokenPosition;
            if (!flow.MoveIf(TokType.Obr))
                throw new ImpossibleException("Panic. Something wrong in parser");

            if (!TryReadNodeList(flow, out var arguments)
                || !flow.MoveIf(TokType.Cbr, out var cbr))
                throw ErrorFactory.FunctionArgumentError(functionResultNode.ToString(), obrId, flow);

            return  new ResultFunCallSyntaxNode(functionResultNode,arguments.ToArray(),  Interval.New(functionResultNode.Interval.Start, cbr.Finish));
        }

        private static bool TryReadNodeList(TokFlow flow, out List<ISyntaxNode> read)
        {
            read = new List<ISyntaxNode>();
            do
            {
                var exp = ReadNodeOrNull(flow);
                if (exp != null)
                    read.Add(exp);
                else if (read.Count > 0)
                    return false;
                else
                    break;
            } while (flow.MoveIf(TokType.Sep, out _));
            return true;
        }
    }
}
