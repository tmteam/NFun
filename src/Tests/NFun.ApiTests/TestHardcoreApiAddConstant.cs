using System;
using System.Linq;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.ApiTests;

public class TestHardcoreApiAddConstant {
    [Test]
    public void UseConstant_inputNotAppears() =>
        Funny.Hardcore
            .WithConstants(("pipi", Math.PI))
            .WithFunction(new LogFunction())
            .Build("y = pipi").AssertRuntimes(r => {
                Assert.IsTrue(r.Variables.All(v => v.IsOutput));
                r.Calc().AssertReturns("y", Math.PI);
            });

    [Test]
    public void UseConstantWithOp() =>
        Funny
            .Hardcore
            .WithConstants(("one", 1))
            .Build("y = -one")
            .AssertRuntimes(r => r
                .Calc()
                .AssertReturns("y", -1));

    [Test]
    public void UseClrConstant_inputNotAppears() =>
        Funny.Hardcore
            .WithConstant("pipi", Math.PI)
            .WithFunction(new LogFunction())
            .Build("y = pipi")
            .AssertRuntimes(
                r => {
                    r.AssertInputsCount(0);
                    r.Calc().AssertReturns("y", Math.PI);
                });

    [Test]
    public void UseTwoClrConstants() =>
        Funny.Hardcore
            .WithConstant("one", 1)
            .WithConstant("two", 2)
            .WithFunction(new LogFunction())
            .Build("y = one+two")
            .AssertRuntimes(
                r => {
                    r.AssertInputsCount(0);
                    r.Calc().AssertReturns("y", 3);
                });

    [Test]
    public void UseTwoNfunConstants() =>
        Funny.Hardcore
            .WithConstants(("one", 1), ("two", 2))
            .Build("y = one+two")
            .AssertRuntimes(r => {
                r.AssertInputsCount(0);
                r.Calc().AssertReturns("y", 3);
            });

    [Test]
    public void OverrideConstantWithOutputVariable_constantNotUsed() =>
        Funny.Hardcore
            .WithConstant("pi", Math.PI)
            .WithFunction(new LogFunction())
            .Build("pi = 3; y = pi")
            .AssertRuntimes(r => {
                r.AssertInputsCount(0);
                r.Calc().AssertReturns(("y", 3), ("pi", 3));
            });

    [Test]
    public void OverrideConstantWithInputVariable_constantNotUsed() =>
        Funny.Hardcore
            .WithConstant("pi", Math.PI)
            .Build("pi:int; y = pi")
            .AssertRuntimes(r => {
                r.AssertInputsCount(1);
                r.Calc("pi", 2).AssertReturns("y", 2);
            });
}
