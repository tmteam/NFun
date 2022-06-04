using NFun.TypeInferenceAdapter;
using NUnit.Framework;

namespace NFun.UnitTests; 

public class AliasTableTest {
    [Test]
    public void EmptyTable_HasVariableReturnsNull() {
        var table = new VariableScopeAliasTable();
        Assert.IsFalse(table.HasVariable("some"));
    }

    [Test]
    public void AddVariableAlias_AliasDoesNotEqualOriginName() {
        var table = new VariableScopeAliasTable();
        var name = "some";
        table.AddVariableAlias(1, name);
        var result = table.GetVariableAlias(name);
        Assert.NotNull(result);
        Assert.AreNotEqual(result, name);
        StringAssert.Contains(name, result);
    }

    [Test]
    public void AddVariableAlias_DifferentLayersGotDifferentNames() {
        var table = new VariableScopeAliasTable();

        var name = "some";
        table.AddVariableAlias(1, name);
        var level0alias = table.GetVariableAlias(name);
        table.EnterScope(12, new[] { name });
        var level1alias = table.GetVariableAlias(name);
        table.EnterScope(42, new[] { name });

        var level2alias = table.GetVariableAlias(name);

        Assert.AreNotEqual(level2alias, level1alias);
        Assert.AreNotEqual(level2alias, level0alias);

        table.ExitScope();
        Assert.AreEqual(level1alias, table.GetVariableAlias(name));

        table.ExitScope();
        Assert.AreEqual(level0alias, table.GetVariableAlias(name));
    }
}