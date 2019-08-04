using System;
using NFun.Interpritation.Functions;
using NFun.Runtime;
using NFun.Types;

namespace NFun.ExprementalTests
{
    public class IsGoodFunction : FunctionBase
    {
        public IsGoodFunction() : base("isGood", VarType.Bool, VarType.Anything)
        {
            
        }
        public override object Calc(object[] args)
        {
            if (args[0] is IVQT add)
                return add.Q==192;
            return true;
        }
    }
    public class VQFunction : GenericFunctionBase
    {
        public VQFunction() : base("vq", VarType.Generic(0),
            VarType.Generic(0), 
            VarType.Int32)
        {
            
        }
        public override object Calc(object[] args)
        {
            if (args[0] is IVQT v)
                return VqtHelper.MakeVQT(v.V, args.Get<Int32>(1), v.T);
            return VqtHelper.MakeVQT(args[0], args.Get<Int32>(1), -1);
        }
    }
    public class VQTFunction : GenericFunctionBase
    {
        public VQTFunction() : base("vqt", VarType.Generic(0),
            VarType.Generic(0), 
            VarType.Int32, 
            VarType.Int64)
        {
            
        }
        public override object Calc(object[] args)
            => VqtHelper.MakeVQT(args[0], args.Get<Int32>(1), args.Get<Int64>(2));
    }

    
    public class minQFunction : FunctionBase
    {
        public minQFunction() : base("worstQ", VarType.Int32,VarType.ArrayOf(VarType.Anything))
        {
            
        }
        public override object Calc(object[] args)
        {
            var arr = (args[0] as IFunArray);
            int res = 192;
            foreach (var a in arr)
            {
                if(a is IVQT vqt)
                {
                    if (vqt.Q < res)
                        res = vqt.Q;
                }
            }
            return res;
        }
    }
    public class SetTFunction : GenericFunctionBase
    {
        public SetTFunction() : base("setT", VarType.Generic(0),
            VarType.Generic(0), VarType.Int64)
        {
            
        }
        public override object Calc(object[] args)
        {
            var t = args.Get<Int64>(1);
            if (args[0] is IVQT vqt)
            {
                return VqtHelper.MakeVQT(vqt.V, vqt.Q, t);
            }
            return VqtHelper.MakeVQT(args[0], -1, t);
        }
    }
    public class SetQFunction : GenericFunctionBase
    {
        public SetQFunction() : base("setQ", VarType.Generic(0),VarType.Generic(0), VarType.Int32)
        {
            
        }
        public override object Calc(object[] args)
        {
            var q = args.Get<int>(1);
            if (args[0] is IVQT vqt)
            {
                return VqtHelper.MakeVQT(vqt.V, q, vqt.T);
            }
            return VqtHelper.MakeVQT(args[0], q, -1);
            
        }
    }
    public class maxQFunction : FunctionBase
    {
        public maxQFunction() : base("bestQ", VarType.Int32,VarType.ArrayOf(VarType.Anything))
        {
            
        }
        public override object Calc(object[] args)
        {
            var arr = (args[0] as IFunArray);
            int res = 0;
            foreach (var a in arr)
            {
                if(a is IVQT vqt)
                {
                    if (vqt.Q > res)
                        res = vqt.Q;
                }
            }
            return res;
        }
    }
    public class GoodStamp : FunctionBase
    {
        public GoodStamp() : base("good", VarType.Int32)
        {
        }

        public override object Calc(object[] args)
        {
            return 192;
        }
    }
    
    public class BadStamp : FunctionBase
    {
        public BadStamp() : base("bad", VarType.Int32) {
        }

        public override object Calc(object[] args) {
            return 8;
        }
    }
    public class ToUtcFunction : FunctionBase
    {
        public ToUtcFunction() : base("toUtc", VarType.Int64, new []{VarType.Int64})
        {
        }

        public override object Calc(object[] args)
        {
            var ticks = args.Get<long>(0);
            var dt = new DateTime(ticks);
            return dt.ToUniversalTime().Ticks;
        }
    }
    public class NowFunction : FunctionBase
    {
        public NowFunction() : base("now", VarType.Int64, new VarType[0])
        {
        }

        public override object Calc(object[] args) 
            => DateTime.Now.Ticks;
    }
}
