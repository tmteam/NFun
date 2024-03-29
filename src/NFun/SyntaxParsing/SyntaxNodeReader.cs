using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Numerics;
using NFun.Functions;
using NFun.Exceptions;
using NFun.ParseErrors;
using NFun.Runtime.Arrays;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tokenization;

namespace NFun.SyntaxParsing;

/// <summary>
/// Reads concrete syntax nodes from token flow
/// </summary>
public static class SyntaxNodeReader {
    private const int MinPriority = 0;
    private const int OperatorBitInversePriority = 1;
    private const int OperatorNotPriority = 10;

    private static readonly int MaxPriority;

    private static readonly Dictionary<TokType, byte> BinaryPriorities = new();

    static SyntaxNodeReader() {
        var priorities = new List<TokType[]>(14) {
            new[] { TokType.ArrOBr, TokType.Dot, TokType.ParenthObr },
            new[] { TokType.Pow },
            new[] { TokType.Mult, TokType.Div, TokType.DivInt, TokType.Rema },
            new[] { TokType.Plus, TokType.Minus },
            new[] { TokType.BitShiftLeft, TokType.BitShiftRight },
            new[] { TokType.BitAnd },
            new[] { TokType.BitXor },
            new[] { TokType.BitOr },
            new[] {
                TokType.More, TokType.Less, TokType.MoreOrEqual,
                TokType.LessOrEqual
            },
            new [] { TokType.In, TokType.Equal, TokType.NotEqual },
            Array.Empty<TokType>(), // gap for unary 'not x'
            new[] { TokType.And },
            new[] { TokType.Xor },
            new[] { TokType.Or }
        };

        for (byte i = 0; i < priorities.Count; i++)
        {
            foreach (var tokType in priorities[i])
                BinaryPriorities.Add(tokType, i);
        }

        MaxPriority = priorities.Count - 1;
    }

    internal static string GetOperatorFunctionName(TokType type) =>
        OperatorFunNames.TryGetValue(type, out var output) ? output : null;

    private static readonly Dictionary<TokType, string> OperatorFunNames = new() {
        { TokType.Plus, CoreFunNames.Add },
        { TokType.Minus, CoreFunNames.Substract },
        { TokType.Mult, CoreFunNames.Multiply },
        { TokType.Div, CoreFunNames.DivideReal },
        { TokType.DivInt, CoreFunNames.DivideInt },
        { TokType.Rema, CoreFunNames.Remainder },
        { TokType.Pow, CoreFunNames.Pow },
        { TokType.And, CoreFunNames.And },
        { TokType.Or, CoreFunNames.Or },
        { TokType.Xor, CoreFunNames.Xor },
        { TokType.BitAnd, CoreFunNames.BitAnd },
        { TokType.BitOr, CoreFunNames.BitOr },
        { TokType.BitXor, CoreFunNames.BitXor },
        { TokType.More, CoreFunNames.More },
        { TokType.MoreOrEqual, CoreFunNames.MoreOrEqual },
        { TokType.Less, CoreFunNames.Less },
        { TokType.LessOrEqual, CoreFunNames.LessOrEqual },
        { TokType.Equal, CoreFunNames.Equal },
        { TokType.NotEqual, CoreFunNames.NotEqual },
        { TokType.BitShiftLeft, CoreFunNames.BitShiftLeft },
        { TokType.BitShiftRight, CoreFunNames.BitShiftRight },
        { TokType.In, CoreFunNames.In }
    };

    private static readonly HashSet<TokType> ComparisonChainOperators = new() {
        TokType.More,
        TokType.MoreOrEqual,
        TokType.Less,
        TokType.LessOrEqual
    };

    public static ISyntaxNode ReadNodeOrNull(TokFlow flow) =>
        ReadNodeOrNull(flow, MaxPriority);

