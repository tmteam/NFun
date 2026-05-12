using NFun.TestTools;
using NUnit.Framework;
using static NFun.OptionalTypesSupport;

namespace NFun.SyntaxTests;

[TestFixture]
public class KnownBugsTest {

    // ═══════════════════════════════════════════════════════════════
    // Bug: Default value struct literal int field inferred as Real
    // ═══════════════════════════════════════════════════════════════

    [Test]
    [Ignore("Bug: struct default param literal ({verbose=false, limit=10}) hits 'Complex constant type is not supported' in TicSetupVisitor")]
    public void DefaultStructLiteral_ShouldRespectParamType() =>
        "f(x, opts:{verbose:bool,limit:int}={verbose=false, limit=10}) = if(opts.verbose) x*opts.limit else x \r y = f(5)"
            .AssertReturns("y", 5);
}
