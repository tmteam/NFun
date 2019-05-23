using System;
using NFun.ParseErrors;
using NFun.Tokenization;

namespace NFun.SyntaxParsing
{
    public static class TokenParsingHelper
    {
        public static ISyntaxNode ReadExpressionOrNull(this TokFlow flow)
        { 
            return new SyntaxNodeReader(flow).ReadExpressionOrNull();
        }
        public static ISyntaxNode TryReadExpressionAndReturnBack(this TokFlow flow)
        {
            int lastFlowPosition = flow.CurrentTokenPosition;
            try
            {
                return new SyntaxNodeReader(flow).ReadExpressionOrNull();
            }
            catch (FunParseException){}
            finally
            {
                flow.Move(lastFlowPosition);
            }
            throw new InvalidOperationException();
        }
    }
}