    /// <summary>
    /// Reads node with lowest priority
    /// Equiualent to ReadNodeOrNull(0)
    /// num, -num, id, fun, if, (...)
    /// throws FunParseException if underlying syntax is invalid,
    /// or returns null if underlying syntax cannot be represented as atomic node
    /// (EOF for example)
    /// /// </summary>
    private static ISyntaxNode ReadAtomicNodeOrNull(TokFlow flow) {
        flow.SkipNewLines();

        //-num turns to (-1 * num)
        var start = flow.CurrentTokenFinishPosition;
        if (flow.IsCurrent(TokType.Minus))
        {
            if (flow.IsPrevious(TokType.Minus))
                throw Errors.MinusDuplicates(flow.Previous, flow.Current);
            flow.MoveNext();

            var nextNode = ReadNodeOrNull(flow, MinPriority);
            if (nextNode == null)
                throw Errors.UnaryArgumentIsMissing(flow.Current);

            var interval = new Interval(start, nextNode.Interval.Finish);
            if (nextNode is ConstantSyntaxNode constant)
            {
                switch (constant.Value)
                {
                    case double d:
                        return SyntaxNodeFactory.Constant(-d, constant.OutputType, interval);
                    case ulong u64:
                        return SyntaxNodeFactory.Constant(-(long)u64, constant.OutputType, interval);
                    case long i64:
                        return SyntaxNodeFactory.Constant(-i64, constant.OutputType, interval);
                }
            }
            else if (nextNode is GenericIntSyntaxNode g)
            {
                const ulong i64AbsMinValue = 9223372036854775808;
                switch (g.Value)
                {
                    case ulong u64 when u64 >= i64AbsMinValue:
                    {
                        if (u64 > i64AbsMinValue)
                            throw Errors.NumberOverflow(g.Interval, FunnyType.Int64);
                        return SyntaxNodeFactory.IntGenericConstant(long.MinValue, g.IsHexOrBin, interval);
                    }
                    case ulong u64:
                        return SyntaxNodeFactory.IntGenericConstant(-(long)u64, g.IsHexOrBin, interval);
                    case long i64:
                        return SyntaxNodeFactory.IntGenericConstant(-i64, g.IsHexOrBin, interval);
                }
            }

            return SyntaxNodeFactory.UnarOperatorCall(CoreFunNames.Negate, nextNode, start, nextNode.Interval.Finish);
        }

        if (flow.MoveIf(TokType.BitInverse))
        {
            var node = ReadNodeOrNull(flow, OperatorBitInversePriority);
            if (node == null)
                throw Errors.UnaryArgumentIsMissing(flow.Current);
            return SyntaxNodeFactory.UnarOperatorCall(CoreFunNames.BitInverse, node, start, node.Interval.Finish);
        }

        if (flow.MoveIf(TokType.Not))
        {
            var node = ReadNodeOrNull(flow, OperatorNotPriority);
            if (node == null)
                throw Errors.UnaryArgumentIsMissing(flow.Current);
            return SyntaxNodeFactory.UnarOperatorCall(CoreFunNames.Not, node, start, node.Interval.Finish);
        }

        if (flow.MoveIf(TokType.Rule))
            return ReadFunAnonymousFunction(flow);

        if (flow.MoveIf(TokType.FiObr))
            return ReadStruct(flow);

        if (flow.MoveIf(TokType.True, out var trueTok))
            return SyntaxNodeFactory.Constant(true, FunnyType.Bool, trueTok.Interval);

        if (flow.MoveIf(TokType.False, out var falseTok))
            return SyntaxNodeFactory.Constant(false, FunnyType.Bool, falseTok.Interval);

        if (flow.MoveIf(TokType.HexOrBinaryNumber, out var binVal))
        {
            //0xff, 0b01
            var val = binVal.Value;
            var dimensions = val[1] switch {
                'b' => 2,
                'x' => 16,
                _ => throw new NFunImpossibleException("Hex or bin constant has invalid format: " + val)
            };
            var substr = val.Replace("_", null)[2..];

            if (dimensions == 16)
            {
                if (UInt64.TryParse(substr, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var h16))
                    return SyntaxNodeFactory.HexOrBinIntConstant(h16, binVal.Interval);

                throw Errors.NumberOverflow(binVal.Interval, FunnyType.UInt64);
            }

            if (substr.Length > 64)
                throw Errors.NumberOverflow(binVal.Interval, FunnyType.UInt64);

            return SyntaxNodeFactory.HexOrBinIntConstant(Convert.ToUInt64(substr, 2), binVal.Interval);
        }

        if (flow.MoveIf(TokType.IntNumber, out var intVal))
        {
            //1,2,3
            var decVal = BigInteger.Parse(intVal.Value.Replace("_", String.Empty), CultureInfo.InvariantCulture);
            if (decVal > ulong.MaxValue)
                throw Errors.NumberOverflow(intVal.Interval, FunnyType.UInt64);

            return SyntaxNodeFactory.IntGenericConstant((ulong)decVal, intVal.Interval);
        }

        if (flow.MoveIf(TokType.RealNumber, out var realVal)) //1.0 . Real type cannot be parsed without a type context.
        {
            return
                SyntaxNodeFactory.Constant(realVal.Value.Replace("_", String.Empty), FunnyType.Real, realVal.Interval);
        }

        if (flow.MoveIf(TokType.IpAddress, out var ipVal))
        {
            //192.168.0.1
            var text = ipVal.Value.Replace("_", String.Empty);
            if (!IPAddress.TryParse(text, out var ip))
                throw Errors.InvalidIpAddress(ipVal);
            return SyntaxNodeFactory.IpAddressConstant(ip, ipVal.Interval);
        }

        if (flow.MoveIf(TokType.Text, out var txt))
            return SyntaxNodeFactory.Constant(new TextFunnyArray(txt.Value), FunnyType.Text, txt.Interval);

        if (flow.MoveIf(TokType.Id, out var headToken))
        {
            //fun call
            // 'id(1,2)'
            if (flow.IsCurrent(TokType.ParenthObr))
                return ReadFunctionCall(flow, headToken);

            // variable with type definition
            //'id:int'
            var type = TryReadTypeDef(flow);
            if (type != FunnyType.Empty)
                return SyntaxNodeFactory.TypedVar(headToken.Value, type, headToken.Start, flow.CurrentTokenFinishPosition);
            // just variable
            // 'id'
            return SyntaxNodeFactory.Var(headToken);
        }

        if (flow.IsCurrent(TokType.TextOpenInterpolation))
            return ReadInterpolationText(flow);

        if (flow.IsCurrent(TokType.ParenthObr))
            return ReadParenthNodeList(flow);

        if (flow.IsCurrent(TokType.If))
            return ReadIfThenElseNode(flow);

        // '[' can be used as array index, only if there is new line
        if (flow.IsCurrent(TokType.ArrOBr))
            return ReadInitializeArrayNode(flow);

        if (flow.MoveIf(TokType.Default, out var defaultToken))
            return SyntaxNodeFactory.DefaultValue(defaultToken.Interval);

        if (flow.IsCurrent(TokType.NotAToken))
            throw Errors.NotAToken(flow.Current);
        return null;
    }


