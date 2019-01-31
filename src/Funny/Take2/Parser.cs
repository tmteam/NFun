using System.Collections.Generic;

namespace Funny.Take2
{
    public class Parser
    {
        public List<LexEquatation> Parse(TokenFlow flow)
        {
            var res = new List<LexEquatation>();

            while (true)
            {
                if (flow.IsDone || flow.IsCurrent(TokType.Eof))
                    return res;
               
                if(!flow.IsCurrent(TokType.Id))
                    throw new ParseException("Starts with no id");

                var id = flow.Current.Value;

                flow.MoveNext();
                if(!flow.IsCurrent(TokType.Equal))
                    throw new ParseException("has no =");
                flow.MoveNext();
               
                var exNode = ReadExpression(flow);
                res.Add(new LexEquatation(id, exNode));
            }

            return res;
        }
        //Чтение атомарного значения (число, id или выражение в скобках)
        LexNode ReadAtomicOrNull(TokenFlow flow)
        {
            //Если минус то читаем дальше и заворачиваем в умножение на 1
            if (flow.IsCurrent(TokType.Minus))
            {
                flow.MoveNext();
                var nextNode = ReadAtomicOrNull(flow);
                if(nextNode==null)
                    throw new ParseException("minus without next val");
                var valueNegativeOne = new LexNode(Tok.New(TokType.Uint, "-1", flow.CurrentPos));
                return new LexNode(Tok.New(TokType.Mult, flow.CurrentPos), 
                    valueNegativeOne,nextNode);
            }
            if (flow.IsCurrent(TokType.Uint) || flow.IsCurrent(TokType.Id))
            {
                var ans = new LexNode(flow.Current);
                flow.MoveNext();
                return ans;
            }
            
            if (flow.IsCurrent(TokType.Obr))
                return  ReadBrackedExpression(flow);
            return null;
        }
        //Чтение высокоуровневой операции (атомарное значение или их умножение, степень, деление)
        LexNode ReadHiPriorityVal(TokenFlow flow)
        {
            var node = ReadAtomicOrNull(flow);
            if (node == null)
                return null;

            if (flow.IsCurrent(TokType.Mult)
                || flow.IsCurrent(TokType.Div)
                || flow.IsCurrent(TokType.Pow))
            {
                var op = flow.Current;
                flow.MoveNext();
                var nextVal = ReadHiPriorityVal(flow);
                if (nextVal == null)
                    throw new ParseException($"{flow.Current.Type} without \'b\' arg");
                return new LexNode(op, node, nextVal);
            }
            
            return node;
        }
        
        LexNode ReadExpression(TokenFlow flow)
        {
            var node = ReadHiPriorityVal(flow);
            
            if (  flow.IsDone 
                  || flow.IsCurrent(TokType.Eof) 
                  || flow.IsCurrent(TokType.Cbr))
                return node;
          
            var hasNewLine = SkipNewLines(flow);

            if (hasNewLine && flow.IsCurrent(TokType.Abc))
                    return node;
            
            
            if (  flow.IsCurrent(TokType.Plus)
                ||flow.IsCurrent(TokType.Minus))
            {
                var op = flow.Current;
                if (node == null)
                    throw new ParseException($"{op.Type} without \'a\' arg");
                flow.MoveNext();
                var nextVal = ReadExpression(flow);
                if (nextVal == null)
                    throw new ParseException($"{op.Type} without \'b\' arg");
                node = new LexNode(op, node, nextVal);
            }
            return node;
        }

        private static bool SkipNewLines(TokenFlow flow)
        {
            bool hasNewLine = false;
            while (flow.IsCurrent(TokType.NewLine))
            {
                hasNewLine = true;
                flow.MoveNext();
            }

            return hasNewLine;
        }


        private LexNode ReadBrackedExpression(TokenFlow flow)
        {
            flow.MoveNext();
            
            var node = ReadExpression(flow);

            if(flow.IsDone)
                throw new ParseException("No cbr. End.");

            if (node == null)
                throw new ParseException("No expr. \"" + flow.Current + "\" instead");

            if (!flow.IsCurrent(TokType.Cbr))
                throw new ParseException("No cbr");
            flow.MoveNext();
            return node;
        }
    }
}