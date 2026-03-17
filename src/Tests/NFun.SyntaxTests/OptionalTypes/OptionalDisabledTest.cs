namespace NFun.SyntaxTests.OptionalTypes;

using NFun.TestTools;
using NUnit.Framework;

[TestFixture]
public class OptionalDisabledTest {

    [TestCase("y = none ?? 42")]
    [TestCase("x:int?\r y = x ?? 0")]
    [TestCase("y = 42 ?? 0")]
    public void CoalesceOperator_FailsWhenDisabled(string expr) =>
        expr.AssertObviousFailsOnParse(optionalTypesSupport: OptionalTypesSupport.Disabled);
}