    /// <summary>
    /// Reads node with specified syntax priority
    /// throws FunParseException if underlying syntax is invalid,
    /// or returns null if underlying syntax cannot be represented as node
    /// (EOF for example)
    /// </summary>
    private static ISyntaxNode ReadNodeOrNull(TokFlow flow, int priority) {
        //Lower priority is the special case
        if (priority < MinPriority)
            return ReadAtomicNodeOrNull(flow);

        //starting with left Node
        var leftNode = ReadNodeOrNull(flow, priority - 1);

        //building the syntax tree
        while (true)
        {
            flow.SkipNewLines();
            // if flow is done than current node is everything we got
            // example:
            // 1*2+3 {return whole expression }
            if (flow.IsDone)
                return leftNode;

            var opToken = flow.Current;

            // Check for hidden multiplication
            // like "10x" or "10(x)"
            if (IsHiddenMultiplication(leftNode, opToken))
            {
                var rightNode = ReadAtomicNodeOrNull(flow).NotNull("R node cannot be read in hidden multiplication");
                return SyntaxNodeFactory.BinOperatorCall(CoreFunNames.Multiply, leftNode, rightNode);
            }

            //if current token is not an operation
            //than expression is done
            //example:
            // 1*2 \r{stops here} y=...
            if (!BinaryPriorities.TryGetValue(opToken.Type, out var opPriority))
                return leftNode;

            //if op has higher priority us
            //than expression is done
            // example:
            //2*3{stops here}-1
            if (opPriority > priority)
                return leftNode;

            if (leftNode == null)
                throw Errors.LeftBinaryArgumentIsMissing(opToken);

            if (opToken.Type == TokType.ArrOBr)
            {
                //We can use array slicing, only if there were no new lines before.
                //there is problem of choose between anonymous array init and array slice
                //otherwise
                if (flow.IsPrevious(TokType.NewLine))
                    return leftNode;

                leftNode = ReadArraySlice(flow, leftNode);
            }
            else if (opToken.Type == TokType.ParenthObr)
            {
                if (flow.IsPrevious(TokType.NewLine))
                    return leftNode;
                if (!flow.IsPrevious(TokType.ParenthCbr) && !flow.IsPrevious(TokType.FiCbr) &&
                    !flow.IsPrevious(TokType.Default))
                    return leftNode;
                //call result of previous expression:
                // (expr)(arg1, ... argN)
                leftNode = ReadResultCall(flow, leftNode);
            }
            else if (opToken.Type == TokType.Dot)
            {
                flow.MoveNext(); // dot
                if (!flow.MoveIf(TokType.Id, out var id))
                    throw Errors.FunctionOrStructMemberNameIsMissedAfterDot(opToken);
                // Open parenthesis. It means call
                if (flow.IsCurrent(TokType.ParenthObr))
                    leftNode = ReadFunctionCall(flow, id, leftNode);
                else //else it is struct field
                    leftNode = SyntaxNodeFactory.FieldAccess(leftNode, id);
            }
            else if (ComparisonChainOperators.Contains(opToken.Type))
                leftNode = ReadComparisonChain(flow, leftNode, priority);
            else
                leftNode = ReadBinOperator(flow, leftNode, opToken, priority);
        }
    }

