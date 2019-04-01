using System.Collections.Generic;
using System.Linq;
using Funny.BuiltInFunctions;
using Funny.Interpritation.Nodes;
using Funny.Tokenization;
using Funny.Types;

namespace Funny.Parsing
{
    public class LexNodeReader
    {
        private readonly TokenFlow _flow;

        static LexNodeReader()
        {
            var priorities = new List<TokType[]>();
            priorities.Add(new []{TokType.AnonymFun,TokType.ArrOBr});
            
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
                
                });
            
            priorities.Add(new[]
            {
                TokType.BitOr,  
                TokType.PipeForward,
            });
            
            for (byte i = 0; i < priorities.Count; i++)
            {
                foreach (var tokType in priorities[i])
                    Priorities.Add(tokType, i);
            }

            maxPriority = priorities.Count - 1;
        }

        private readonly static int maxPriority;

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
            => ReadNext(maxPriority);

        //ReadZeroPriority operation (num, -num, id, fun, if, (...))
        private LexNode ReadAtomicOrNull()
        {
            _flow.SkipNewLines();
            //-num turns to (-1 * num)
            if (_flow.IsCurrent(TokType.Minus))
            {
                if (_flow.IsPrevious(TokType.Minus))
                    throw new FunParseException("minus duplicates");

                _flow.MoveNext();
                var nextNode = ReadAtomicOrNull();
                if (nextNode == null)
                    throw new FunParseException("minus without next val");
                return LexNode.Fun(CoreFunNames.Multiply,new[]{LexNode.Num("-1"), nextNode});
            }

            if (_flow.MoveIf(TokType.Not, out _))
            {
                var node = ReadNext(5);
                if(node==null)
                    throw  new FunParseException("expected expression after 'not'");
                return LexNode.Fun(CoreFunNames.Not, new []{node});
            }
            if (_flow.MoveIf(TokType.True, out var trueTok))
                return LexNode.Num(trueTok.Value);
            if (_flow.MoveIf(TokType.False, out var falseTok))
                return LexNode.Num(falseTok.Value);
            if (_flow.MoveIf(TokType.Number, out var val))
                return LexNode.Num(val.Value);
            if (_flow.MoveIf(TokType.Text, out var txt))
                return LexNode.Text(txt.Value);
            if (_flow.MoveIf(TokType.Id, out var headToken))
            {
                if (_flow.IsCurrent(TokType.Obr))
                    return ReadFunctionCall(headToken.Value);
                
                if (_flow.IsCurrent(TokType.Colon))
                {
                    _flow.MoveNext();
                    var type = _flow.ReadVarType();
                    return LexNode.Argument(headToken.Value, type);
                }
                else
                    return LexNode.Var(headToken.Value);
            }

            if (_flow.IsCurrent(TokType.Obr))
                return ReadBrackedList();
            if (_flow.IsCurrent(TokType.If))
                return ReadIfThenElse();
            if (_flow.IsCurrent(TokType.ArrOBr))
                return ReadInitializeArray();
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
                
                var currentOp = _flow.Current.Type;
                //if current token is not an operation
                //than expression is done
                //example:
                // 1*2 \r{return expression} y=...
                if (!Priorities.TryGetValue(currentOp, out var opPriority))
                    return leftNode;
                
                //if op has higher priority us
                //than expression is done
                // example:
                //2*3{stops here}-1
                if (opPriority > priority)
                    return leftNode;
                
                if (leftNode == null)
                    throw new FunParseException($"{currentOp} without left arg");
                
                if (currentOp == TokType.ArrOBr)
                {
                    leftNode = ReadArraySliceNode(leftNode);
                }
                else if (currentOp == TokType.PipeForward)
                {
                    _flow.MoveNext();
                    var id = _flow.MoveIfOrThrow(TokType.Id,"function name expected");
                    leftNode =  ReadFunctionCall(id.Value, pipedVal: leftNode);       
                }
                else if (currentOp == TokType.AnonymFun)
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
                        throw new FunParseException($"{currentOp} without right arg");
                    
                    //building the tree from the left                    

                    if (OperatorFunNames.ContainsKey(currentOp))
                        leftNode = LexNode.Fun(OperatorFunNames[currentOp],new[]{leftNode, rightNode});
                    else
                        throw new FunParseException("Unknown operator \""+ currentOp+"\"");
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
            _flow.MoveNext();
            var index = ReadExpressionOrNull();
            
            if (!_flow.MoveIf(TokType.Colon, out _))
            {
                if (index == null)
                    throw new FunParseException("Array index expected");
                
                _flow.MoveIfOrThrow(TokType.ArrCBr);
                return LexNode.Fun(CoreFunNames.GetElementName, new[] {arrayNode, index});
            }
            
            index = index ?? LexNode.Num("0");
            var end = ReadExpressionOrNull()?? LexNode.Num(int.MaxValue.ToString());
            
            if (!_flow.MoveIf(TokType.Colon, out _))
            {
                _flow.MoveIfOrThrow(TokType.ArrCBr);
                return LexNode.Fun(CoreFunNames.SliceName, new[]
                {
                    arrayNode, 
                    index, 
                    end
                });
            }
            
            var step = ReadExpressionOrNull();
            _flow.MoveIfOrThrow(TokType.ArrCBr);
            if(step==null)
                return LexNode.Fun(CoreFunNames.SliceName, new[]
                {
                    arrayNode, index, end
                });
            
            return LexNode.Fun(CoreFunNames.SliceName, new[]
            {
                arrayNode, index, end, step
            });
        }


        #region  read concreete
        IList<LexNode> ReadNodeList()
        {            
            var list = new List<LexNode>();
            do
            {
                var exp = ReadExpressionOrNull();
                if (exp != null)
                    list.Add(exp);
                else if (list.Count > 0)
                    throw new FunParseException("Expression expected");
                else
                    break;
            } while (_flow.MoveIf(TokType.Sep, out _));
            return list;
        }

        private LexNode ReadInitializeArray()
        {
            _flow.MoveIfOrThrow(TokType.ArrOBr);
            var list = ReadNodeList();
            if (list.Count == 1 && _flow.MoveIf(TokType.TwoDots, out _))
            {
                var secondArg = ReadExpressionOrNull();
                if (secondArg == null)
                    throw new FunParseException("Expression expected, but was " + _flow.Current);
                if (_flow.MoveIf(TokType.TwoDots, out _))
                {
                    var thirdArg = ReadExpressionOrNull();
                    if (thirdArg == null)
                        throw new FunParseException("Expression expected, but was " + _flow.Current);
                    _flow.MoveIfOrThrow(TokType.ArrCBr);
                    return LexNode.ProcArrayInit(
                        start: list[0],
                        step: secondArg,
                        end: thirdArg);
                }
                else
                {
                    _flow.MoveIfOrThrow(TokType.ArrCBr);
                    return LexNode.ProcArrayInit(
                        start: list[0], 
                        end: secondArg);
                }
            }
            _flow.MoveIfOrThrow(TokType.ArrCBr);
            return LexNode.Array(list.ToArray());
        }
        private LexNode ReadBrackedList()
        {
            _flow.MoveNext();
            var nodeList = ReadNodeList();
            if (nodeList.Count==0)
                throw new FunParseException("No expr. \"" + _flow.Current + "\" instead");
            _flow.MoveIfOrThrow(TokType.Cbr);
            if (nodeList.Count == 1)
                return nodeList[0];
            else
                return LexNode.ListOf(nodeList.ToArray());
        }

        private LexNode ReadIfThenElse()
        {
            var ifThenNodes = new List<LexNode>();
            do
            {
                _flow.MoveIfOrThrow(TokType.If);
                var condition = ReadExpressionOrNull();
                if (condition == null)
                    throw new FunParseException("condition expression is missing");
                _flow.MoveIfOrThrow(TokType.Then);
                var thenResult = ReadExpressionOrNull();
                if (thenResult == null)
                    throw new FunParseException("then expression is missing");
                ifThenNodes.Add(LexNode.IfThen(condition, thenResult));
            } while (!_flow.IsCurrent(TokType.Else));

            _flow.MoveIfOrThrow(TokType.Else);
            var elseResult = ReadExpressionOrNull();
            if (elseResult == null)
                throw new FunParseException("else expression is missing");
            return LexNode.IfElse(ifThenNodes, elseResult);
        }

        private LexNode ReadFunctionCall(string name, LexNode pipedVal=null)
        {
            bool hasObr =_flow.MoveIf(TokType.Obr, out _);

            if (!hasObr)
            {
                if (pipedVal != null) 
                    return LexNode.Fun(name, new[] {pipedVal});
                else
                    throw new FunParseException("'(' expected, but was " + _flow.Current);
            }
            var arguments = ReadNodeList();
            if(pipedVal!=null)
                arguments.Insert(0,pipedVal);
            _flow.MoveIfOrThrow(TokType.Cbr, "\",\" or \")\" expected");
            return LexNode.Fun(name, arguments.ToArray());
        }
        #endregion
    }
}
