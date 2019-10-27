using System;
using System.Linq;
using System.Xml.Schema;
using NFun.Interpritation.Functions;
using NFun.Runtime.Arrays;
using NFun.Types;

namespace NFun.BuiltInFunctions
{
    public class SortTextFunction : FunctionBase
    {
        public SortTextFunction() : base("sort", VarType.ArrayOf(VarType.Text), VarType.ArrayOf(VarType.Text)){}

        public override object Calc(object[] args)
        {
            var arr = args.GetListOfStringOrThrow(0).ToArray();
            Array.Sort(arr, StringComparer.InvariantCulture);
            return new ImmutableFunArray(arr.Select(s=>new TextFunArray(s)).ToArray());
        }
    }
    public class TrimFunction : FunctionBase
    {
        public TrimFunction() : base("trim",
            VarType.Text, 
            VarType.Text)
        {
            
        }

        public override object Calc(object[] args) => args.GetTextOrThrow(0).Trim();
    }
    
    public class TrimStartFunction : FunctionBase
    {
        public TrimStartFunction() : base("trimStart",
            VarType.Text, 
            VarType.Text)
        {
            
        }

        public override object Calc(object[] args) => args.GetTextOrThrow(0).TrimStart();
    }
    
    public class TrimEndFunction : FunctionBase
    {
        public TrimEndFunction() : base("trimEnd",
            VarType.Text, 
            VarType.Text)
        {
            
        }
        public override object Calc(object[] args) => args.GetTextOrThrow(0).TrimEnd();
    }
    
    
    public class SplitFunction : FunctionBase
    {
        public SplitFunction() : base("split",
            VarType.ArrayOf(VarType.Text), 
            VarType.Text,
            VarType.Text)
        {
            
        }
        public override object Calc(object[] args)
            => new ImmutableFunArray(
                args.GetTextOrThrow(0)
                .Split( new[] {args.GetTextOrThrow(1)}, StringSplitOptions.RemoveEmptyEntries)
                .Select(s=>new TextFunArray(s))
                .ToArray());
    }
    
    
    public class JoinFunction : FunctionBase
    {
        public JoinFunction() : base("join",
            VarType.Text,
            VarType.ArrayOf(VarType.Text),
            VarType.Text)
        {
            
        }
        public override object Calc(object[] args)
            =>  new TextFunArray(string.Join(args.GetTextOrThrow(1), args.GetListOfStringOrThrow(0)));
    }
}