    private static ISyntaxNode ReadBinOperator(TokFlow flow, ISyntaxNode leftNode, Tok opToken, int priority) {
        flow.MoveNext();
        var rightNode = ReadNodeOrNull(flow, priority - 1)
                        ?? throw Errors.RightBinaryArgumentIsMissing(leftNode, opToken);

        //building the tree from the left
        if (!OperatorFunNames.TryGetValue(opToken.Type, out var opFunName))
            throw Errors.OperatorIsUnknown(opToken);

        return SyntaxNodeFactory.BinOperatorCall(opFunName, leftNode, rightNode);
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

    private static ISyntaxNode ReadComparisonChain(TokFlow flow, ISyntaxNode leftNode, int priority) {

        var operators = new List<Tok>();
        var operands = new List<ISyntaxNode> { leftNode };

        while (ComparisonChainOperators.Contains(flow.Current.Type))
        {
            var opToken = flow.Current;
            flow.MoveNext();
            var nextNode = ReadNodeOrNull(flow, priority-1)
                           ?? throw Errors.RightBinaryArgumentIsMissing(leftNode, opToken);
            operands.Add(nextNode);
            operators.Add(opToken);
        }

        if (operands.Count == 2)
            return SyntaxNodeFactory.BinOperatorCall(OperatorFunNames[operators[0].Type], operands[0], operands[1]);
        else
            return SyntaxNodeFactory.ComparisonChain(operands, operators);
    }

    /// <summary>
    /// Check for hidden multiplication
    ///  like "10x" or 10(x)
    /// </summary>
    private static bool IsHiddenMultiplication(ISyntaxNode leftNode, Tok currentToken) {
        // left node has to be numeric constant
        if (leftNode is not ConstantSyntaxNode && leftNode is not GenericIntSyntaxNode)
            return false;

        // no space between number and variable name allowed
        if (leftNode.Interval.Finish != currentToken.Interval.Start)
            return false;
        // hidden multiplication allowed before 'id' or '(..)'

        return currentToken.Type == TokType.Id || currentToken.Type == TokType.ParenthObr;
    }

    private static ISyntaxNode ReadFunAnonymousFunction(TokFlow flow) {
        var pos = flow.CurrentTokenFinishPosition;

        var bodyOrTypeNotation = ReadNodeOrNull(flow);
        if (bodyOrTypeNotation == null)
            throw Errors.AnonymousFunBodyIsMissing(new Interval(flow.CurrentTokenPosition, pos));

        var returnType = TryReadTypeDef(flow);
        if (flow.Current.Is(TokType.Def))
        {
            if (bodyOrTypeNotation.ParenthesesCount != 1)
                throw Errors.UnexpectedTokenEqualAfterRule(flow.Current.Interval);

            flow.MoveNext();
            flow.SkipNewLines();
            //full typed defentionion like:
            // TokType.Rule (a[:type], b[:type]...)[:type] = body
            //so body is just a type definition
            var definition = bodyOrTypeNotation;
            bodyOrTypeNotation = ReadNodeOrNull(flow);
            if (bodyOrTypeNotation == null)
                throw Errors.AnonymousFunBodyIsMissing(new Interval(flow.CurrentTokenPosition, pos));
            return SyntaxNodeFactory.AnonymFunction(definition, returnType, bodyOrTypeNotation);
        }

        if (returnType != FunnyType.Empty)
            //If return type is specified, and there is no def after it - than it is an mistake
            throw Errors.AnonymousFunBodyIsMissing(new Interval(pos, pos));

        return SyntaxNodeFactory.SuperAnonymFunction(bodyOrTypeNotation);
    }

    private static ISyntaxNode ReadStruct(TokFlow flow) {
        var begin = flow.CurrentTokenFinishPosition;

        var equations = new List<EquationSyntaxNode>();
        bool hasAnyDelimeter = true;
        flow.SkipNewLines();

        while (true)
        {
            if (flow.MoveIf(TokType.FiCbr))
                break;
            if (!hasAnyDelimeter)
                throw Errors.StructFieldDelimiterIsMissed(new Interval(flow.CurrentTokenStartPosition - 1,
                    flow.CurrentTokenFinishPosition));

            if (!flow.MoveIf(TokType.Id, out var idToken))
                throw Errors.StructFieldIdIsMissed(flow.Current);

            var type = TryReadTypeDef(flow);
            if (type != FunnyType.Empty)
                throw Errors.StructFieldSpecificationIsNotSupportedYet(new Interval(idToken.Finish + 1,
                    flow.CurrentTokenStartPosition - 1));

            if (!flow.MoveIf(TokType.Def))
                throw Errors.StructFieldDefinitionTokenIsMissed(flow.Current);

            flow.SkipNewLines();
            var body = ReadNodeOrNull(flow);
            if (body == null)
                throw Errors.StructFieldBodyIsMissed(idToken);
            var equation = SyntaxNodeFactory.Equation(idToken, body);
            equations.Add(equation);
            //Read node or null may eat last new-line-token
            hasAnyDelimeter = flow.Previous.Type == TokType.NewLine;
            //skip ','
            if (flow.MoveIf(TokType.Sep))
                hasAnyDelimeter = true;
            //skip new lines
            if (flow.SkipNewLines())
                hasAnyDelimeter = true;
            //Unexpected EOF - means that } is missed
            if (flow.IsDoneOrEof())
                throw Errors.StructIsUndone(flow.CurrentTokenFinishPosition);
        }

        var interval = new Interval(begin, flow.CurrentTokenFinishPosition);
          return SyntaxNodeFactory.Struct(equations, interval);
    }

    private static ISyntaxNode ReadInterpolationText(TokFlow flow) {
        var openInterpolationToken = flow.AssertAndMove(TokType.TextOpenInterpolation);
        //interpolation
        var concatenations = new List<ISyntaxNode>();
        //Open interpolation string
        // '...{
        if (openInterpolationToken.Value != String.Empty)
            concatenations.Add(
                SyntaxNodeFactory.Constant(
                    new TextFunnyArray(openInterpolationToken.Value),
                    FunnyType.Text,
                    openInterpolationToken.Interval));

        while (true)
        {
            //Read interpolation body
            //{...}
            var allNext = ReadNodeOrNull(flow);
            if (allNext == null)
                throw Errors.InterpolationExpressionIsMissing(concatenations.Last());

            var toText = SyntaxNodeFactory.FunCall(CoreFunNames.ToText, new[] { allNext }, allNext.Interval);
            concatenations.Add(toText);


            //interpolation end
            // }...'
            if (flow.Current.Type is TokType.TextCloseInterpolation)
            {
                if (flow.Current.Value != String.Empty)
                {
                    concatenations.Add(
                        SyntaxNodeFactory.Constant(
                            new TextFunnyArray(flow.Current.Value),
                            FunnyType.Text,
                            flow.Current.Interval));
                }

                flow.MoveNext();

                var start = openInterpolationToken.Start;
                var finish = flow.Current.Finish;
                switch (concatenations.Count)
                {
                    //Cases for 1, 2 and 3 args are most common.
                    //Here is an optimization for these cases.
                    //
                    case 1:
                        return SyntaxNodeFactory.FunCall(CoreFunNames.ToText, concatenations, start, finish);
                    case 2:
                        return SyntaxNodeFactory.FunCall(CoreFunNames.Concat2Texts, concatenations, start, finish);
                    case 3:
                        return SyntaxNodeFactory.FunCall(CoreFunNames.Concat3Texts, concatenations, start, finish);
                    default:
                    {
                        var arrayOfTexts = SyntaxNodeFactory.Array(concatenations.ToArray(), start, finish);
                        return SyntaxNodeFactory.FunCall(
                            CoreFunNames.ConcatArrayOfTexts, new[] { arrayOfTexts }, start, finish);
                    }
                }
            }
            //interpolation continuation
            // }...{
            else if (flow.Current.Type is TokType.TextMidInterpolation)
            {
                concatenations.Add(
                    SyntaxNodeFactory.Constant(
                        new TextFunnyArray(flow.Current.Value),
                        FunnyType.Text,
                        openInterpolationToken.Interval));
                flow.MoveNext();
            }
            else
                throw new NFunImpossibleException("imp328. Invalid interpolation sequence");
        }
    }

    /// <summary>
    /// Read array index or array slice node
    /// </summary>
    private static ISyntaxNode ReadArraySlice(TokFlow flow, ISyntaxNode arrayNode) {
        var openBraket = flow.Current;
        flow.MoveNext();
        var index = ReadNodeOrNull(flow);

        if (!flow.MoveIf(TokType.Colon, out var colon))
        {
            if (index == null)
            {
                if (flow.MoveIf(TokType.ArrCBr, out var closeBracket))
                    throw Errors.ArrayIndexExpected(openBraket, closeBracket);
                else
                    throw Errors.ArrayIndexOrSliceExpected(openBraket);
            }

            if (!flow.MoveIf(TokType.ArrCBr))
                throw Errors.ArrayIndexCbrMissed(openBraket, flow.Current);

            return SyntaxNodeFactory.OperatorCall(
                CoreFunNames.GetElementName,
                new[] { arrayNode, index }, openBraket.Start,
                flow.CurrentTokenFinishPosition);
        }

        index ??= SyntaxNodeFactory.Constant(0, FunnyType.Int32, new Interval(openBraket.Start, colon.Finish));

        var end = ReadNodeOrNull(flow) ??
                  SyntaxNodeFactory.Constant(int.MaxValue, FunnyType.Int32, new Interval(colon.Finish, flow.CurrentTokenFinishPosition));

        if (!flow.MoveIf(TokType.Colon, out _))
        {
            if (!flow.MoveIf(TokType.ArrCBr))
                throw Errors.ArraySliceCbrMissed(openBraket, flow.Current, false);
            return SyntaxNodeFactory.OperatorCall(
                CoreFunNames.SliceName, new[] { arrayNode, index, end }, openBraket.Start, flow.CurrentTokenFinishPosition);
        }

        var step = ReadNodeOrNull(flow);
        if (!flow.MoveIf(TokType.ArrCBr))
            throw Errors.ArraySliceCbrMissed(openBraket, flow.Current, true);
        if (step == null)
            return SyntaxNodeFactory.OperatorCall(
                CoreFunNames.SliceName, new[] { arrayNode, index, end }, openBraket.Start, flow.CurrentTokenFinishPosition);

        return SyntaxNodeFactory.OperatorCall(
            CoreFunNames.SliceName, new[] { arrayNode, index, end, step }, openBraket.Start, flow.CurrentTokenFinishPosition);
    }

    /// <summary>
    /// Read array initialization node.
    /// [a..b]
    /// [a..b step c]
    /// [a,b,c,d]
    /// </summary>
    /// <param name="flow"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private static ISyntaxNode ReadInitializeArrayNode(TokFlow flow) {
        var startTokenNum = flow.CurrentTokenPosition;
        var openBracket = flow.AssertAndMove(TokType.ArrOBr);

        var list = ReadNodeList(flow);

        if (list.Count == 1 && flow.MoveIf(TokType.TwoDots, out var twoDots))
        {
            // Range [a..b] or [a..b step c]
            var secondArg = ReadNodeOrNull(flow);
            if (secondArg == null)
            {
                var lastToken = twoDots;
                var missedVal = flow.Current;

                if (flow.Current.Is(TokType.ArrCBr))
                {
                    lastToken = flow.Current;
                    missedVal = default;
                }
                else if (flow.Current.Is(TokType.TwoDots))
                {
                    lastToken = flow.Current;
                    missedVal = default;
                }

                throw Errors.ArrayInitializeSecondIndexMissed(openBracket, lastToken, missedVal);
            }

            if (flow.MoveIf(TokType.Step, out var step))
            {
                var thirdArg = ReadNodeOrNull(flow);
                if (thirdArg == null)
                {
                    var lastToken = step;
                    var missedVal = flow.Current;
                    if (flow.Current.Is(TokType.ArrCBr))
                    {
                        lastToken = flow.Current;
                        missedVal = default;
                    }
                    else if (flow.Current.Is(TokType.TwoDots))
                    {
                        lastToken = flow.Current;
                        missedVal = default;
                    }

                    throw Errors.ArrayInitializeStepMissed(openBracket, lastToken, missedVal);
                }

                if (!flow.MoveIf(TokType.ArrCBr, out var closeBracket))
                    throw Errors.ArrayIntervalInitializeCbrMissed(openBracket, flow.Current, true);

                return SyntaxNodeFactory.OperatorCall(
                    name: CoreFunNames.RangeName,
                    args: new[] { list[0], secondArg, thirdArg },
                    start: openBracket.Start,
                    end: closeBracket.Finish);
            }
            else
            {
                if (!flow.MoveIf(TokType.ArrCBr, out var closeBracket))
                    throw Errors.ArrayIntervalInitializeCbrMissed(openBracket, flow.Current, false);
                return SyntaxNodeFactory.OperatorCall(
                    name: CoreFunNames.RangeName,
                    args: new[] { list[0], secondArg },
                    start: openBracket.Start,
                    end: closeBracket.Finish);
            }
        }

        if (!flow.MoveIf(TokType.ArrCBr, out var closeBr))
            throw Errors.ArrayInitializeByListError(startTokenNum, flow);
        return SyntaxNodeFactory.Array(list.ToArray(), openBracket.Start, closeBr.Finish);
    }

