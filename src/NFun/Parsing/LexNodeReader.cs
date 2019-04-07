using System.Collections.Generic;
using System.Linq;
using NFun.BuiltInFunctions;
using NFun.ParseErrors;
using NFun.Runtime;
using NFun.Tokenization;

namespace NFun.Parsing
{
    public class LexNodeReader
    {
        private readonly TokenFlow _flow;

        static LexNodeReader()
        {
            var priorities = new List<TokType[]>();
            priorities.Add(new []
            {
                TokType.AnonymFun,
                TokType.ArrOBr,
                TokType.PipeForward,
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
                TokType.BitShiftRight,
                TokType.BitAnd, 
                TokType.BitXor 
            });
            priorities.Add(new[]
            {
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
                {TokType.Plus,CoreFunNames.Add},
                {TokType.Minus,CoreFunNames.Substract},
                {TokType.Mult,CoreFunNames.Multiply},
                {TokType.Div,CoreFunNames.Divide},
                {TokType.Rema,CoreFunNames.Remainder},
                {TokType.Pow,CoreFunNames.Pow},

                {TokType.And,CoreFunNames.And},
                {TokType.Or,CoreFunNames.Or},
                {TokType.Xor,CoreFunNames.Xor},
                
                {TokType.BitAnd,CoreFunNames.BitAnd},
                {TokType.BitOr,CoreFunNames.BitOr},
                {TokType.BitXor,CoreFunNames.BitXor},
                
                {TokType.More,CoreFunNames.More},
                {TokType.MoreOrEqual,CoreFunNames.MoreOrEqual},
                {TokType.Less,CoreFunNames.Less},
                {TokType.LessOrEqual,CoreFunNames.LessOrEqual},

                {TokType.Equal,CoreFunNames.Equal},
                {TokType.NotEqual,CoreFunNames.NotEqual},

                {TokType.BitShiftLeft,CoreFunNames.BitShiftLeft},
                {TokType.BitShiftRight,CoreFunNames.BitShiftRight},
                {TokType.ArrConcat, CoreFunNames.ArrConcat},
                {TokType.In,CoreFunNames.In},
            };
        public LexNodeReader(TokenFlow flow)
        {
            _flow = flow;
        }

        public LexNode ReadExpressionOrNull()
            => ReadNext(MaxPriority);

        //ReadZeroPriority operation (num, -num, id, fun, if, (...))
        private LexNode ReadAtomicOrNull()
        {
            _flow.SkipNewLines();

            //-num turns to (-1 * num)
            var start = _flow.Position;
            if (_flow.IsCurrent(TokType.Minus))
            {
                if (_flow.IsPrevious(TokType.Minus))
                    throw ErrorFactory.MinusDuplicates(_flow.Previous, _flow.Current);
                _flow.MoveNext();
                
                var nextNode = ReadAtomicOrNull();
                if (nextNode == null)
                    throw ErrorFactory.UnaryArgumentIsMissing(_flow.Current);
                
                var negativeNode = LexNode.Num("-1", start, start);
                return LexNode.OperatorFun(
                    CoreFunNames.Multiply,
                    new[]{negativeNode, nextNode},start, nextNode.Finish);
            }

            if (_flow.MoveIf(TokType.BitInverse))
            {
                var node = ReadNext(1);
                if(node==null)
                    throw ErrorFactory.UnaryArgumentIsMissing(_flow.Current);
                return LexNode.OperatorFun(
                    CoreFunNames.BitInverse, 
                    new []{node}, start, node.Finish);
            }
            if (_flow.MoveIf(TokType.Not))
            {
                var node = ReadNext(5);
                if(node==null)
                    throw ErrorFactory.UnaryArgumentIsMissing(_flow.Current);
                return LexNode.OperatorFun(
                    CoreFunNames.Not, 
                    new []{node},start, node.Finish);
            }
            if (_flow.MoveIf(TokType.True, out var trueTok))
                return LexNode.Num(trueTok);
            if (_flow.MoveIf(TokType.False, out var falseTok))
                return LexNode.Num(falseTok);
            if (_flow.MoveIf(TokType.Number, out var val))
                return LexNode.Num(val);
            if (_flow.MoveIf(TokType.Text, out var txt))
                return LexNode.Text(txt);
            if (_flow.MoveIf(TokType.Id, out var headToken))
            {
                if (_flow.IsCurrent(TokType.Obr))
                    return ReadFunctionCall(headToken);
                
                if (_flow.IsCurrent(TokType.Colon))
                {
                    _flow.MoveNext();
                    var type = _flow.ReadVarType();
                    return LexNode.TypedVar(headToken.Value, type, headToken.Start, _flow.Position);
                }
                else
                    return LexNode.Var(headToken);
            }
            if (_flow.IsCurrent(TokType.Obr))
                return ReadBrackedListOrNull();
            if (_flow.IsCurrent(TokType.If))
                return ReadIfThenElse();
            if (_flow.IsCurrent(TokType.ArrOBr))
                return ReadInitializeArray();
            if (_flow.IsCurrent(TokType.NotAToken))
                throw ErrorFactory.NotAToken(_flow.Current);
            return null;
        }


        LexNode ReadNext(int priority)
        {
            //Lower priority is the special case
            if (priority == 0)
                return ReadAtomicOrNull();

            //starting with left Node
            var leftNode = ReadNext(priority - 1);

            //building the syntax tree
            while (true)
            {
                _flow.SkipNewLines();
                //if flow is done than current node is everything we got
                // example:
                // 1*2+3 {return whole expression }
                if (_flow.IsDone)
                    return leftNode;
                
                var opToken = _flow.Current;
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
                
                if (opToken.Type == TokType.ArrOBr)
                    leftNode = ReadArraySliceNode(leftNode);
                else if (opToken.Type == TokType.PipeForward)
                {
                    _flow.MoveNext();
                    if(!_flow.MoveIf(TokType.Id, out var id))
                        throw ErrorFactory.FunctionNameIsMissedAfterPipeForward(opToken);
                    leftNode =  ReadFunctionCall(id, leftNode);       
                }
                else if (opToken.Type == TokType.AnonymFun)
                {
                    _flow.MoveNext();
                    var body = ReadExpressionOrNull();       
                    leftNode = LexNode.AnonymFun(leftNode, body);
                }
                
                else
                {
                    _flow.MoveNext();

                    var rightNode = ReadNext(priority - 1);
                    if (rightNode == null)
                        throw ErrorFactory.RightBinaryArgumentIsMissing(leftNode, opToken);
                    
                    //building the tree from the left                    
                    if (OperatorFunNames.ContainsKey(opToken.Type))
                        leftNode = LexNode.OperatorFun(
                            OperatorFunNames[opToken.Type],
                            new[]{leftNode, rightNode},
                            leftNode.Start, 
                            rightNode.Finish);
                    else
                        throw ErrorFactory.OperatorIsUnknown(opToken);

                    //trace:
                    //ReadNext(priority: 3 ) // *,/,%,AND
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

        private LexNode ReadArraySliceNode(LexNode arrayNode)
        {
            var openBraket = _flow.Current;
            _flow.MoveNext();
            var index = ReadExpressionOrNull();
            
            if (!_flow.MoveIf(TokType.Colon, out var colon))
            {
                if (index == null)
                {
                    if(_flow.MoveIf(TokType.ArrCBr, out var closeBracket))
                        throw ErrorFactory.ArrayIndexExpected(openBraket,closeBracket);
                    else    
                        throw ErrorFactory.ArrayIndexOrSliceExpected(openBraket);
                }
                if(!_flow.MoveIf(TokType.ArrCBr))
                    throw ErrorFactory.ArrayIndexCbrMissed(openBraket,_flow.Current);
                    
                return LexNode.OperatorFun(
                    CoreFunNames.GetElementName, 
                    new[] {arrayNode, index},openBraket.Start, 
                    _flow.Position);
            }
            
            index = index ?? LexNode.Num("0",openBraket.Start, colon.Finish);
            
            var end = ReadExpressionOrNull()?? LexNode.Num(int.MaxValue.ToString(),colon.Finish, _flow.Position);
            
            if (!_flow.MoveIf(TokType.Colon, out _))
            {
                if(!_flow.MoveIf(TokType.ArrCBr))
                    throw ErrorFactory.ArraySliceCbrMissed(openBraket,_flow.Current, false);
                return LexNode.OperatorFun(CoreFunNames.SliceName, new[]
                {
                    arrayNode, 
                    index, 
                    end
                }, openBraket.Start, _flow.Position);
            }
            
            var step = ReadExpressionOrNull();
            if(!_flow.MoveIf(TokType.ArrCBr))
                throw ErrorFactory.ArraySliceCbrMissed(openBraket,_flow.Current, true);
            if(step==null)
                return LexNode.OperatorFun(CoreFunNames.SliceName, new[] {
                    arrayNode, index, end
                },openBraket.Start, _flow.Position);
            
            return LexNode.OperatorFun(CoreFunNames.SliceName, new[] {
                arrayNode, index, end, step
            },openBraket.Start, _flow.Position);
        }


        #region  read concreete
        IList<LexNode> ReadNodeList()
        {
            var list = new List<LexNode>();
            int start = _flow.Current.Start;
            do
            {
                var exp = ReadExpressionOrNull();
                if (exp != null)
                    list.Add(exp);
                else if (list.Count > 0)
                    throw ErrorFactory.BracketExpressionMissed(start, _flow.Position, list);
                else
                    break;
            } while (_flow.MoveIf(TokType.Sep, out _));
            return list;
        }

        private LexNode ReadInitializeArray()
        {
            var openBracket = _flow.MoveIfOrThrow(TokType.ArrOBr);
            var list = ReadNodeList();
            if (list.Count == 1 && _flow.MoveIf(TokType.TwoDots, out var twoDots))
            {
                var secondArg = ReadExpressionOrNull();
                if (secondArg == null)
                {
                    var lastToken = twoDots;
                    var missedVal = _flow.Current;
                    if (_flow.Current.Is(TokType.ArrCBr)) {
                        lastToken = _flow.Current;
                        missedVal = default(Tok);
                    }
                    else if(_flow.Current.Is(TokType.TwoDots)) {
                        lastToken = _flow.Current;
                        missedVal = default(Tok);                        
                    }
                    throw ErrorFactory.ArrayInitializeSecondIndexMissed(
                        openBracket, lastToken, missedVal);
                }

                if (_flow.MoveIf(TokType.TwoDots, out var secondTwoDots))
                {
                    var thirdArg = ReadExpressionOrNull();
                    if (thirdArg == null)
                    {
                        var lastToken = secondTwoDots;
                        var missedVal = _flow.Current;
                        if (_flow.Current.Is(TokType.ArrCBr)) {
                            lastToken = _flow.Current;
                            missedVal = default(Tok);
                        }
                        else if(_flow.Current.Is(TokType.TwoDots)) {
                            lastToken = _flow.Current;
                            missedVal = default(Tok);                        
                        }
                        throw ErrorFactory.ArrayInitializeStepMissed(
                            openBracket, lastToken, missedVal);
                    }
                    if (!_flow.MoveIf(TokType.ArrCBr, out var closeBracket))
                        throw ErrorFactory.ArrayIntervalInitializeCbrMissed(openBracket, _flow.Current, true);
                    return LexNode.ProcArrayInit(
                        from: list[0],
                        step: secondArg,
                        to: thirdArg,
                        start: openBracket.Start, 
                        end:closeBracket.Finish);
                }
                else
                {
                    if (!_flow.MoveIf(TokType.ArrCBr,out var closeBracket))
                        throw ErrorFactory.ArrayIntervalInitializeCbrMissed(openBracket, _flow.Current, false);
                    return LexNode.ProcArrayInit(
                        @from: list[0], 
                        to: secondArg,
                        start: openBracket.Start, 
                        end:closeBracket.Finish);
                }
            }
            if (!_flow.MoveIf(TokType.ArrCBr,out var closeBr))
                throw ErrorFactory.ArrayEnumInitializeCbrMissed(openBracket.Start, _flow.Position, list);
            return LexNode.Array(list.ToArray(), openBracket.Start, closeBr.Finish);
        }
        private LexNode ReadBrackedListOrNull()
        {
            int start = _flow.Current.Start;
            _flow.MoveNext();
            var nodeList = ReadNodeList();
            if (nodeList.Count == 0)
                throw ErrorFactory.BracketExpressionMissed(start, _flow.Position, nodeList);
            if (!_flow.MoveIf(TokType.Cbr, out var cbr))
                throw ErrorFactory.BracketExprCbrOrSeparatorMissed(start, _flow.Position, nodeList);
            var interval = new Interval(start, cbr.Finish);
            if (nodeList.Count == 1)
            {
                nodeList[0].Interval = interval;
                nodeList[0].IsBracket = true;
                return nodeList[0];
            }
            else
                return LexNode.ListOf(nodeList.ToArray(), interval, true);
        }

        private LexNode ReadIfThenElse()
        {
            int ifElseStart = _flow.Position;
            var ifThenNodes = new List<LexNode>();
            do
            {
                int conditionStart = _flow.Current.Start;
                if(!_flow.MoveIf(TokType.If))
                    throw ErrorFactory.IfKeywordIsMissing(ifElseStart, _flow.Position);

                var condition = ReadExpressionOrNull();
                if (condition == null)
                    throw ErrorFactory.ConditionIsMissing(conditionStart, _flow.Position);
                
                if(!_flow.MoveIf(TokType.Then))
                    throw ErrorFactory.ThenKeywordIsMissing(ifElseStart, _flow.Position);

                var thenResult = ReadExpressionOrNull();
                if (thenResult == null)
                    throw ErrorFactory.ThenExpressionIsMissing(conditionStart, _flow.Position);
                
                ifThenNodes.Add(LexNode.IfThen(condition, thenResult, 
                    start: conditionStart, 
                    end: _flow.Position));
            } while (!_flow.IsCurrent(TokType.Else));
            
            if(!_flow.MoveIf(TokType.Else))
                throw ErrorFactory.ElseKeywordIsMissing(ifElseStart, _flow.Position);

            var elseResult = ReadExpressionOrNull();
            if (elseResult == null)
                throw ErrorFactory.ElseExpressionIsMissing(ifElseStart, _flow.Position);

            return LexNode.IfElse(ifThenNodes, elseResult, ifElseStart, _flow.Position);
        }

        private LexNode ReadFunctionCall(Tok head, LexNode pipedVal=null)
        {
            var start = pipedVal?.Start ?? head.Start;
            if(!_flow.MoveIf(TokType.Obr))
                throw ErrorFactory.FunctionCallObrMissed(
                    start, 
                    head.Value, 
                    _flow.Position, 
                    pipedVal);

            var arguments = ReadNodeList();
            if(pipedVal!=null)
                arguments.Insert(0,pipedVal);
            if(!_flow.MoveIf(TokType.Cbr, out var cbr))
                throw ErrorFactory.FunctionCallCbrOrSeparatorMissed(start, head.Value, arguments, _flow.Current, pipedVal); 
            return LexNode.FunCall(head.Value, arguments.ToArray(), start, cbr.Finish);
        }
        #endregion
    }
}
