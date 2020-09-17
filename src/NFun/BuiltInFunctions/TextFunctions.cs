using System;
using System.Linq;
using System.Text;
using NFun.Interpritation.Functions;
using NFun.Runtime.Arrays;
using NFun.Types;

namespace NFun.BuiltInFunctions
{
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
  
    public class TrimFunction : FunctionWithManyArguments
    {
        public TrimFunction() : base("trim",VarType.Text,VarType.Text){}

        public override object Calc(object[] args) => args.GetTextOrThrow(0).Trim();
    }
    
    public class TrimStartFunction : FunctionWithManyArguments
    {
        public TrimStartFunction() : base("trimStart",VarType.Text,VarType.Text){
        }

        public override object Calc(object[] args) => args.GetTextOrThrow(0).TrimStart();
    }
    
    public class TrimEndFunction : FunctionWithManyArguments
    {
        public TrimEndFunction() : base("trimEnd",VarType.Text,VarType.Text){}
        public override object Calc(object[] args) => args.GetTextOrThrow(0).TrimEnd();
    }
    
    
    public class SplitFunction : FunctionWithManyArguments
    {
        public SplitFunction() : base("split",
            VarType.ArrayOf(VarType.Text), 
            VarType.Text,
            VarType.Text){
        }
        public override object Calc(object[] args)
            => new ImmutableFunArray(
                args.GetTextOrThrow(0)
                .Split( new[] {args.GetTextOrThrow(1)}, StringSplitOptions.RemoveEmptyEntries)
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