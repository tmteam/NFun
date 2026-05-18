using NFun.Interpretation.Nodes;
using NFun.Runtime;
using NFun.Types;

namespace NFun.Interpretation.Functions;

internal class ConcreteUserFunction : FunctionWithManyArguments, IUserFunction {

    internal static ConcreteUserFunction Create(
        string name,
        VariableSource[] variables,
        IExpressionNode expression,
        bool isRecursive,
        VariableSource[] localSources = null,
        int[] sharedRecursionDepth = null,
        bool isLambda = false) {
        var argTypes = new FunnyType[variables.Length];
        for (var i = 0; i < variables.Length; i++)
            argTypes[i] = variables[i].Type;
        ConcreteUserFunction result = isRecursive
            ? new ConcreteRecursiveUserFunction(
                name, variables, expression, argTypes,
                localSources ?? System.Array.Empty<VariableSource>(),
                sharedRecursionDepth)
            : new ConcreteUserFunction(name, variables, expression, argTypes);
        result._isLambda = isLambda;
        return result;
    }

    // True for `rule`/`fun(x):` lambdas — they treat the body's last expression
    // value as the implicit return. Named `fun` definitions return `none` when
    // no explicit `return` fires (Statements.md §Functions).
    internal bool _isLambda;

    internal ConcreteUserFunction(
        string name,
        VariableSource[] argumentSources,
        IExpressionNode expression, FunnyType[] argTypes)
        : base(
            name,
            expression.Type,
            argTypes) {
        ArgumentSources = argumentSources;
        Expression = expression;
    }

    internal readonly IExpressionNode Expression;

    internal VariableSource[] ArgumentSources { get; }

    public bool IsGeneric => false;
    
    public virtual FunctionRecursionKind RecursionKind => FunctionRecursionKind.NoRecursion;
    
    protected void SetVariables(object[] args) {
        for (int i = 0; i < args.Length; i++)
            ArgumentSources[i].SetFunnyValueUnsafe(args[i]);
    }

    public override IConcreteFunction Clone(ICloneContext context)
    {
        var sourceClones = ArgumentSources.SelectToArray(s => s.Clone());
        var scopeContext = context.GetScopedContext(sourceClones);

        var newUserFunction = new ConcreteUserFunction(Name, sourceClones, Expression.Clone(scopeContext), ArgTypes);
        newUserFunction._isLambda = _isLambda;
        //context.AddUserFunction(this, newUserFunction);
        return newUserFunction;
    }

    public override object Calc(object[] args) {
        SetVariables(args);
        var result = Expression.Calc();
        if (result is Nodes.ReturnSignal signal)
            return signal.Value;
        // Multi-line `fun` body that falls off the end without a `return`
        // returns `none` per Statements.md §Functions. Lambdas (`rule`,
        // `fun(x):` block) keep the last-expression-as-implicit-return
        // semantics. Single-line `f(x) = expr` is not a BlockExpressionNode.
        if (!_isLambda && Expression is Nodes.BlockExpressionNode)
            return Types.FunnyNone.Instance;
        return result;
    }
    
    public override string ToString()
        => $"FUN-user {TypeHelper.GetFunSignature(Name, ReturnType, ArgTypes)}";
}