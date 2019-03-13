using System.Collections.Generic;
using System.Linq;
using Funny.Tokenization;

namespace Funny.Parsing
{
    public class LexNodeReader
    {
        private readonly TokenFlow _flow;

        private static readonly Dictionary<TokType, byte> Priorities
            = new Dictionary<TokType, byte>()
            {
                {TokType.Equal, 1},
                {TokType.NotEqual, 1},
                {TokType.More, 1},
                {TokType.Less, 1},
                {TokType.MoreOrEqual, 1},
                {TokType.LessOrEqual, 1},
                {TokType.Pow, 2},
                {TokType.Mult, 3},
                {TokType.Div, 3},
                {TokType.Rema, 3},
                {TokType.And, 3},
                {TokType.Plus, 4},
                {TokType.Minus, 4},
                {TokType.Or, 4},
                {TokType.Xor, 4}
            };

        public LexNodeReader(TokenFlow flow)
        {
            this._flow = flow;
        }

        public LexNode ReadExpressionOrNull()
            => ReadNext(4);

        //ReadZeroPriority operation (num, -num, id, fun, if, (...))
        private LexNode ReadAtomicOrNull()
        {
            _flow.SkipNewLines();
            //-num turns to (-1 * num)
            if (_flow.IsCurrent(TokType.Minus))
            {
                if (_flow.IsPrevious(TokType.Minus))
                    throw new ParseException("minus duplicates");

                _flow.MoveNext();
                var nextNode = ReadAtomicOrNull();
                if (nextNode == null)
                    throw new ParseException("minus without next val");
                return LexNode.Op(LexNodeType.Mult, LexNode.Num("-1"), nextNode);
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
                    return ReadFunctionCall(headToken);
                else
                    return LexNode.Var(headToken.Value);
            }

            if (_flow.IsCurrent(TokType.Obr))
                return ReadBrackedExpression();
            if (_flow.IsCurrent(TokType.If))
                return ReadIfThenElse();
            if (_flow.IsCurrent(TokType.ArrOBr))
                return ReadInitializeArray();
            return null;
        }

      

        LexNode ReadNext(int priority)
        {
            //HELL IS HERE

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

                _flow.MoveNext();
                var rightNode = ReadNext(priority - 1);
                if (rightNode == null)
                    throw new ParseException($"{currentOp} without right arg");
                //building the tree from the left
                leftNode = LexNode.Op(currentOp, leftNode, rightNode);
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


        #region  read concreete
        private LexNode ReadInitializeArray()
        {
            _flow.MoveIfOrThrow(TokType.ArrOBr);
            
            var arguments = new List<LexNode>();
            while (true)
            {
                if (_flow.MoveIf(TokType.ArrCBr, out _))
                    return LexNode.Array(arguments.ToArray());

                if (arguments.Any())
                    _flow.MoveIfOrThrow(TokType.Sep, @""","" or ""]"" expected");

                var arg = ReadExpressionOrNull();
                arguments.Add(arg);
            }
        }
        private LexNode ReadBrackedExpression()
        {
            _flow.MoveNext();

            var node = ReadExpressionOrNull();

            if (node == null)
                throw new ParseException("No expr. \"" + _flow.Current + "\" instead");

            _flow.MoveIfOrThrow(TokType.Cbr);
            return node;
        }

        private LexNode ReadIfThenElse()
        {
            var ifThenNodes = new List<LexNode>();
            do
            {
                _flow.MoveIfOrThrow(TokType.If);
                var condition = ReadExpressionOrNull();
                if (condition == null)
                    throw new ParseException("condition expression is missing");
                _flow.MoveIfOrThrow(TokType.Then);
                var thenResult = ReadExpressionOrNull();
                if (thenResult == null)
                    throw new ParseException("then expression is missing");
                ifThenNodes.Add(LexNode.IfThen(condition, thenResult));
            } while (!_flow.IsCurrent(TokType.Else));

            _flow.MoveIfOrThrow(TokType.Else);
            var elseResult = ReadExpressionOrNull();
            if (elseResult == null)
                throw new ParseException("else expression is missing");
            return LexNode.IfElse(ifThenNodes, elseResult);
        }

        private LexNode ReadFunctionCall(Tok id)
        {
            _flow.MoveNext(); //skip Obr
            var arguments = new List<LexNode>();
            while (true)
            {
                if (_flow.MoveIf(TokType.Cbr, out _))
                    return LexNode.Fun(id.Value, arguments.ToArray());

                if (arguments.Any())
                    _flow.MoveIfOrThrow(TokType.Sep, "\",\" or \")\" expected");

                var arg = ReadExpressionOrNull();
                arguments.Add(arg);
            }
        }

        #endregion

   

    }
}
