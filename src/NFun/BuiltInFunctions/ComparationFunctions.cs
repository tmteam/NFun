using System;
using NFun.Interpritation.Functions;
using NFun.Types;

namespace NFun.BuiltInFunctions
{
    public class NotEqualFunction : GenericFunctionWithTwoArguments
    {
        public NotEqualFunction() : base(CoreFunNames.NotEqual, VarType.Bool, VarType.Generic(0), VarType.Generic(0)) { }
        protected override object Calc(object a, object b)=> !TypeHelper.AreEqual(a, b);
    }

    public class EqualFunction : GenericFunctionWithTwoArguments
    {
        public EqualFunction() : base(CoreFunNames.Equal, VarType.Bool, VarType.Generic(0), VarType.Generic(0))
        {
        }

        protected override object Calc(object a, object b)
            => TypeHelper.AreEqual(a,b);

    }

    public class MoreFunction : GenericFunctionBase
    {
        public MoreFunction() : base(CoreFunNames.More, GenericConstrains.Comparable,VarType.Bool, VarType.Generic(0), VarType.Generic(0)){}

        protected override object Calc(object[] args)
        {
            var a = (IComparable)args[0];
            var b = (IComparable)args[1];
            return a.CompareTo(b)==1;
        }
    }
    public class MoreOrEqualFunction : GenericFunctionBase
    {
        public MoreOrEqualFunction() : base(CoreFunNames.MoreOrEqual, GenericConstrains.Comparable , VarType.Bool, VarType.Generic(0), VarType.Generic(0)) { }

        protected override object Calc(object[] args)
        {
            var a = (IComparable)args[0];
            var b = (IComparable)args[1];
            return a.CompareTo(b) != -1;
        }
    }
    public class LessFunction : GenericFunctionWithTwoArguments
    {
        public LessFunction() : base(CoreFunNames.Less, GenericConstrains.Comparable , VarType.Bool, VarType.Generic(0), VarType.Generic(0)) { }

        protected override object Calc(object arg1, object arg2)
        {
            var left  = (IComparable)arg1;
            var right = (IComparable)arg2;
            return left.CompareTo(right) == -1;
        }
    }
    public class LessOrEqualFunction : GenericFunctionBase
    {
        public LessOrEqualFunction() : base(CoreFunNames.LessOrEqual, GenericConstrains.Comparable , VarType.Bool, VarType.Generic(0), VarType.Generic(0)) { }

        protected override object Calc(object[] args)
        {
            var a = (IComparable)args[0];
            var b = (IComparable)args[1];
            return a.CompareTo(b) != 1;
        }
    }
    public class MinFunction : GenericFunctionBase
    {
        public MinFunction() : base("min", GenericConstrains.Comparable, VarType.Generic(0), VarType.Generic(0), VarType.Generic(0)) { }

        protected override object Calc(object[] args)
        {
            var a = (IComparable)args[0];
            var b = (IComparable)args[1];
            if (a.CompareTo(b) != 1) return a;
            return b;
        }
    }
  
    public class MaxFunction : GenericFunctionWithTwoArguments
    {
        public MaxFunction() : base("max", GenericConstrains.Comparable, VarType.Generic(0), VarType.Generic(0), VarType.Generic(0)) { }

        protected override object Calc(object a, object b)
        {
            var arg1 = (IComparable)a;
            var arg2 = (IComparable)b;
            return arg1.CompareTo(arg2) == 1 ? a : b;
        }
    }
}
