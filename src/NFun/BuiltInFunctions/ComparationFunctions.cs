using System;
using System.Collections.Generic;
using System.Text;
using NFun.Interpritation.Functions;
using NFun.Runtime.Arrays;
using NFun.Types;

namespace NFun.BuiltInFunctions
{
    public class NotEqualFunction : GenericFunctionBase
    {
        public NotEqualFunction() : base(CoreFunNames.NotEqual, VarType.Bool, VarType.Generic(0), VarType.Generic(0)) { }
        public override object Calc(object[] args) => !TypeHelper.AreEqual(args[0], args[1]);
    }

    public class EqualFunction : GenericFunctionBase
    {
        public EqualFunction() : base(CoreFunNames.Equal, VarType.Bool, VarType.Generic(0), VarType.Generic(0))
        {
        }

        public override object Calc(object[] args) 
            => TypeHelper.AreEqual(args[0], args[1]);

    }

    public class MoreFunction : GenericFunctionBase
    {
        public MoreFunction() : base(CoreFunNames.More, GenericConstrains.Comparable,VarType.Bool, VarType.Generic(0), VarType.Generic(0)){}

        public override object Calc(object[] args)
        {
            var a = ((IComparable)args[0]);
            var b = ((IComparable)args[1]);
            return a.CompareTo(b)==1;
        }
    }
    public class MoreOrEqualFunction : GenericFunctionBase
    {
        public MoreOrEqualFunction() : base(CoreFunNames.MoreOrEqual, GenericConstrains.Comparable , VarType.Bool, VarType.Generic(0), VarType.Generic(0)) { }

        public override object Calc(object[] args)
        {
            var a = ((IComparable)args[0]);
            var b = ((IComparable)args[1]);
            return a.CompareTo(b) != -1;
        }
    }
    public class LessFunction : GenericFunctionBase
    {
        public LessFunction() : base(CoreFunNames.Less, GenericConstrains.Comparable , VarType.Bool, VarType.Generic(0), VarType.Generic(0)) { }

        public override object Calc(object[] args)
        {
            var a = ((IComparable)args[0]);
            var b = ((IComparable)args[1]);
            return a.CompareTo(b) == -1;
        }
    }
    public class LessOrEqualFunction : GenericFunctionBase
    {
        public LessOrEqualFunction() : base(CoreFunNames.LessOrEqual, GenericConstrains.Comparable , VarType.Bool, VarType.Generic(0), VarType.Generic(0)) { }

        public override object Calc(object[] args)
        {
            var a = ((IComparable)args[0]);
            var b = ((IComparable)args[1]);
            return a.CompareTo(b) != 1;
        }
    }
    public class MinFunction : GenericFunctionBase
    {
        public MinFunction() : base("min", GenericConstrains.Comparable, VarType.Generic(0), VarType.Generic(0), VarType.Generic(0)) { }

        public override object Calc(object[] args)
        {
            var a = ((IComparable)args[0]);
            var b = ((IComparable)args[1]);
            if (a.CompareTo(b) != 1) return a;
            return b;
        }
    }
  
    public class MaxFunction : GenericFunctionBase
    {
        public MaxFunction() : base("max", GenericConstrains.Comparable, VarType.Generic(0), VarType.Generic(0), VarType.Generic(0)) { }

        public override object Calc(object[] args)
        {
            var a = ((IComparable)args[0]);
            var b = ((IComparable)args[1]);
            if (a.CompareTo(b) == 1) return a;
            return b;
        }
    }
}
