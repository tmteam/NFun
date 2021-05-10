using System;
using NFun.Exceptions;
using NFun.TestTools;
using NFun.Types;
using NUnit.Framework;

namespace NFun.Tests
{
    public class OLD_AprioriTypesTest
    {
        [Test]
        public void AprioriInputSpecified_CalcsWithCorrectType()
        {
            var runtime = Funny.Hardcore
                .WithApriori<string>("x")
                .Build("y = x");
                
            var res = runtime.Calculate(VarVal.New("x","test"));
            res.OLD_AssertReturns(VarVal.New("y","test"));
            Assert.AreEqual(VarType.Text, runtime.Inputs[0].Type,"input");
            Assert.AreEqual(VarType.Text, runtime.Outputs[0].Type,"output");
        }
        
        [Test]
        public void InputVariableSpecifiedAndDoesNotConflict_AprioriInputCalcs()
        {
            var runtime = FunBuilder
                .With("x:text; y = x")
                .WithApriori("x", VarType.Text)
                .Build();
            var res = runtime.Calculate(VarVal.New("x","test"));
            res.OLD_AssertReturns(VarVal.New("y","test"));
            Assert.AreEqual(VarType.Text, runtime.Inputs[0].Type,"input");
            Assert.AreEqual(VarType.Text, runtime.Outputs[0].Type,"output");
        }
        
        [Test]
        public void OutputVarSpecifiedAndDoesNotConflict_AprioriOutputCalcs()
        {
            var runtime = FunBuilder
                .With("y:text = x")
                .WithApriori("y", VarType.Text)
                .Build();
            var res = runtime.Calculate(VarVal.New("x","test"));
            res.OLD_AssertReturns(VarVal.New("y","test"));
            Assert.AreEqual(VarType.Text, runtime.Inputs[0].Type,"input");
            Assert.AreEqual(VarType.Text, runtime.Outputs[0].Type,"output");

        }
        
        
        [Test]
        public void AprioriVariableDoesNotUsed_Calculates()
        {
            var runtime = FunBuilder
                .With("x:text; y = x")
                .WithApriori("alfa", VarType.Text)
                .Build();
            var res = runtime.Calculate(VarVal.New("x","test"));
            res.OLD_AssertReturns(VarVal.New("y","test"));
            Assert.AreEqual(VarType.Text, runtime.Inputs[0].Type,"input");
            Assert.AreEqual(VarType.Text, runtime.Outputs[0].Type,"output");
        }
        
        [Test]
        public void OutputVarSpecifiedWithDifferentAprioriType_throws() =>
            AssertFunParseError(
                FunBuilder.With("y:int = x").WithApriori("y", VarType.Text));

        [Test]
        public void InputVarSpecifiedWithAprioriOutputConflict_throws() =>
            AssertFunParseError(
                FunBuilder.With("x:int; y = x").WithApriori("y", VarType.Text));
        
        [Test]
        public void OutputVarSpecifiedHasInputNameWithSameName_throws() =>
            AssertFunParseError(
                FunBuilder.With("y:int = x").WithApriori("x", VarType.Text));
        
        [Test]
        public void OutputVarSpecifiedHasInputAprioriType_Calculates()
            => Assert.DoesNotThrow(()=> FunBuilder.With("y:text = x").WithApriori("x", VarType.Text).Build());

        
        [Test]
        public void SpecifyTwoSameOutputs_throws()  
        {
            var builder = FunBuilder.With("x = y").WithApriori("y", VarType.Text);
            Assert.Throws<ArgumentException>(() => builder.WithApriori("y", VarType.Text));
        }
        
        [Test]
        public void SpecifyTwoSameInputs_throws()
        {
            var builder = FunBuilder.With("x = y").WithApriori("x", VarType.Bool);
            Assert.Throws<ArgumentException>(() => builder.WithApriori("x", VarType.Text));
        }
        
        
        private void AssertFunParseError(IFunBuilder builder) => Assert.Throws<FunParseException>(() => builder.Build());
    }
}