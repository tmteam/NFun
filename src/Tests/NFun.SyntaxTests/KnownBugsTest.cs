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
    public void DefaultStructLiteral_ShouldRespectParamType() =>
        "f(x, opts:{verbose:bool,limit:int}={verbose=false, limit=10}) = if(opts.verbose) x*opts.limit else x \r y = f(5)"
            .AssertReturns("y", 5);
}
