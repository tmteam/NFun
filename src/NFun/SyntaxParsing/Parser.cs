using System.Collections.Generic;
using System.Linq;
using NFun.ParseErrors;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.SyntaxParsing
{
    public class Parser
    {
        private readonly TokFlow _flow;
        public const string AnonymousEquationId = "out";

        public static SyntaxTree Parse(TokFlow flow)
            =>new Parser(flow).ParseTree(flow);
        
        private readonly List<ISyntaxNode> _nodes = new List<ISyntaxNode>();
        private readonly List<string> _equationNames = new List<string>();
        
        //current reader state
        private bool _hasAnonymousEquation = false;
        private bool _startOfTheLine = false;
        private int _exprStartPosition = 0;
        private VarAttribute[] _attributes;

        private Parser(TokFlow flow)
        {
            _flow = flow;
        }
        
        private SyntaxTree ParseTree(TokFlow flow)
        {
            while (true)
            {
                flow.SkipNewLines();
                if (flow.IsDoneOrEof()) break;

                _attributes           = flow.ReadAttributes();
                _startOfTheLine       = flow.IsStartOfTheLine();
                _exprStartPosition    = flow.Current.Start;

                var e = SyntaxNodeReader.ReadNodeOrNull(flow)
                        ?? throw ErrorFactory.UnknownValueAtStartOfExpression(_exprStartPosition, flow.Current);
                
                if (e is TypedVarDefSyntaxNode typed)
                {
                    if (flow.IsCurrent(TokType.Def))
                        ReadEquation(typed, typed.Id);
                    else
                        ReadInputVariableSpecification(typed);
                }
                else if (flow.IsCurrent(TokType.Def) || flow.IsCurrent(TokType.Colon))
                {
                    if (e is NamedIdSyntaxNode variable)
                        ReadEquation(variable, variable.Id);
                    //Fun call can be used as fun definition
                    else if (e is FunCallSyntaxNode fun && !fun.IsOperator)
                        ReadUserFunction(fun);
                    else
                        throw ErrorFactory.ExpressionBeforeTheDefinition(_exprStartPosition, e, flow.Current);
                }
                else
                    ReadAnonymousEquation(e);
            }
            return new SyntaxTree(_nodes.ToArray());
        }
        /// <summary>
        /// Read input type specification.
        /// Like i:int
        /// </summary>
        private void ReadInputVariableSpecification(TypedVarDefSyntaxNode typed) 
            => _nodes.Add(new VarDefinitionSyntaxNode(typed, _attributes));
        /// <summary>
        /// Read anonymous equation. Throws if at least one other equation exists
        /// </summary>
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

            //anonymous
            var equation = new EquationSyntaxNode(AnonymousEquationId, _exprStartPosition, e, _attributes);
            _hasAnonymousEquation = true;
            _equationNames.Add(equation.Id);
            _nodes.Add(equation);
        }
        /// <summary>
        /// Read user function
        /// like y(a,b,c:int):real = ...
        /// </summary>
        private void ReadUserFunction( FunCallSyntaxNode fun)
        {
            if (!_startOfTheLine)
                throw ErrorFactory.FunctionDefinitionHasToStartFromNewLine(_exprStartPosition, fun, _flow.Current);
            if (_attributes.Length>0)
                throw ErrorFactory.AttributeOnFunction(fun);
            
            var id = fun.Id;
            if (fun.IsInBrackets)
                throw ErrorFactory.UnexpectedBracketsOnFunDefinition( fun, _exprStartPosition,_flow.Previous.Finish);

            var arguments = new List<TypedVarDefSyntaxNode>();
            foreach (var headNodeChild in fun.Args)
            {
                if (headNodeChild is TypedVarDefSyntaxNode varDef)
                    arguments.Add(varDef);
                else if(headNodeChild is NamedIdSyntaxNode varSyntax)
                    arguments.Add(new TypedVarDefSyntaxNode(varSyntax.Id, headNodeChild.OutputType, headNodeChild.Interval));
                else    
                    throw ErrorFactory.WrongFunctionArgumentDefinition(fun, headNodeChild);
              
                if(headNodeChild.IsInBrackets)    
                    throw ErrorFactory.FunctionArgumentInBracketDefinition(fun, headNodeChild, _flow.Current);
            }

            var outputType = FunnyType.Empty;
            if (_flow.MoveIf(TokType.Colon, out _))
                outputType = _flow.ReadType();
            
            _flow.SkipNewLines();
            if (!_flow.MoveIf(TokType.Def, out var def))
                throw ErrorFactory.FunDefTokenIsMissed(id, arguments, _flow.Current);  

            var expression = SyntaxNodeReader.ReadNodeOrNull(_flow);
            if (expression == null)
            {
                int finish = _flow.Peek?.Finish ?? _flow.Position;
                    
                throw ErrorFactory.FunExpressionIsMissed(id, arguments, 
                    new Interval(def.Start, finish));
            }

            var functionNode =  new UserFunctionDefinitionSyntaxNode(arguments, fun, expression, outputType );
            
            _nodes.Add(functionNode);
        }
        /// <summary>
        /// Read named equation
        /// like: y = 1 + x
        /// </summary>
        private void ReadEquation(ISyntaxNode equationHeader, string id)
        {
            if (_hasAnonymousEquation)
                throw ErrorFactory.UnexpectedExpression(_nodes.OfType<EquationSyntaxNode>().Single());
            if (!_startOfTheLine)
                throw ErrorFactory.DefinitionHasToStartFromNewLine(_exprStartPosition, equationHeader, _flow.Current);
            
            _flow.MoveNext();
            var equation = ReadEquationBody(id);

            if (equationHeader is TypedVarDefSyntaxNode typed)
            {
                equation.TypeSpecificationOrNull = typed;
                equation.OutputType = typed.FunnyType;
            }

            _nodes.Add(equation);
            _equationNames.Add(equation.Id);
        }
        
        private EquationSyntaxNode ReadEquationBody(string id)
        {
            _flow.SkipNewLines();
            var start = _flow.Position;
            var exNode = SyntaxNodeReader.ReadNodeOrNull(_flow);
            if (exNode == null)
                throw ErrorFactory.VarExpressionIsMissed(start, id, _flow.Current);
            return new EquationSyntaxNode(id, start, exNode, _attributes);
        }
    }
}
