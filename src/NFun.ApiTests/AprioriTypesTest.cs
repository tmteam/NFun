using System;
using NFun.TestTools;
using NFun.Types;
using NUnit.Framework;

namespace NFun.ApiTests
{
    public class AprioriTypesTest
    {
        [Test]
        public void AprioriInputSpecified_CalcsWithCorrectType()
        {
            var runtime = Funny.Hardcore
                .WithApriori<string>("x")
                .Build("y = x");
                
            var res = runtime.Calc("x","test");
            res.AssertReturns("y","test");
            Assert.AreEqual(FunnyType.Text, runtime.GetVariable("x").Type);
            Assert.AreEqual(FunnyType.Text, runtime.GetVariable("y").Type);
        }
        
        [Test]
        public void InputVariableSpecifiedAndDoesNotConflict_AprioriInputCalcs()
        {
            var runtime = Funny.Hardcore
                .WithApriori<string>("x")
                .Build("x:text; y = x");
            var res = runtime.Calc("x","test");
            res.AssertReturns("y","test");
            Assert.AreEqual(FunnyType.Text, runtime.Inputs[0].Type,"input");
            Assert.AreEqual(FunnyType.Text, runtime.Outputs[0].Type,"output");
        }
        
        [Test]
        public void OutputVarSpecifiedAndDoesNotConflict_AprioriOutputCalcs()
        {
            var runtime = Funny.Hardcore
                    .WithApriori<string>("y")
                    .Build("x:text; y = x");
                    
            var res = runtime.Calc("x","test");
            res.AssertReturns("y","test");
            Assert.AreEqual(FunnyType.Text, runtime.Inputs[0].Type,"input");
            Assert.AreEqual(FunnyType.Text, runtime.Outputs[0].Type,"output");

        }
        
        
        [Test]
        public void AprioriVariableDoesNotUsed_Calculates()
        {
            var runtime = Funny.Hardcore
                .WithApriori<string>("alfa")
                .Build("x:text; y = x");                

            var res = runtime.Calc("x","test");
            res.AssertReturns("y","test");
            Assert.AreEqual(FunnyType.Text, runtime.Inputs[0].Type,"input");
            Assert.AreEqual(FunnyType.Text, runtime.Outputs[0].Type,"output");
        }

        [Test]
        public void OutputVarSpecifiedWithDifferentAprioriType_throws() =>
            TestHelper.AssertObviousFailsOnParse(() =>
                Funny.Hardcore.WithApriori("y", FunnyType.Text).Build("y:int = x"));

        [Test]
        public void InputVarSpecifiedWithAprioriOutputConflict_throws() =>
            TestHelper.AssertObviousFailsOnParse(() =>
                Funny.Hardcore.WithApriori("y", FunnyType.Text).Build("x:int; y = x"));

        [Test]
        public void OutputVarSpecifiedHasInputNameWithSameName_throws() =>
            TestHelper.AssertObviousFailsOnParse(() =>
                Funny.Hardcore.WithApriori("x", FunnyType.Text).Build("x:int; y = x"));
            
        [Test]
        public void OutputVarSpecifiedHasInputAprioriType_Calculates()
            => Assert.DoesNotThrow(()=> Funny.Hardcore.WithApriori("x", FunnyType.Text).Build("y:text = x"));

        
        [Test]
        public void SpecifyTwoSameOutputs_throws()  
        {
            var builder = Funny.Hardcore.WithApriori("y", FunnyType.Text);
            Assert.Throws<ArgumentException>(() => builder.WithApriori("y", FunnyType.Text));
        }
        
        [Test]
        public void SpecifyTwoSameInputs_throws()
        {
            var builder = Funny.Hardcore.WithApriori("x", FunnyType.Bool);
            Assert.Throws<ArgumentException>(() => builder.WithApriori("x", FunnyType.Text));
        }
    }
}