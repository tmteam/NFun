using System;
using System.Collections.Generic;
using System.Linq;
using NFun.BuiltInFunctions;
using NFun.ParseErrors;
using NFun.Runtime;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.SyntaxParsing
{
    public class SyntaxNodeReader
    {
        private readonly TokFlow _flow;

        static SyntaxNodeReader()
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
        
        public SyntaxNodeReader(TokFlow flow)
        {
            _flow = flow;
        }

        public ISyntaxNode ReadExpressionOrNull()
            => ReadNext(MaxPriority);
        
        //ReadZeroPriority operation (num, -num, id, fun, if, (...))
        private ISyntaxNode ReadAtomicOrNull()
        {
           var hasNewLineBefore = _flow.SkipNewLines();

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
                
                if (nextNode is ConstantSyntaxNode constant)
                {
                    if (constant.Value is Int32 i32)
                        return new ConstantSyntaxNode(-i32, constant.OutputType, new Interval(start,nextNode.Interval.Finish));
                }
                return SyntaxNodeFactory.OperatorFun(
                    CoreFunNames.Negate,
                    new[]{nextNode}, start, nextNode.Interval.Finish);
            }

            if (_flow.MoveIf(TokType.BitInverse))
            {
                var node = ReadNext(1);
                if(node==null)
                    throw ErrorFactory.UnaryArgumentIsMissing(_flow.Current);
                return SyntaxNodeFactory.OperatorFun(
                    CoreFunNames.BitInverse, 
                    new []{node}, start, node.Interval.Finish);
            }
            if (_flow.MoveIf(TokType.Not))
            {
                var node = ReadNext(5);
                if(node==null)
                    throw ErrorFactory.UnaryArgumentIsMissing(_flow.Current);
                return SyntaxNodeFactory.OperatorFun(
                    CoreFunNames.Not, 
                    new []{node},start, node.Interval.Finish);
            }
            if (_flow.MoveIf(TokType.True, out var trueTok))
                return SyntaxNodeFactory.Constant(true, VarType.Bool,  trueTok.Interval);
            if (_flow.MoveIf(TokType.False, out var falseTok))
                return SyntaxNodeFactory.Constant(false, VarType.Bool,  falseTok.Interval);
            if (_flow.MoveIf(TokType.Number, out var val))
            {
                try
                {
                    var (obj, type) = TokenHelper.ToConstant(val.Value);
                    return SyntaxNodeFactory.Constant(obj, type, val.Interval);
                }
                catch (SystemException) {
                    throw ErrorFactory.CannotParseNumber(val.Value, val.Interval);
                }
            }
            if (_flow.MoveIf(TokType.Text, out var txt))
                return SyntaxNodeFactory.Constant( 
                    new TextFunArray(txt.Value), 
                    VarType.Text, 
                    txt.Interval);

            if (_flow.MoveIf(TokType.Id, out var headToken))
            {
                if (_flow.IsCurrent(TokType.Obr))
                    return ReadFunctionCall(headToken);
                
                if (_flow.IsCurrent(TokType.Colon))
                {
                    _flow.MoveNext();
                    var type = _flow.ReadVarType();
                    return SyntaxNodeFactory.TypedVar(headToken.Value, type, headToken.Start, _flow.Position);
                }
                else
                    return SyntaxNodeFactory.Var(headToken);
            }
            if (_flow.IsCurrent(TokType.Obr))
                return ReadBrackedListOrNull();
            if (_flow.IsCurrent(TokType.If))
                return ReadIfThenElse();
            // '[' can be used as array index, only if there is new line
            if (_flow.IsCurrent(TokType.ArrOBr))
                return ReadInitializeArray();
            if (_flow.IsCurrent(TokType.NotAToken))
                throw ErrorFactory.NotAToken(_flow.Current);
            return null;
        }


        ISyntaxNode ReadNext(int priority)
        {
            //Lower priority is the special case
            if (priority == 0)
                return ReadAtomicOrNull();

            //starting with left Node
            var leftNode = ReadNext(priority - 1);

            //building the syntax tree
            while (true)
            {
                var hasNewLines = _flow.SkipNewLines();
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
                
                if (opToken.Type == TokType.ArrOBr )
                {
                    //We can use array slicing, only if there were no new lines before.
                    //there is problem of choose between anonymous array init and array slice 
                    //otherwise
                    if (_flow.IsPrevious(TokType.NewLine))
                    {
                        return leftNode;
                    }

                    leftNode = ReadArraySliceNode(leftNode);
                }
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
                    leftNode = SyntaxNodeFactory.AnonymFun(leftNode, body);
                }
                
                else
                {
                    _flow.MoveNext();

                    var rightNode = ReadNext(priority - 1);
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

        private ISyntaxNode ReadArraySliceNode(ISyntaxNode arrayNode)
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
                    
                return SyntaxNodeFactory.OperatorFun(
                    CoreFunNames.GetElementName, 
                    new[] {arrayNode, index},openBraket.Start, 
                    _flow.Position);
            }
            
            index = index ?? SyntaxNodeFactory.Constant(0, VarType.Int32, Interval.New(openBraket.Start, colon.Finish));
            
            var end = ReadExpressionOrNull()?? 
                      SyntaxNodeFactory.Constant(int.MaxValue, VarType.Int32, Interval.New(colon.Finish, _flow.Position));
            
            if (!_flow.MoveIf(TokType.Colon, out _))
            {
                if(!_flow.MoveIf(TokType.ArrCBr))
                    throw ErrorFactory.ArraySliceCbrMissed(openBraket,_flow.Current, false);
                return SyntaxNodeFactory.OperatorFun(CoreFunNames.SliceName, new[]
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
                return SyntaxNodeFactory.OperatorFun(CoreFunNames.SliceName, new[] {
                    arrayNode, index, end
                },openBraket.Start, _flow.Position);
            
            return SyntaxNodeFactory.OperatorFun(CoreFunNames.SliceName, new[] {
                arrayNode, index, end, step
            },openBraket.Start, _flow.Position);
        }

        #region  read concreete
        bool TryReadNodeList(out IList<ISyntaxNode> read)
        {
            read = new List<ISyntaxNode>();
            do
            {
                var exp = ReadExpressionOrNull();
                if (exp != null)
                    read.Add(exp);
                else if (read.Count > 0)
                    return false;
                else
                    break;
            } while (_flow.MoveIf(TokType.Sep, out _));
            return true;
        }
        IList<ISyntaxNode> ReadNodeList()
        {
            var list = new List<ISyntaxNode>();
            int start = _flow.Current.Start;
            do
            {
                var exp = ReadExpressionOrNull();
                if (exp != null)
                    list.Add(exp);
                else if (list.Count > 0)
                    throw ErrorFactory.ExpressionListMissed(start, _flow.Position, list);
                else
                    break;
            } while (_flow.MoveIf(TokType.Sep, out _));
            return list;
        }

        private ISyntaxNode ReadInitializeArray()
        {
                
            var startTokenNum = _flow.CurrentTokenPosition;
            var openBracket = _flow.MoveIfOrThrow(TokType.ArrOBr);
            
            if (!TryReadNodeList(out var list))
            {
                throw ErrorFactory.ArrayInitializeByListError(startTokenNum, _flow);
            }
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
                    return SyntaxNodeFactory.ProcArrayInit(
                        @from: list[0],
                        to:  secondArg,
                        step: thirdArg,
                        start: openBracket.Start, 
                        end:closeBracket.Finish);
                }
                else
                {
                    if (!_flow.MoveIf(TokType.ArrCBr,out var closeBracket))
                        throw ErrorFactory.ArrayIntervalInitializeCbrMissed(openBracket, _flow.Current, false);
                    return SyntaxNodeFactory.ProcArrayInit(
                        @from: list[0], 
                        to: secondArg,
                        start: openBracket.Start, 
                        end:closeBracket.Finish);
                }
            }
            if (!_flow.MoveIf(TokType.ArrCBr,out var closeBr))
                throw ErrorFactory.ArrayInitializeByListError(startTokenNum, _flow);
            return SyntaxNodeFactory.Array(list.ToArray(), openBracket.Start, closeBr.Finish);
        }
        private ISyntaxNode ReadBrackedListOrNull()
        {
            int start = _flow.Current.Start;
            int obrId = _flow.CurrentTokenPosition;
            _flow.MoveNext();
            var nodeList = ReadNodeList();
            if (nodeList.Count == 0)
                throw ErrorFactory.BracketExpressionMissed(start, _flow.Position, nodeList);
            if (!_flow.MoveIf(TokType.Cbr, out var cbr))
                throw ErrorFactory.BracketExpressionListError(obrId, _flow);
            var interval = new Interval(start, cbr.Finish);
            if (nodeList.Count == 1)
            {
                nodeList[0].Interval = interval;
                nodeList[0].IsInBrackets = true;
                return nodeList[0];
            }
            else
                return SyntaxNodeFactory.ListOf(nodeList.ToArray(), interval, true);
        }

        private ISyntaxNode ReadIfThenElse()
        {
            int ifElseStart = _flow.Position;
            var ifThenNodes = new List<IfCaseSyntaxNode>();
            do
            {
                int conditionStart = _flow.Current.Start;

                var hasNewLineBefore =_flow.IsPrevious(TokType.NewLine);
                
                //if
                if(!_flow.MoveIf(TokType.If))
                    throw ErrorFactory.IfKeywordIsMissing(ifElseStart, _flow.Position);
                if(ifThenNodes.Any() && !hasNewLineBefore)
                    throw ErrorFactory.NewLineMissedBeforeRepeatedIf(_flow.Previous.Interval);

                //(condition)
                if (!_flow.MoveIf(TokType.Obr))
                {
                    var failedExpr = ReadExpressionOrNull();
                    if(failedExpr!=null)
                        throw ErrorFactory.IfConditionIsNotInBrackets(failedExpr.Interval.Start,failedExpr.Interval.Finish);
                    else    
                        throw ErrorFactory.IfConditionIsNotInBrackets(ifElseStart, _flow.Position);
                }
                var condition =  ReadExpressionOrNull();
                if (condition == null)
                    throw ErrorFactory.ConditionIsMissing(conditionStart, _flow.Position);
                if(!_flow.MoveIf(TokType.Cbr))
                    throw ErrorFactory.IfConditionIsNotInBrackets(ifElseStart, _flow.Position);
                
                //then
                var thenResult = ReadExpressionOrNull();
                if (thenResult == null)
                    throw ErrorFactory.ThenExpressionIsMissing(conditionStart, _flow.Position);
                
                ifThenNodes.Add(SyntaxNodeFactory.IfThen(condition, thenResult, 
                    start: conditionStart, 
                    end: _flow.Position));
            } while (!_flow.IsCurrent(TokType.Else));
            
            if(!_flow.MoveIf(TokType.Else))
                throw ErrorFactory.ElseKeywordIsMissing(ifElseStart, _flow.Position);

            var elseResult = ReadExpressionOrNull();
            if (elseResult == null)
                throw ErrorFactory.ElseExpressionIsMissing(ifElseStart, _flow.Position);

            return SyntaxNodeFactory.IfElse(ifThenNodes, elseResult, ifElseStart, _flow.Position);
        }

        private ISyntaxNode ReadFunctionCall(Tok head, ISyntaxNode pipedVal = null)
        {
            var obrId = _flow.CurrentTokenPosition;
            var start = pipedVal?.Interval.Start ?? head.Start;
            if(!_flow.MoveIf(TokType.Obr))
                throw ErrorFactory.FunctionCallObrMissed(
                    start, head.Value, _flow.Position, pipedVal);

            if (!TryReadNodeList(out var arguments) 
                || !_flow.MoveIf(TokType.Cbr, out var cbr))
                throw ErrorFactory.FunctionArgumentError(head.Value, obrId, _flow);
            
            if(pipedVal!=null)
                arguments.Insert(0,pipedVal);
            
            return SyntaxNodeFactory.FunCall(head.Value, arguments.ToArray(), start, cbr.Finish);
        }
        #endregion
    }
}
