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
            return a.CompareTo(b) > 0;
        }
    }
    public class MoreOrEqualFunction : GenericFunctionBase
    {
        public MoreOrEqualFunction() : base(CoreFunNames.MoreOrEqual, GenericConstrains.Comparable , VarType.Bool, VarType.Generic(0), VarType.Generic(0)) { }

        protected override object Calc(object[] args)
        {
            var a = (IComparable)args[0];
            var b = (IComparable)args[1];
            return a.CompareTo(b) >= 0;
        }
    }
    public class LessFunction : GenericFunctionWithTwoArguments
    {
        public LessFunction() : base(CoreFunNames.Less, GenericConstrains.Comparable , VarType.Bool, VarType.Generic(0), VarType.Generic(0)) { }

        protected override object Calc(object arg1, object arg2)
        {
            var left  = (IComparable)arg1;
            var right = (IComparable)arg2;
            return left.CompareTo(right) <0;
        }
    }
    public class LessOrEqualFunction : GenericFunctionBase
    {
        public LessOrEqualFunction() : base(CoreFunNames.LessOrEqual, GenericConstrains.Comparable , VarType.Bool, VarType.Generic(0), VarType.Generic(0)) { }

        protected override object Calc(object[] args)
        {
            var a = (IComparable)args[0];
            var b = (IComparable)args[1];
            return a.CompareTo(b) <= 0;
        }
    }
    public class MinFunction : PureGenericFunctionBase
    {
        public MinFunction() : base("min", GenericConstrains.Comparable, 2) { }

        public override IConcreteFunction CreateConcrete(VarType[] concreteTypesMap)
        {
            var generic = concreteTypesMap[0];
            FunctionWithTwoArgs function  = new MinConcreteFunction();
            function.Setup(Name,generic);
            return function;
        }
        class MinConcreteFunction: FunctionWithTwoArgs
        {
            public override object Calc(object a, object b)
            {
                var arg1 = (IComparable)a;
                var arg2 = (IComparable)b;
                return arg1.CompareTo(arg2) >0 ? b : a;
            }
        }
    }
  
  
    public class MaxFunction : PureGenericFunctionBase
    {
        public MaxFunction() : base("max", GenericConstrains.Comparable, 2) { }

        public override IConcreteFunction CreateConcrete(VarType[] concreteTypesMap)
        {
            var generic = concreteTypesMap[0];
            FunctionWithTwoArgs function  = new MaxConcreteFunction();
            function.Setup(Name,generic);
            return function;
        }
        class MaxConcreteFunction: FunctionWithTwoArgs
        {
            public override object Calc(object a, object b)
            {
                var arg1 = (IComparable)a;
                var arg2 = (IComparable)b;
                var result =  arg1.CompareTo(arg2) > 0 ? a : b;
                return result;
            }
        }
    }
    
   
}
