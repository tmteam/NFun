using System.Linq;
using NFun.Interpritation.Functions;
using NFun.Runtime;
using NFun.Runtime.Arrays;
using NFun.Types;

namespace NFun.BuiltInFunctions
{
    public class IsInSingleGenericFunctionDefenition : GenericFunctionBase
    {
        public IsInSingleGenericFunctionDefenition() : base(CoreFunNames.In, 
            VarType.Bool,
            VarType.Generic(0), 
            VarType.ArrayOf(VarType.Generic(0)))
        {
        }
        public override object Calc(object[] args)
        {
            var val = args[0];
            var arr = (IFunArray)args[1];
            return arr.Any(a => TypeHelper.AreEqual(a, val));
        }
    }

    public class SliceWithStepGenericFunctionDefenition : GenericFunctionBase
    {
        public SliceWithStepGenericFunctionDefenition() : base(CoreFunNames.SliceName, 
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.Int32,
            VarType.Int32,
            VarType.Int32)
        {
        }

        public override object Calc(object[] args)
        {
            var start = args.Get<int>(1);
            if(start<0)
                throw new FunRuntimeException("Argument out of range");
            var end = args.Get<int>(2);
            if(end<0)
                throw new FunRuntimeException("Argument out of range");
            if(end!=0 && start>end)
                throw new FunRuntimeException("Start cannot be more than end");
            var step = args.Get<int>(3);
            if(step<0)
                throw new FunRuntimeException("Argument out of range");
            if (step == 0)
                step = 1;
            var arr = (IFunArray)args[0];
            return arr.Slice(start, end, step);
        }
    }
    
    public class SliceGenericFunctionDefenition : GenericFunctionBase
    {
        public SliceGenericFunctionDefenition() : base(CoreFunNames.SliceName, 
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.Int32,
            VarType.Int32)
        {
        }

        public override object Calc(object[] args)
        {
            var start = args.Get<int>(1);
            if(start<0)
                throw new FunRuntimeException("Argument out of range");

            var end = args.Get<int>(2);
            if(end<0)
                throw new FunRuntimeException("Argument out of range");
                
            if(end!=0 && start>end)
                throw new FunRuntimeException("Start cannot be more than end");
       
            var arr = (IFunArray)args[0];
            return arr.Slice(start, (end==int.MaxValue?null:(int?)end), null);
        }
    }
    
    public class GetGenericFunctionDefenition : GenericFunctionBase
    {
        public GetGenericFunctionDefenition() : base(CoreFunNames.GetElementName, 
            VarType.Generic(0),
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.Int32)
        {
        }

        public override object Calc(object[] args)
        {
            var index = args.Get<int>(1);
            if(index<0)
                throw new FunRuntimeException("Argument out of range");
                
            var arr = (IFunArray)args[0];
            var res =arr.GetElementOrNull(index);
            
            if(res==null)
                throw new FunRuntimeException("Argument out of range");
            return res;
        }
    }
    public class SetGenericFunctionDefenition : GenericFunctionBase
    {
        
        public SetGenericFunctionDefenition() : base("set", 
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.Int32,
            VarType.Generic(0))
        {
        }

        public override object Calc(object[] args)
        {
            var arr = (IFunArray)args[0];

            var index = args.Get<int>(1);
            if(index<0)
                throw new FunRuntimeException("Argument out of range");
            if(index>arr.Count+1)
                throw new FunRuntimeException("Argument out of range");
            var val = args.Get<int>(2);

            var newArr = arr.ToArray();
            newArr[index] = val;
            return new ImmutableFunArray(newArr);
        }
    }

    public class FindGenericFunctionDefenition : GenericFunctionBase
    {
        public FindGenericFunctionDefenition() : base("find", 
            VarType.Int32,
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.Generic(0))
        {
        }

        public override object Calc(object[] args)
        {
            var arr = (IFunArray)args[0];
            var factor = args[1] ;
            int i = 0;
            foreach (var element in arr)
            {
                if(TypeHelper.AreEqual(element, factor))
                    return i;
                i++;
            }
            return -1;
        }
    }

