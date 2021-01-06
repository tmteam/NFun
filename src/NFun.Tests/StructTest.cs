using System.Collections.Generic;
using NFun;
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
                .AssertReturns(VarVal.New("y", new Dictionary<string,object>{{"a",1.0}}));

        [Test]
        public void TwoFieldStructInitialization() =>
            FunBuilder
                .Build("y = @{a = 1.0; b ='vasa'}")
                .Calculate()
                .AssertReturns(VarVal.New("y", new Dictionary<string,object>
                {
                    {"a",1.0},
                    {"b","vasa"}
                }));
        
        [Test]
        public void ThreeFieldStructInitializationWithCalculation() =>
            FunBuilder
                .Build("y = @{a = 1.0; b ='vasa'; c = 12*5.0}")
                .Calculate()
                .AssertReturns(VarVal.New("y", new Dictionary<string,object> {
                    {"a",1.0},
                    {"b","vasa"},
                    {"c",60.0},
                }));
        
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
                .AssertReturns(VarVal.New("y", new Dictionary<string,object> {
                    {"a",true},
                    {"b",new Dictionary<string,object>
                    {
                        {"c",new []{1.0,2.0,3.0}},
                        {"d",false}
                    }},
                    {"c",60.0}
                }));
    }
}