    /// <summary>
    /// Read nodes enlisted in parenthesis
    /// (a,b,c)
    /// </summary>
    private static ISyntaxNode ReadParenthNodeList(TokFlow flow) {
        int start = flow.CurrentTokenStartPosition;
        int obrPos = flow.CurrentTokenPosition;
        flow.MoveNext();
        var nodeList = ReadNodeList(flow);

        if (!flow.MoveIf(TokType.ParenthCbr, out var cbr))
            throw Errors.ParenthesisExpressionListError(obrPos, flow);
        var interval = new Interval(start, cbr.Finish);
        if (nodeList.Count == 1)
        {
            nodeList[0].Interval = interval;
            nodeList[0].ParenthesesCount++;
            return nodeList[0] ?? throw new NFunImpossibleException("There is no first node in nodelist");
        }
        else
            return SyntaxNodeFactory.ListOf(nodeList, interval, 1);
    }

    /// <summary>
    /// Read if-then-if-then-else node
    /// </summary>
    private static ISyntaxNode ReadIfThenElseNode(TokFlow flow) {
        int ifElseStart = flow.CurrentTokenStartPosition;
        var ifThenNodes = new List<IfCaseSyntaxNode>();
        do
        {
            int conditionStart = flow.CurrentTokenStartPosition;

            var hasNewLineBefore = flow.IsPrevious(TokType.NewLine);

            //if
            if (!flow.MoveIf(TokType.If))
                throw Errors.IfKeywordIsMissing(ifElseStart, flow.CurrentTokenFinishPosition);
            if (ifThenNodes.Any() && !hasNewLineBefore)
                throw Errors.NewLineMissedBeforeRepeatedIf(flow.Previous.Interval);

            //(condition)
            if (!flow.MoveIf(TokType.ParenthObr))
            {
                var failedExpr = ReadNodeOrNull(flow);
                if (failedExpr != null)
                    throw Errors.IfConditionIsNotInParenthesis(
                        failedExpr.Interval.Start,
                        failedExpr.Interval.Finish);
                else
                    throw Errors.IfConditionIsNotInParenthesis(ifElseStart, flow.CurrentTokenFinishPosition);
            }

            var condition = ReadNodeOrNull(flow);
            if (condition == null)
                throw Errors.ConditionIsMissing(conditionStart, flow.CurrentTokenFinishPosition);
            if (!flow.MoveIf(TokType.ParenthCbr))
                throw Errors.IfConditionIsNotInParenthesis(ifElseStart, flow.CurrentTokenFinishPosition);

            //then
            var thenResult = ReadNodeOrNull(flow);
            if (thenResult == null)
                throw Errors.ThenExpressionIsMissing(conditionStart, flow.CurrentTokenFinishPosition);

            ifThenNodes.Add(
                SyntaxNodeFactory.IfCase(
                    condition, thenResult,
                    start: conditionStart,
                    end: flow.CurrentTokenFinishPosition));
        } while (!flow.IsCurrent(TokType.Else));

        if (!flow.MoveIf(TokType.Else))
            throw Errors.ElseKeywordIsMissing(ifElseStart, flow.CurrentTokenFinishPosition);

        var elseResult = ReadNodeOrNull(flow);
        if (elseResult == null)
            throw Errors.ElseExpressionIsMissing(ifElseStart, flow.CurrentTokenFinishPosition);

        return SyntaxNodeFactory.IfElse(ifThenNodes.ToArray(), elseResult, ifElseStart, flow.CurrentTokenFinishPosition);
    }