    public class ChunkGenericFunctionDefenition : GenericFunctionBase
    {
        public ChunkGenericFunctionDefenition() : base("chunk", 
            VarType.ArrayOf(VarType.ArrayOf(VarType.Generic(0))),
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.Int32)
        {
        }

        public override object Calc(object[] args)
        {
            var arr = (IFunArray)args[0];
            var chunkSize = args.Get<int>(1);
            if(chunkSize<=0)
                throw new FunRuntimeException("Chunk size is "+chunkSize+". It has to be positive");

            var res = arr
                .Select((x, i) => new {Index = i, Value = x})
                .GroupBy(x => x.Index / chunkSize)
                .Select(x => ImmutableFunArray.By(x.Select(v => v.Value)));
            return ImmutableFunArray.By(res);
        }
    }
    public class FlatGenericFunctionDefenition : GenericFunctionBase
    {
        public FlatGenericFunctionDefenition() : base("flat", 
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.ArrayOf(VarType.ArrayOf(VarType.Generic(0))))
        {
        }

        public override object Calc(object[] args)
        {
            var arr = (IFunArray)args[0];
            return ImmutableFunArray.By(arr.SelectMany(o => (IFunArray) o));
        }
    }
    public class ReduceGenericFunctionDefenition : GenericFunctionBase
    {
        public ReduceGenericFunctionDefenition() : base("reduce", 
            VarType.Generic(0),
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.Fun(VarType.Generic(0), VarType.Generic(0), VarType.Generic(0)))
        {
        }

        public override object Calc(object[] args)
        {
            var arr = (IFunArray)args[0];
            if(arr.Count==0)
                throw new FunRuntimeException("Input array is empty");
            
            var fold = args[1] as FunctionBase;
            
            var res = arr.Aggregate((a,b)=>fold.Calc(new []{a,b}));
            return res; 
        }
    }
   
    
    public class ReduceWithDefaultsGenericFunctionDefenition : GenericFunctionBase
    {
        public ReduceWithDefaultsGenericFunctionDefenition() : base("reduce", 
            VarType.Generic(1),
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.Generic(1),
            VarType.Fun(VarType.Generic(1), VarType.Generic(1), VarType.Generic(0)))
        {
        }

        public override object Calc(object[] args)
        {
            var arr = (IFunArray)args[0];
            var defaultValue = args[1];
            var fold = args[2] as FunctionBase;
            
            var res = arr.Aggregate(defaultValue, (a,b)=>fold.Calc(new []{a,b}));
            return res; 
        }
    }
    public class UniteGenericFunctionDefenition : GenericFunctionBase
    {
        public UniteGenericFunctionDefenition() : base("unite", 
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.ArrayOf(VarType.Generic(0)))
        {
        }

        public override object Calc(object[] args)
        {
            var arr1 = (IFunArray)args[0];
            var arr2 = (IFunArray)args[1];
            return ImmutableFunArray.By(arr1.Union(arr2));
        }
    }
    public class UniqueGenericFunctionDefenition : GenericFunctionBase
    {
        public UniqueGenericFunctionDefenition() : base("unique", 
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.ArrayOf(VarType.Generic(0)))
        {
        }

        public override object Calc(object[] args)
        {
            var arr1 = (IFunArray)args[0];
            var arr2 = (IFunArray)args[1];
            return ImmutableFunArray.By(arr1.Except(arr2).Concat(arr2.Except(arr1)));
        }
    }
    public class IntersectGenericFunctionDefenition : GenericFunctionBase
    {
        public IntersectGenericFunctionDefenition() : base("intersect", 
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.ArrayOf(VarType.Generic(0)))
        {
        }

        public override object Calc(object[] args)
        {
            var arr1 = (IFunArray)args[0];
            var arr2 = (IFunArray)args[1];
            return ImmutableFunArray.By(arr1.Intersect(arr2));
        }
    }
    public class ConcatArraysGenericFunctionDefenition : GenericFunctionBase
    {
        public ConcatArraysGenericFunctionDefenition(string name) : base(name, 
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.ArrayOf(VarType.Generic(0)))
        {
        }

