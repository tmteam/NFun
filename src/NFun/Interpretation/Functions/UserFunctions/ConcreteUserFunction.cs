using System.Linq;
using NFun.Interpretation.Nodes;
using NFun.Runtime;
using NFun.Types;

namespace NFun.Interpretation.Functions;

internal class ConcreteUserFunction : FunctionWithManyArguments, IUserFunction {

    internal static ConcreteUserFunction Create(
        string name,
        VariableSource[] variables,
        IExpressionNode expression,
        bool isRecursive) {
        var argTypes = new FunnyType[variables.Length];
        for (var i = 0; i < variables.Length; i++)
            argTypes[i] = variables[i].Type;
        if (isRecursive)
            return new ConcreteRecursiveUserFunction(name, variables, expression, argTypes);
        else
            return new ConcreteUserFunction(name, variables, expression, argTypes);
    }

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
        //context.AddUserFunction(this, newUserFunction);
        return newUserFunction;
    }

    public override object Calc(object[] args) {
        SetVariables(args);
        return Expression.Calc();
    }
    
    public override string ToString()
        => $"FUN-user {TypeHelper.GetFunSignature(Name, ReturnType, ArgTypes)}";
}