    private static ISyntaxNode ReadFunctionCall(TokFlow flow, Tok head, ISyntaxNode pipedVal = null) {
        var obrId = flow.CurrentTokenPosition;
        var start = pipedVal?.Interval.Start ?? head.Start;
        if (!flow.MoveIf(TokType.ParenthObr))
            throw Errors.FunctionCallObrMissed(start, head.Value, flow.CurrentTokenFinishPosition, pipedVal);

        var arguments = ReadNodeList(flow);

        if (!flow.MoveIf(TokType.ParenthCbr, out _))
            throw Errors.FunctionArgumentError(head.Value, obrId, flow);

        if (pipedVal == null)
            return SyntaxNodeFactory.FunCall(head.Value, arguments, start, flow.CurrentTokenFinishPosition);
        else
            return SyntaxNodeFactory.PipedFunCall(head.Value, pipedVal, arguments, start, flow.CurrentTokenFinishPosition);
    }

    private static ISyntaxNode ReadResultCall(TokFlow flow, ISyntaxNode functionResultNode) {
        var obrId = flow.CurrentTokenPosition;
        if (!flow.MoveIf(TokType.ParenthObr))
            AssertChecks.Panic("Panic. Something wrong in parser");

        var arguments = ReadNodeList(flow);
        if (!flow.MoveIf(TokType.ParenthCbr, out var cbr))
            throw Errors.FunctionArgumentError(functionResultNode.ToString(), obrId, flow);

        return SyntaxNodeFactory.ResultFunCall(functionResultNode, arguments, cbr.Finish);
    }

    private static List<ISyntaxNode> ReadNodeList(TokFlow flow) {
        var read = new List<ISyntaxNode>();
        do
        {
            var exp = ReadNodeOrNull(flow);
            if (exp != null)
                read.Add(exp);
            else
                break; //trailing coma support
        } while (flow.MoveIf(TokType.Sep, out _));

        return read;
    }

    private static FunnyType TryReadTypeDef(TokFlow flow) {
        if (!flow.IsCurrent(TokType.Colon))
            return FunnyType.Empty;

        flow.MoveNext();
        var current = flow.Current;
        var type = flow.ReadType();
        if (type == FunnyType.Empty)
            throw Errors.TypeExpectedButWas(current);
        return type;
    }
}