        public override object Calc(object[] args)
        {
            var arr1 = (IFunArray)args[0];
            var arr2 = (IFunArray)args[1];
            var res = ImmutableFunArray.By(arr1.Concat(arr2));
            return res;
        }
    }
   
    public class SubstractArraysGenericFunctionDefenition : GenericFunctionBase
    {
        public SubstractArraysGenericFunctionDefenition() : base("except", 
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.ArrayOf(VarType.Generic(0)))
        {
        }

        public override object Calc(object[] args)
        {
            var arr1 = (IFunArray)args[0];
            var arr2 = (IFunArray)args[1];
            return ImmutableFunArray.By(arr1.Except(arr2));
        }
    }
  
    public class AnyGenericFunctionDefenition : GenericFunctionBase
    {
        public AnyGenericFunctionDefenition() : base("any", 
            VarType.Bool,
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.Fun(VarType.Bool, VarType.Generic(0)))
        {
        }

        public override object Calc(object[] args)
        {
            var arr = (IFunArray)args[0];
            var filter = args[1] as FunctionBase;

            return arr.Any(a => (bool) filter.Calc(new[] {a}));
        }
    }
    public class AllGenericFunctionDefenition : GenericFunctionBase
    {
        public AllGenericFunctionDefenition() : base("all", 
            VarType.Bool,
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.Fun(VarType.Bool, VarType.Generic(0)))
        {
        }

        public override object Calc(object[] args)
        {
            var arr = (IFunArray)args[0];
            var filter = args[1] as FunctionBase;

            return arr.All(a => (bool) filter.Calc(new[] {a}));
        }
    }
    public class FilterGenericFunctionDefenition : GenericFunctionBase
    {
        public FilterGenericFunctionDefenition() : base("filter", 
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.Fun(VarType.Bool, VarType.Generic(0)))
        {
        }

        public override object Calc(object[] args)
        {
            var arr = (IFunArray)args[0];
            var filter = args[1] as FunctionBase;
            
            return ImmutableFunArray.By(arr.Where(a=>(bool)filter.Calc(new []{a})));
        }
    }
    
    public class RepeatGenericFunctionDefenition : GenericFunctionBase
    {
        public RepeatGenericFunctionDefenition() : base("repeat",
            VarType.ArrayOf(VarType.Generic(0)), 
            VarType.Generic(0), 
            VarType.Int32)
        {
        }

        public override object Calc(object[] args)
        {
            var first = args[0];
            return ImmutableFunArray.By(Enumerable.Repeat(first, args.Get<int>(1)));
        }
    }
    
    public class ReverseGenericFunctionDefenition: GenericFunctionBase
    {
        public ReverseGenericFunctionDefenition() : base("reverse", 
            VarType.ArrayOf(VarType.Generic(0)), 
            VarType.ArrayOf(VarType.Generic(0)))
        {
        }

        public override object Calc(object[] args)
        {
            var arr  = (IFunArray) args[0];
            return ImmutableFunArray.By(arr.Reverse());
        }
    }
    public class TakeGenericFunctionDefenition: GenericFunctionBase
    {
        public TakeGenericFunctionDefenition() : base("take", 
            VarType.ArrayOf(VarType.Generic(0)), 
            VarType.ArrayOf(VarType.Generic(0)), 
            VarType.Int32)
        {
        }

        public override object Calc(object[] args)
        {
            return ((IFunArray)args[0]).Slice(null,args.Get<int>(1)-1,1);
        }
    }
    public class SkipGenericFunctionDefenition: GenericFunctionBase
    {
        public SkipGenericFunctionDefenition() : base("skip", 
            VarType.ArrayOf(VarType.Generic(0)), 
            VarType.ArrayOf(VarType.Generic(0)), 
            VarType.Int32)
        {
        }

        public override object Calc(object[] args)
        {
            return ((IFunArray)args[0]).Slice(args.Get<int>(1),null,1);
        }
    }

}