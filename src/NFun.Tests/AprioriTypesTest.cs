using System;
using NFun;
using NFun.Exceptions;
using NFun.Types;
using NUnit.Framework;

namespace Funny.Tests
{
    public class AprioriTypesTest
    {
        [Test]
        public void AprioriInputSpecified_CalcsWithCorrectType()
        {
            var runtime = FunBuilder
                .With("y = x")
                .WithAprioriInput("x", VarType.Text)
                .Build();
            var res = runtime.Calculate(VarVal.New("x","test"));
            res.AssertReturns(VarVal.New("y","test"));
            Assert.AreEqual(VarType.Text, runtime.Inputs[0].Type,"input");
            Assert.AreEqual(VarType.Text, runtime.Outputs[0].Type,"output");
        }
        
        [Test]
        public void InputVariableSpecifiedAndDoesNotConflict_AprioriInputCalcs()
        {
            var runtime = FunBuilder
                .With("x:text; y = x")
                .WithAprioriInput("x", VarType.Text)
                .Build();
            var res = runtime.Calculate(VarVal.New("x","test"));
            res.AssertReturns(VarVal.New("y","test"));
            Assert.AreEqual(VarType.Text, runtime.Inputs[0].Type,"input");
            Assert.AreEqual(VarType.Text, runtime.Outputs[0].Type,"output");
        }
        
        [Test]
        public void OutputVarSpecifiedAndDoesNotConflict_AprioriOutputCalcs()
        {
            var runtime = FunBuilder
                .With("y:text = x")
                .WithAprioriOutput("y", VarType.Text)
                .Build();
            var res = runtime.Calculate(VarVal.New("x","test"));
            res.AssertReturns(VarVal.New("y","test"));
            Assert.AreEqual(VarType.Text, runtime.Inputs[0].Type,"input");
            Assert.AreEqual(VarType.Text, runtime.Outputs[0].Type,"output");

        }
        
        [Test]
        public void InputAprioriDoesNotUsed_Calculates()
        {
            var runtime = FunBuilder
                .With("y:text = x")
                .WithAprioriInput("omega", VarType.Text)
                .Build();
            var res = runtime.Calculate(VarVal.New("x","test"));
            res.AssertReturns(VarVal.New("y","test"));
            Assert.AreEqual(VarType.Text, runtime.Inputs[0].Type,"input");
            Assert.AreEqual(VarType.Text, runtime.Outputs[0].Type,"output");

        }
        
        [Test]
        public void OutputVariableDoesNotUsed_Calculates()
        {
            var runtime = FunBuilder
                .With("x:text; y = x")
                .WithAprioriOutput("alfa", VarType.Text)
                .Build();
            var res = runtime.Calculate(VarVal.New("x","test"));
            res.AssertReturns(VarVal.New("y","test"));
            Assert.AreEqual(VarType.Text, runtime.Inputs[0].Type,"input");
            Assert.AreEqual(VarType.Text, runtime.Outputs[0].Type,"output");
        }
        
        [Test]
        public void OutputVarSpecifiedWithDifferentAprioriType_throws() =>
            AssertFunParseError(
                FunBuilder.With("y:int = x").WithAprioriOutput("y", VarType.Text));

        [Test]
        public void InputVarSpecifiedWithAprioriOutputConflict_throws() =>
            AssertFunParseError(
                FunBuilder.With("x:int; y = x").WithAprioriOutput("y", VarType.Text));
        
        [Test]
        public void OutputVarSpecifiedHasInputNameWithSameName_throws() =>
            AssertFunParseError(
                FunBuilder.With("y:int = x").WithAprioriOutput("x", VarType.Text));
        
        [Test]
        public void OutputVarSpecifiedHasInputNameWithSameNameAndType_throws() =>
            AssertFunParseError(
                FunBuilder.With("y:text = x").WithAprioriOutput("x", VarType.Text));
        
        [Test]
        public void OutputVarSpecifiedHasInputNameWithSameWithoutType_throws() =>
            AssertFunParseError(
                FunBuilder.With("y = x").WithAprioriOutput("x", VarType.Int16));
        
        [Test]
        public void InputVarSpecifiedHasOutputNameWithSameName_throws() =>
            AssertFunParseError(
                FunBuilder.With("y:int = x").WithAprioriInput("y", VarType.Text));
        
        [Test]
        public void InputVarSpecifiedHasOutputNameWithSameNameWithoutType_throws() =>
            AssertFunParseError(
                FunBuilder.With("y = x").WithAprioriInput("y", VarType.Bool));
        [Test]
        public void SpecifyTwoSameInputs_throws()
        {
            var builder = FunBuilder.With("x = y").WithAprioriInput("x", VarType.Text);
            Assert.Throws<ArgumentException>(() => builder.WithAprioriInput("x", VarType.Text));
        }
        [Test]
        public void SpecifyTwoSameOutputs_throws()
        {
            var builder = FunBuilder.With("x = y").WithAprioriOutput("y", VarType.Text);
            Assert.Throws<ArgumentException>(() => builder.WithAprioriOutput("y", VarType.Text));
        }
        
        [Test]
        public void SpecifySameInputAndOutput_throws()
        {
            var builder = FunBuilder.With("z").WithAprioriInput("x", VarType.Text);
            Assert.Throws<ArgumentException>(() => builder.WithAprioriOutput("x", VarType.Text));
        }
        [Test]
        public void SpecifySameOutputAndInput_throws()
        {
            var builder = FunBuilder.With("z").WithAprioriOutput("y", VarType.Text);
            Assert.Throws<ArgumentException>(() => builder.WithAprioriInput("y", VarType.Text));
        }
        
        [Test]
        public void InputVarSpecifiedHasOutputNameWithSameNameAndType_throws() =>
            AssertFunParseError(
                FunBuilder.With("y:text = x").WithAprioriInput("y", VarType.Text));

        private void AssertFunParseError(FunBuilder builder) => Assert.Throws<FunParseException>(() => builder.Build());
    }
}