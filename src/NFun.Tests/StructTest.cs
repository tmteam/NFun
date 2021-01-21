using System.Collections.Generic;
using NFun;
using NFun.Runtime;
using NFun.Runtime.Arrays;
using NFun.Types;
using NUnit.Framework;

namespace Funny.Tests
{
    public class StructTest
    {
        [Test]
        public void SingleFieldStructInitialization() =>
            FunBuilder
                .Build("y = @{a = 1.0}")
                .Calculate()
                .AssertReturns(VarVal.New("y", FunnyStruct.Create("a",1.0)));

        [Test]
        public void TwoFieldStructInitialization() =>
            FunBuilder
                .Build("y = @{a = 1.0; b ='vasa'}")
                .Calculate()
                .AssertReturns(VarVal.New("y", FunnyStruct.Create("a", 1.0, "b", "vasa".AsFunText()))); 
        
        [Test]
        public void ThreeFieldStructInitializationWithCalculation() =>
            FunBuilder
                .Build("y = @{a = 1.0; b ='vasa'; c = 12*5.0}")
                .Calculate()
                .AssertReturns(VarVal.New("y", new FunnyStruct(new Dictionary<string,object> {
                    {"a",1.0},
                    {"b","vasa".AsFunText()},
                    {"c",60.0},
                })));
        
        [Test]
        public void StructInitializationWithCalculationAndNestedStruct() =>
            FunBuilder
                .Build("y = @{" +
                       "  a = true;" +
                       "  b = @{ " +
                       "           c=[1.0,2.0,3.0];" +
                       "           d=false" +
                       "        }" +
                       "  c = 12*5.0" +
                       "}")
                .Calculate()
                .AssertReturns(VarVal.New("y",new FunnyStruct(new Dictionary<string,object> {
                    {"a",true},
                    {"b",new FunnyStruct(new Dictionary<string,object> {
                        {"c", new ImmutableFunArray(new []{1.0,2.0,3.0})},
                        {"d",false}
                    })},
                    {"c",60.0}
                })));
        
        
        [Test]
        public void SingleFieldAccess() =>
            FunBuilder
                .Build("y:int = a.age")
                .Calculate(VarVal.New("a",FunnyStruct.Create("age",42),VarType.StructOf("age",VarType.Int32)))
                .AssertReturns(VarVal.New("y",42));
        
        [Test]
        public void AccessToNestedFieldsWithExplicitTi() =>
            FunBuilder
                .Build("y:int = @{age = 42; name = 'vasa'}.age")
                .Calculate()
                .AssertReturns(VarVal.New("y",42));
        [Test]
        public void AccessToNestedFields() =>
            FunBuilder
                .Build("y = @{age = 42; name = 'vasa'}.age")
                .Calculate()
                .AssertReturns(VarVal.New("y",42.0));
        [Test]
        public void AccessToNestedFields2() =>
            FunBuilder
                .Build("y = @{age = 42; name = 'vasa'}.name")
                .Calculate()
                .AssertReturns(VarVal.New("y","vasa"));
        
        [Test]
        public void AccessToNestedFieldsWithExplicitTi2() =>
            FunBuilder
                .Build("y:anything = @{age = 42; name = 'vasa'}.name")
                .Calculate()
                .AssertReturns(VarVal.New("y",(object)"vasa"));
        
        
        [Test]
        public void TwoFieldsAccess()
        {
            var result =FunBuilder
                .Build("y1:int = a.age; y2:real = a.size")
                .Calculate(VarVal.New("a",
                    new FunnyStruct(new Dictionary<string, object> {
                        {"age", 42}, {"size", 1.1}
                    }), VarType.StructOf(new Dictionary<string, VarType> {
                        {"age", VarType.Int32}, {"size", VarType.Real}
                    })));
            result.AssertHas(VarVal.New("y1", 42));
            result.AssertHas(VarVal.New("y2", 1.1));
        }
        
        [Test]
        public void ThreeFieldsAccess()
        {
            var result =FunBuilder
                .Build("agei:int = a.age; sizer = a.size+12.0; name = a.name")
                .Calculate(VarVal.New("a",
                    new FunnyStruct(new Dictionary<string, object>
                    {
                        {"age", 42}, {"size", 1.1}, {"name", "vasa"}
                    }), VarType.StructOf(new Dictionary<string, VarType>
                    {
                        {"age", VarType.Int32}, {"size", VarType.Real}, {"name", VarType.Anything}
                    })));
            result.AssertHas(VarVal.New("agei", 42));
            result.AssertHas(VarVal.New("sizer", 13.1));
            result.AssertHas(new VarVal("name", "vasa", VarType.Anything));
        }
        
        /*
         *TODO
         * 
a = @{b = 1; c=2}
y = a.b + a.c

a = @{b = x; c=2}
y = a.b + a.c

a = @{b = 1; c=x}
y = a.b + a.c

a = @{b = x; c=x}
y = a.b + a.c

a = @{b = x; c=x}
b = @{d = a; e = a.x}
y = b.d.x + e.x


a = @{b= [1,2,3]; c = 'vasa'}
d = @{e = 'lala'; f = a}
y = d.f.b[1]

a = @{b= [x,2,3]; c = 'vasa'}
d = @{e = 'lala'; f = a}
y = d.f.b[0]


a = [@{age = 42; name = 'vasa'}, @{age = 21; name = 'peta'}]
y = a[1].age
         */
    }
}