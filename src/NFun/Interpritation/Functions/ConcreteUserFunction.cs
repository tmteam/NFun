using System.Collections.Generic;
using System.Linq;
using NFun.Interpritation.Nodes;
using NFun.Runtime;
using NFun.Types;

namespace NFun.Interpritation.Functions
{
    public class ConcreteUserFunction : FunctionWithManyArguments
    {
        internal VariableSource[] ArgumentSources { get; }
        public bool IsReturnTypeStrictlyTyped { get; }
        protected readonly IExpressionNode Expression;

        internal static ConcreteUserFunction Create(string name,
            VariableSource[] variables,
            bool isReturnTypeStrictlyTyped,
            IExpressionNode expression,
            bool isRecursive)
        {
            var argTypes = new FunnyType[variables.Length];
            for (int i = 0; i < variables.Length; i++) 
                argTypes[i] = variables[i].Type;
            if(isRecursive)
                return new ConcreteRecursiveUserFunction(name, variables, isReturnTypeStrictlyTyped, expression, argTypes);
            else
                return new ConcreteUserFunction(name, variables, isReturnTypeStrictlyTyped, expression, argTypes);
        }

        internal ConcreteUserFunction(
            string name, 
            VariableSource[] argumentSources,
            bool isReturnTypeStrictlyTyped,
            IExpressionNode expression, FunnyType[] argTypes) 
            : base(
                name, 
                expression.Type,
                argTypes)
        {
            ArgumentSources = argumentSources;
            IsReturnTypeStrictlyTyped = isReturnTypeStrictlyTyped;
            Expression = expression;
        }

        protected void SetVariables(object[] args)
        {
            for (int i = 0; i < args.Length; i++)
                ArgumentSources[i].InternalFunnyValue=  args[i];
        }
        public override object Calc(object[] args)
        {
            SetVariables(args);
            return Expression.Calc();
        }

        public override string ToString() 
            => $"{Name}({string.Join(",",ArgTypes.Select(a=>a.ToString()))}):{ReturnType}";
    }
}