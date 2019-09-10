using NFun.TypeInference;
using NUnit.Framework;

namespace Funny.Tests.UnitTests
{
    public class AliasTableTest
    {
        [Test]
        public void EmptyTable_HasVariableReturnsNull()
        {
            var table = new AliasTable();
            Assert.IsFalse(table.HasVariable("some"));
        }
        [Test]
        public void AddVariableAlias_AliasDoesNotEqualOriginName()
        {
            var table = new AliasTable();
            var name = "some";
            table.AddVariableAlias(1, name);
            var result = table.GetVariableAlias(name);
            Assert.NotNull(result);
            Assert.AreNotEqual(result, name);
            StringAssert.Contains(name,result);
        }
        
        [Test]
        public void AddVariableAlias_DifferentLayersGotDifferentNames()
        {
            var table = new AliasTable();
            
            var name = "some";
            table.AddVariableAlias(1, name);
            var level0alias = table.GetVariableAlias(name);
            table.InitVariableScope(12, new[]{name});
            var level1alias = table.GetVariableAlias(name);
            table.InitVariableScope(42, new[]{name});
            
            var level2alias = table.GetVariableAlias(name);
            
            Assert.AreNotEqual(level2alias, level1alias);
            Assert.AreNotEqual(level2alias, level0alias);
            
            table.ExitVariableScope();
            Assert.AreEqual(level1alias, table.GetVariableAlias(name));                
            
            table.ExitVariableScope();
            Assert.AreEqual(level0alias, table.GetVariableAlias(name));                
        }
    }
}