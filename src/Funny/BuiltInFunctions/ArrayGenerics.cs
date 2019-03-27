using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Funny.Interpritation.Functions;
using Funny.Runtime;
using Funny.Types;

namespace Funny.BuiltInFunctions
{
    public class SliceWithStepGenericFunctionDefenition : GenericFunctionBase
    {
        public SliceWithStepGenericFunctionDefenition() : base(CoreFunNames.SliceName, 
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.Int,
            VarType.Int,
            VarType.Int)
        {
        }

        public override object Calc(object[] args)
        {
            var start = (int)args[1];
            if(start<0)
                throw new FunRuntimeException("Argument out of range");
            var end = (int)args[2];
            if(end<0)
                throw new FunRuntimeException("Argument out of range");
            if(end!=0 && start>end)
                throw new FunRuntimeException("Start cannot be more than end");
            var step = (int) args[3];
            if(step<0)
                throw new FunRuntimeException("Argument out of range");
            if (step == 0)
                step = 1;
            var enumerable = args[0] as IEnumerable;
            var obj = new List<object>();
            
            int i = 0;
            foreach (var val in enumerable) {
                var actual = i - start;
                if (actual>=0 &&  actual % step == 0)
                {
                    if(end==0 || end>= actual)
                        obj.Add(val);
                }
                i++;
            }
            return obj;
        }
    }
    
    public class SliceGenericFunctionDefenition : GenericFunctionBase
    {
        public SliceGenericFunctionDefenition() : base(CoreFunNames.SliceName, 
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.Int,
            VarType.Int)
        {
        }

        public override object Calc(object[] args)
        {
            var start = (int)args[1];
            if(start<0)
                throw new FunRuntimeException("Argument out of range");

            var end = (int)args[2];
            if(end<0)
                throw new FunRuntimeException("Argument out of range");
                
            if(end!=0 && start>end)
                throw new FunRuntimeException("Start cannot be more than end");
       
            var query = (args[0] as IEnumerable).Cast<Object>().Skip(start);
            if (end != 0)
                query = query.Take(end - start+1);
            return query.ToArray();
        }
    }
    public class GetGenericFunctionDefenition : GenericFunctionBase
    {
        
        public GetGenericFunctionDefenition() : base(CoreFunNames.GetElementName, 
            VarType.Generic(0),
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.Int)
        {
        }

        public override object Calc(object[] args)
        {
            var index = (int)args[1];
            if(index<0)
                throw new FunRuntimeException("Argument out of range");
                
            if (args[0] is IList list)
            {
                if(list.Count<= index)
                    throw new FunRuntimeException("Argument out of range");
                return list[index];
            }
            
            var arr = (args[0] as IEnumerable).Cast<Object>();
            var res =  arr.ElementAtOrDefault(index);
            if(res==null)
                throw new FunRuntimeException("Argument out of range");
            return res;
        }
    }
    public class FoldGenericFunctionDefenition : GenericFunctionBase
    {
        public FoldGenericFunctionDefenition() : base("fold", 
            VarType.Generic(0),
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.Fun(VarType.Generic(0), VarType.Generic(0), VarType.Generic(0)))
        {
        }

        public override object Calc(object[] args)
        {
            var arr = (args[0] as IEnumerable).Cast<object>();
            var fold = args[1] as FunctionBase;
            
            var res = arr.Aggregate((a,b)=>fold.Calc(new []{a,b}));
            return res; 
        }
    }
    public class AmpersantGenericFunctionDefenition : GenericFunctionBase
    {
        public AmpersantGenericFunctionDefenition() : base(CoreFunNames.AmpersantName, 
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.ArrayOf(VarType.Generic(0)))
        {
        }

        public override object Calc(object[] args)
        {
            var arr1 = (args[0] as IEnumerable).Cast<object>();
            var arr2 = (args[1] as IEnumerable).Cast<object>();
            return arr1.Intersect(arr2).ToArray();
        }
    }
    public class AddArraysGenericFunctionDefenition : GenericFunctionBase
    {
        public AddArraysGenericFunctionDefenition() : base(CoreFunNames.AddName, 
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.ArrayOf(VarType.Generic(0)))
        {
        }

