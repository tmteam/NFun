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
        public void ConstAccessNested() =>
            FunBuilder
                .Build("y = @{ b = 'foo'}.b")
                .Calculate()
                .AssertReturns(VarVal.New("y", "foo"));
        
        [Test]
        public void ConstAccessNestedComposite() =>
            FunBuilder
                .Build("y = @{ b = [1,2,3]}.b[1]")
                .Calculate()
                .AssertReturns(VarVal.New("y", 2.0));
        
        [Test]
        public void ConstAccessDoubleNestedComposite() =>
            FunBuilder
                .Build("y = @{ a1 = @{b2 = [1,2,3]}}.a1.b2[1]")
                .Calculate()
                .AssertReturns(VarVal.New("y", 2.0));
        

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

        [Test]
        public void ConstantAccessCreated() =>
            FunBuilder
                .Build("a = @{b = 1; c=2}; y = a.b + a.c")
                .Calculate()
                .AssertHas(VarVal.New("y", 3.0));
        
        [Test]
        public void VarAccessCreated() =>
            FunBuilder
                .Build("a = @{b = x; c=2}; y = a.b + a.c")
                .Calculate(VarVal.New("x",42.0))
                .AssertHas(VarVal.New("y", 44.0));
        [Test]
        public void VarAccessCreatedInverted() =>
            FunBuilder
                .Build("a = @{b = 55; c=x}; y = a.b + a.c")
                .Calculate(VarVal.New("x",42.0))
                .AssertHas(VarVal.New("y", 97.0));
        
        [Test]
        public void VarTwinAccessCreated() =>
            FunBuilder
                .Build("a = @{b = x; c=x}; y = a.b + a.c")
                .Calculate(VarVal.New("x",42.0))
                .AssertHas(VarVal.New("y", 84.0));
        
        [Test]
        public void ConstantAccessNestedCreated() =>
            FunBuilder
                .Build("a = @{b = 24; c=25}; " +
                       "b = @{d = a; e = a.c; f = 3}; " +
                       "y = b.d.b + b.e + b.f")
                .Calculate()
                .AssertHas(VarVal.New("y", 52.0));

        [Test]
        public void VarAccessNestedCreated() =>
            FunBuilder
                .Build("a = @{b = x; c=x}; " +
                       "b = @{d = a; e = a.c; f = 3}; " +
                       "y = b.d.b + b.e + b.f")
                .Calculate(VarVal.New("x",42.0))
                .AssertHas(VarVal.New("y", 87.0));
        
        [Test]
        public void ConstAccessNestedCreatedComposite() =>
            FunBuilder
                .Build( "a = @{b= [1,2,3]};" +
                        "y = a.b[1]")
                .Calculate()
                .AssertHas(VarVal.New("y", 2.0));
        
        [Test]
        public void ConstAccessDoubleNestedCreatedComposite() =>
            FunBuilder
                .Build( "a = @{b= [1,2,3]; c = 'vasa'};" +
                        "d = @{e = 'lala'; f = a};" +
                        "y = d.f.b[1]")
                .Calculate()
                .AssertHas(VarVal.New("y", 2.0));
        
        [Test]
        public void VarAccessNestedCreatedComposite() =>
            FunBuilder
                .Build( "a = @{b= [x,2,3]};" +
                        "y = -a.b[0]")
                .Calculate(VarVal.New("x",42.0))
                .AssertHas(VarVal.New("y", -42.0));
        
        [Test]
        public void VarAccessDoubleNestedCreatedComposite() =>
            FunBuilder
                .Build( "a = @{b= [x,2,3]; c = 'vasa'};" +
                        "d = @{e = 'lala'; f = a};" +
                        "y = -d.f.b[0]")
                .Calculate(VarVal.New("x",42.0))
                .AssertHas(VarVal.New("y", -42.0));
        
        [Test]
        public void ConstAccessArrayOfStructs() =>
            FunBuilder
                .Build( "a = [@{age = 42; name = 'vasa'}, @{age = 21; name = 'peta'}];" +
                        "y = a[1].name;")
                .Calculate()
                .AssertHas(VarVal.New("y","peta"));
    }
}