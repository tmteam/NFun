using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NFun.Interpritation.Functions;
using NFun.Runtime;
using NFun.SyntaxParsing;
using NFun.Types;

namespace NFun.BuiltInFunctions
{
    public class GetVarTextInfoMetafunction: Metafunction
    {
        public GetVarTextInfoMetafunction() : base("getVarMetaInfo", VarType.Text, VarType.Anything)
        {
        }

        public override object Calc(object[] args)
        {
            var source = args[0] as VariableSource;
            if (source == null)
                return "variable not exist";
            var info = $"var meta info result:" +
                       $"{(source.IsOutput ? "output" : "input")} var: {source.Name}:{source.Type}.\r\n" +
                       $"Attributes: {string.Join<VarAttribute>(",", source.Attributes)} " +
                       $"value: {source.Value}";
            Console.WriteLine("");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(info);
            Console.ResetColor();
            return info;
        }
    }

    public class GetValOrDefault : GenericMetafunction
    {
        public GetValOrDefault() : base("defa", VarType.Generic(0), VarType.Generic(0))
        {
        }

        public override object Calc(object[] args) => throw new NotImplementedException();

        public virtual FunctionBase CreateConcrete(VarType[] concreteTypes)
        {
            return new ConcreteMetaFunction(
                name: Name,
                returnType: VarType.SubstituteConcreteTypes(ReturnType, concreteTypes),
                argTypes: ArgTypes.Select(a => VarType.SubstituteConcreteTypes(a, concreteTypes))
                    .ToArray());
        }

        public class ConcreteMetaFunction : FunctionBase
        {
            public ConcreteMetaFunction(
                string name,
                VarType returnType,
                params VarType[] argTypes)
                : base(TypeHelper.GetFunSignature(name, returnType, argTypes), returnType, argTypes)
            {
            }

            public override object Calc(object[] args)
            {
                var varType = ArgTypes[0];
                var source = args[0] as VariableSource;
                if (source == null)
                    return 0;
                return source.Value ?? 0;
            }
        }
    }
}