        public override object Calc(object[] args)
        {
            var arr1 = (args[0] as IEnumerable).Cast<object>();
            var arr2 = (args[1] as IEnumerable).Cast<object>();
            return arr1.Concat(arr2).ToArray();
        }
    }
    
    public class SubstractArraysGenericFunctionDefenition : GenericFunctionBase
    {
        public SubstractArraysGenericFunctionDefenition() : base(CoreFunNames.SubstractName, 
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.ArrayOf(VarType.Generic(0)))
        {
        }

        public override object Calc(object[] args)
        {
            var arr1 = (args[0] as IEnumerable).Cast<object>();
            var arr2 = (args[1] as IEnumerable).Cast<object>();
            return arr1.Except(arr2).ToArray();
        }
    }
    public class MapGenericFunctionDefenition : GenericFunctionBase
    {
        public MapGenericFunctionDefenition() : base("map", 
            VarType.ArrayOf(VarType.Generic(1)),
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.Fun(VarType.Generic(1), VarType.Generic(0)))
        {
        }

        public override object Calc(object[] args)
        {
            var arr = (args[0] as IEnumerable).Cast<object>();
            var map = args[1] as FunctionBase;
            
            var res = arr.Select(a=>map.Calc(new []{a})).ToArray();
            return res; 
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
            var arr = (args[0] as IEnumerable).Cast<object>();
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
            var arr = (args[0] as IEnumerable).Cast<object>();
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
            var arr = (args[0] as IEnumerable).Cast<object>();
            var filter = args[1] as FunctionBase;
            
            var res = arr.Where(a=>(bool)filter.Calc(new []{a})).ToArray();
            return res; 
        }
    }
    
    public class RepeatGenericFunctionDefenition : GenericFunctionBase
    {
        public RepeatGenericFunctionDefenition() : base("repeat",
            VarType.ArrayOf(VarType.Generic(0)), 
            VarType.Generic(0), 
            VarType.Int)
        {
        }

        public override object Calc(object[] args)
        {
            var first = args[0];
            return Enumerable.Repeat(first, (int) args[1]);
        }
    }
    public class ConcatGenericFunctionDefenition: GenericFunctionBase
    {
        public ConcatGenericFunctionDefenition() : base("concat", 
            VarType.ArrayOf(VarType.Generic(0)), 
            VarType.ArrayOf(VarType.Generic(0)), 
            VarType.ArrayOf(VarType.Generic(0)))
        {
        }

        public override object Calc(object[] args)
        {
            var arrLeft = (args[0] as IEnumerable).Cast<object>();
            var arrRight = (args[1] as IEnumerable).Cast<object>();

            return arrLeft.Concat(arrRight);
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
            var arr = (args[0] as IEnumerable).Cast<object>();
            return arr.Reverse().ToArray();
        }
    }
    public class TakeGenericFunctionDefenition: GenericFunctionBase
    {
        public TakeGenericFunctionDefenition() : base("take", 
            VarType.ArrayOf(VarType.Generic(0)), 
            VarType.ArrayOf(VarType.Generic(0)), 
            VarType.Int)
        {
        }

        public override object Calc(object[] args)
        {
            return (args[0] as IEnumerable).Cast<object>().Take((int) args[1]);
        }
    }
    
    public class SkipGenericFunctionDefenition: GenericFunctionBase
    {
        public SkipGenericFunctionDefenition() : base("skip", 
            VarType.ArrayOf(VarType.Generic(0)), 
            VarType.ArrayOf(VarType.Generic(0)), 
            VarType.Int)
        {
        }

        public override object Calc(object[] args)
        {
            return (args[0] as IEnumerable).Cast<object>().Skip((int) args[1]);
        }
    }

}