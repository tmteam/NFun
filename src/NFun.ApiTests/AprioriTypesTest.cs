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
            Assert.AreEqual(VarType.Text, runtime.GetVariable("input").Type);
            Assert.AreEqual(VarType.Text, runtime.GetVariable("output").Type);
        }
        
        [Test]
        public void InputVariableSpecifiedAndDoesNotConflict_AprioriInputCalcs()
        {
            var runtime = Funny.Hardcore
                .WithApriori<string>("x")
                .Build("x:text; y = x");
            var res = runtime.Calc("x","test");
            res.AssertReturns("y","test");
            Assert.AreEqual(VarType.Text, runtime.Inputs[0].Type,"input");
            Assert.AreEqual(VarType.Text, runtime.Outputs[0].Type,"output");
        }
        
        [Test]
        public void OutputVarSpecifiedAndDoesNotConflict_AprioriOutputCalcs()
        {
            var runtime = Funny.Hardcore
                    .WithApriori<string>("y")
                    .Build("x:text; y = x");
                    
            var res = runtime.Calc("x","test");
            res.AssertReturns("y","test");
            Assert.AreEqual(VarType.Text, runtime.Inputs[0].Type,"input");
            Assert.AreEqual(VarType.Text, runtime.Outputs[0].Type,"output");

        }
        
        
        [Test]
        public void AprioriVariableDoesNotUsed_Calculates()
        {
            var runtime = Funny.Hardcore
                .WithApriori<string>("alfa")
                .Build("x:text; y = x");                

            var res = runtime.Calc("x","test");
            res.AssertReturns("y","test");
            Assert.AreEqual(VarType.Text, runtime.Inputs[0].Type,"input");
            Assert.AreEqual(VarType.Text, runtime.Outputs[0].Type,"output");
        }

        [Test]
        public void OutputVarSpecifiedWithDifferentAprioriType_throws() =>
            TestHelper.AssertObviousFailsOnParse(() =>
                Funny.Hardcore.WithApriori("y", VarType.Text).Build("y:int = x"));

        [Test]
        public void InputVarSpecifiedWithAprioriOutputConflict_throws() =>
            TestHelper.AssertObviousFailsOnParse(() =>
                Funny.Hardcore.WithApriori("y", VarType.Text).Build("x:int; y = x"));

        [Test]
        public void OutputVarSpecifiedHasInputNameWithSameName_throws() =>
            TestHelper.AssertObviousFailsOnParse(() =>
                Funny.Hardcore.WithApriori("x", VarType.Text).Build("x:int; y = x"));
            
        [Test]
        public void OutputVarSpecifiedHasInputAprioriType_Calculates()
            => Assert.DoesNotThrow(()=> Funny.Hardcore.WithApriori("x", VarType.Text).Build("y:text = x"));

        
        [Test]
        public void SpecifyTwoSameOutputs_throws()  
        {
            var builder = Funny.Hardcore.WithApriori("y", VarType.Text);
            Assert.Throws<ArgumentException>(() => builder.WithApriori("y", VarType.Text));
        }
        
        [Test]
        public void SpecifyTwoSameInputs_throws()
        {
            var builder = Funny.Hardcore.WithApriori("x", VarType.Bool);
            Assert.Throws<ArgumentException>(() => builder.WithApriori("x", VarType.Text));
        }
    }
}