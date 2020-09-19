using System;
using System.Linq;
using System.Text;
using NFun.Interpritation.Functions;
using NFun.Runtime.Arrays;
using NFun.Types;

namespace NFun.BuiltInFunctions
{
    public class ToTextFunction : FunctionWithSingleArg
    {
        public ToTextFunction() : base(CoreFunNames.ToText, VarType.Text, VarType.Anything) { }

        public override object Calc(object a) => new TextFunArray(TypeHelper.GetFunText(a));
    }
    public class ConcatArrayOfTextsFunction : FunctionWithSingleArg
    {
        public ConcatArrayOfTextsFunction() : base(CoreFunNames.ConcatArrayOfTexts, VarType.Text,VarType.ArrayOf(VarType.Anything)) { }

        public override object Calc(object a)
        {
            var sb = new StringBuilder();
            foreach (var subElement in (IFunArray) a)
            {
                sb.Append(TypeHelper.GetFunText(subElement));
            }
            return new TextFunArray(sb.ToString());
        }
    }
    public class Concat2TextsFunction : FunctionWithTwoArgs
    {
        public Concat2TextsFunction() : base(CoreFunNames.Concat2Texts, VarType.Text,VarType.Anything,VarType.Anything) { }

        public override object Calc(object a, object b) 
            => new TextFunArray(TypeHelper.GetFunText(a) + TypeHelper.GetFunText(b));
    }
    
    public class Concat3TextsFunction : FunctionWithManyArguments
    {
        public Concat3TextsFunction() : base(CoreFunNames.Concat3Texts, VarType.Text,VarType.Anything,VarType.Anything,VarType.Anything) { }

        public override object Calc(object[] args)
        {
            var sb = new StringBuilder();
            foreach (var subElement in  args) 
                sb.Append(TypeHelper.GetFunText(subElement));
            return new TextFunArray(sb.ToString());
        }
    }
    
    public class FormatTextFunction : FunctionWithManyArguments
    {
        public FormatTextFunction() : base("format", VarType.Text, VarType.Text, VarType.ArrayOf(VarType.Anything)) { }

        public override object Calc(object[] args)
        {
            var template = args.GetTextOrThrow(0);
            var formatArguments = (IFunArray) args[1];
            var result = string.Format(template, formatArguments);
            return new TextFunArray(result);
        }
    }
  
    public class TrimFunction : FunctionWithSingleArg
    {
        public TrimFunction() : base("trim",VarType.Text,VarType.Text){}

        public override object Calc(object a) => ((IFunArray) a).ToText().TrimStart().AsFunText();
    }
    
    public class TrimStartFunction : FunctionWithSingleArg
    {
        public TrimStartFunction() : base("trimStart",VarType.Text,VarType.Text){ }

        public override object Calc(object a) => ((IFunArray) a).ToText().TrimStart().AsFunText();
    }
    
    public class TrimEndFunction : FunctionWithSingleArg
    {
        public TrimEndFunction() : base("trimEnd",VarType.Text,VarType.Text){}
        public override object Calc(object a) => ((IFunArray) a).ToText().TrimEnd().AsFunText();
    }
    
    
    public class SplitFunction : FunctionWithTwoArgs
    {
        public SplitFunction() : base("split",
            VarType.ArrayOf(VarType.Text), 
            VarType.Text,
            VarType.Text){
        }

        public override object Calc(object a, object b) =>
            new ImmutableFunArray(
                TypeHelper.GetFunText(a)
                    .Split( new[] {TypeHelper.GetFunText(b)}, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s=>new TextFunArray(s))
                    .ToArray());
    }
    
    
    public class JoinFunction : FunctionWithTwoArgs
    {
        public JoinFunction() : base("join",VarType.Text,VarType.ArrayOf(VarType.Text),VarType.Text)
        {
        }

        public override object Calc(object a, object b)
        {
            var arr       = (IFunArray) a;
            var separator = (IFunArray) b;
            var join = string.Join(separator.ToText(), arr.Select(TypeHelper.GetFunText));
            return new TextFunArray(join);
        }
    }
}