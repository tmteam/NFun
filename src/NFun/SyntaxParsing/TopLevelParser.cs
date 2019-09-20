using System.Collections.Generic;
using System.Linq;
using NFun.ParseErrors;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.SyntaxParsing
{
    
    public class TopLevelParser
    {
        private readonly TokFlow _flow;
        public const string AnonymousEquationId = "out";

        public static SyntaxTree Parse(TokFlow flow)
            =>new TopLevelParser(flow).InternalParse(flow);
        
        private readonly SyntaxNodeReader _reader ;
        private readonly List<ISyntaxNode> _nodes = new List<ISyntaxNode>();
        private readonly List<string> _equationNames = new List<string>();
        
        //current reader states
        private bool _hasAnonymousEquation = false;
        private bool _hasFuctions = false;
        private bool _startOfTheLine = false;
        private int _exprStartPosition = 0;
        private VarAttribute[] _attributes;
        public TopLevelParser(TokFlow flow)
        {
            this._flow = flow;
            _reader = new SyntaxNodeReader(flow);
        }
        public SyntaxTree InternalParse(TokFlow flow)
        {
            while (true)
            {
                flow.SkipNewLines();
                if (flow.IsDoneOrEof()) break;

                _attributes          = flow.ReadAttributes();
                _startOfTheLine       = flow.IsStartOfTheLine();
                _exprStartPosition    = flow.Current.Start;

                var e = _reader.ReadExpressionOrNull()
                        ?? throw ErrorFactory.UnknownValueAtStartOfExpression(_exprStartPosition, flow.Current);
                
                
                if (e is TypedVarDefSyntaxNode typed)
                {
                    if (flow.IsCurrent(TokType.Def))
                        ReadOutputVariable(typed, typed.Id);
                    else
                        ReadInputVariableSpecification(typed);
                }
                else if (flow.IsCurrent(TokType.Def) || flow.IsCurrent(TokType.Colon))
                {
                    if (e is VariableSyntaxNode variable)
                        ReadOutputVariable(variable, variable.Id);
                    else if (e is FunCallSyntaxNode fun && !fun.IsOperator)
                        ReadUserFunction(fun);
                    else
                        throw ErrorFactory.ExpressionBeforeTheDefenition(_exprStartPosition, e, flow.Current);
                }
                else
                    ReadAnonymousEquation(e);
            }
            return new SyntaxTree(_nodes.ToArray());
        }

        private void ReadInputVariableSpecification(TypedVarDefSyntaxNode typed) 
            => _nodes.Add(new VarDefenitionSyntaxNode(typed, _attributes));

        private void ReadAnonymousEquation(ISyntaxNode e)
        {
            if (_equationNames.Any())
            {
                if (_startOfTheLine && _hasAnonymousEquation)
                    throw ErrorFactory.OnlyOneAnonymousExpressionAllowed(_exprStartPosition, e, _flow.Current);
                else
                    throw ErrorFactory.UnexpectedExpression(e);
            }

            if (!_startOfTheLine)
                throw ErrorFactory.AnonymousExpressionHasToStartFromNewLine(_exprStartPosition, e, _flow.Current);

            //todo start
            //anonymous
            var equation = new EquationSyntaxNode(AnonymousEquationId, 0, e, _attributes);
            _hasAnonymousEquation = true;
            _equationNames.Add(equation.Id);
            _nodes.Add(equation);
        }

        private void ReadUserFunction( FunCallSyntaxNode fun)
        {
            //Todo Make operator fun as separate node type

            if (!_startOfTheLine)
                throw ErrorFactory.FunctionDefenitionHasToStartFromNewLine(_exprStartPosition, fun, _flow.Current);
            if (_attributes.Any())
                throw ErrorFactory.AttributeOnFunction(_exprStartPosition, fun);
            
            var id = fun.Id;
            if (fun.IsInBrackets)
                throw ErrorFactory.UnexpectedBracketsOnFunDefenition( fun, _exprStartPosition,_flow.Previous.Finish);

            var arguments = new List<TypedVarDefSyntaxNode>();
            foreach (var headNodeChild in fun.Args)
            {
                if (headNodeChild is TypedVarDefSyntaxNode varDef)
                    arguments.Add(varDef);
                else if(headNodeChild is VariableSyntaxNode varSyntax)
                    arguments.Add(new TypedVarDefSyntaxNode(varSyntax.Id, headNodeChild.OutputType, headNodeChild.Interval));
                else    
                    throw ErrorFactory.WrongFunctionArgumentDefenition(_exprStartPosition, fun, headNodeChild, _flow.Current);
              
                if(headNodeChild.IsInBrackets)    
                    throw ErrorFactory.FunctionArgumentInBracketDefenition(_exprStartPosition, fun, headNodeChild, _flow.Current);
            }

            var outputType = VarType.Empty;
            if (_flow.MoveIf(TokType.Colon, out _))
                outputType = _flow.ReadVarType();
            
            _flow.SkipNewLines();
            if (!_flow.MoveIf(TokType.Def, out var def))
                throw ErrorFactory.FunDefTokenIsMissed(id, arguments, _flow.Current);  

            var expression = _reader.ReadExpressionOrNull();
            if (expression == null)
            {

                int finish = _flow.Peek?.Finish ?? _flow.Position;
                    
                throw ErrorFactory.FunExpressionIsMissed(id, arguments, 
                    new Interval(def.Start, finish));
            }

            var functionNode =  new UserFunctionDefenitionSyntaxNode(arguments, fun, expression, outputType );
            
            _nodes.Add(functionNode);
            _hasFuctions = true;
        }

        private void ReadOutputVariable(ISyntaxNode equationHeader, string id)
        {
            if (_hasAnonymousEquation)
                throw ErrorFactory.UnexpectedExpression(_nodes.OfType<EquationSyntaxNode>().Single());
            if (!_startOfTheLine)
                throw ErrorFactory.DefenitionHasToStartFromNewLine(_exprStartPosition, equationHeader, _flow.Current);
            
            _flow.MoveNext();
            var equation = ReadEquation(_flow, _reader, id, _attributes);

            if (equationHeader is TypedVarDefSyntaxNode typed)
            {
                equation.TypeSpecificationOrNull = typed;
                equation.OutputType = typed.VarType;
            }

            _nodes.Add(equation);
            _equationNames.Add(equation.Id);
        }

        private static EquationSyntaxNode ReadEquation(TokFlow flow, SyntaxNodeReader reader, string id, VarAttribute[] attributes)
        {
            flow.SkipNewLines();
            var start = flow.Position;
            var exNode = reader.ReadExpressionOrNull();
            if (exNode == null)
                throw ErrorFactory.VarExpressionIsMissed(start, id, flow.Current);
            return new EquationSyntaxNode(id, start, exNode, attributes);
        }
    }
}
