using System.Collections.Generic;
using Funny.Tokenization;

namespace Funny.Parsing
{
    public class Parser
    {
        private readonly TokenFlow _flow;

        public Parser(TokenFlow flow)
        {
            this._flow = flow;
        }
        public List<LexEquatation> Parse()
        {
            var res = new List<LexEquatation>();

            while (true)
            {
                _flow.SkipNewLines();
                
                if (_flow.IsDone || _flow.IsCurrent(TokType.Eof))
                    return res;

                
                if(!_flow.IsCurrent(TokType.Id))
                    throw new ParseException("Starts with no id");

                var id = _flow.Current.Value;

                _flow.MoveNext();
                _flow.SkipNewLines();

                if(!_flow.IsCurrent(TokType.Equal))
                    throw new ParseException("has no =");

                 _flow.MoveNext();
                 _flow.SkipNewLines();

                var exNode = ReadExpression();
                res.Add(new LexEquatation(id, exNode));
            }

            return res;
        }
        //Чтение атомарного значения (число, id или выражение в скобках)
        LexNode ReadAtomicOrNull()
        {
            _flow.SkipNewLines();
            //Если минус то читаем дальше и заворачиваем в умножение на 1
            if (_flow.IsCurrent(TokType.Minus))
            {
                if(_flow.IsPrevious(TokType.Minus))
                    throw new ParseException("minus duplicates");
                    
                _flow.MoveNext();
                var nextNode = ReadAtomicOrNull();
                if(nextNode==null)
                    throw new ParseException("minus without next val");
                var valueNegativeOne = new LexNode(Tok.New(TokType.Uint, "-1", _flow.CurrentPos));
                return new LexNode(Tok.New(TokType.Mult, _flow.CurrentPos), 
                    valueNegativeOne,nextNode);
            }
            if (_flow.IsCurrent(TokType.Uint) || _flow.IsCurrent(TokType.Id))
            {
                var ans = new LexNode(_flow.Current);
                _flow.MoveNext();
                return ans;
            }
            
            if (_flow.IsCurrent(TokType.Obr))
                return  ReadBrackedExpression();
            return null;
        }
        //Чтение высокоуровневой операции (атомарное значение или их умножение, степень, деление)
        LexNode ReadHiPriorityVal()
        {
            var node = ReadAtomicOrNull();
            if (node == null)
                return null;
            
            _flow.SkipNewLines();
            if (_flow.IsCurrent(TokType.Mult)
                || _flow.IsCurrent(TokType.Div)
                || _flow.IsCurrent(TokType.Pow))
            {
                var op = _flow.Current;
                _flow.MoveNext();
                var nextVal = ReadHiPriorityVal();
                if (nextVal == null)
                    throw new ParseException($"{_flow.Current.Type} without \'b\' arg");
                return new LexNode(op, node, nextVal);
            }
            
            return node;
        }
        
        LexNode ReadExpression()
        {
            var node = ReadHiPriorityVal();
            
            if (  _flow.IsDone 
                  || _flow.IsCurrent(TokType.Eof) 
                  || _flow.IsCurrent(TokType.Cbr))
                return node;

            //Check for next equatation
            var hasNewLine = _flow.SkipNewLines();
            if (hasNewLine && _flow.IsCurrent(TokType.Abc))
                    return node;
            
            
            if (  _flow.IsCurrent(TokType.Plus)
                ||_flow.IsCurrent(TokType.Minus))
            {
                var op = _flow.Current;
                if (node == null)
                    throw new ParseException($"{op.Type} without \'a\' arg");
                _flow.MoveNext();
                var nextVal = ReadExpression();
                if (nextVal == null)
                    throw new ParseException($"{op.Type} without \'b\' arg");
                node = new LexNode(op, node, nextVal);
            }
            return node;
        }

        
        private LexNode ReadBrackedExpression()
        {
            _flow.MoveNext();
            
            var node = ReadExpression();

            if(_flow.IsDone)
                throw new ParseException("No cbr. End.");

            if (node == null)
                throw new ParseException("No expr. \"" + _flow.Current + "\" instead");

            if (!_flow.IsCurrent(TokType.Cbr))
                throw new ParseException("No cbr");
            _flow.MoveNext();
            return node;
        }
    }
}