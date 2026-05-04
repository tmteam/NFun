using System;
using NUnit.Framework;

namespace NFun.SyntaxTests.Lang;

[TestFixture]
public class LangMutableStructTest {

    [Test]
    public void StructLiteralReadField() {
        var rt = Funny.Hardcore.BuildLang("s = {a = 1, b = 2}\ny = s.a");
        rt.Run();
        Assert.AreEqual(1, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void FieldAssignment_Simple() {
        var rt = Funny.Hardcore.BuildLang("s = {a = 1, b = 2}\ns.a = 42\ny = s.a");
        rt.Run();
        Assert.AreEqual(42, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void FieldAssignment_PreservesOtherFields() {
        var rt = Funny.Hardcore.BuildLang("s = {a = 1, b = 2}\ns.a = 42\ny = s.b");
        rt.Run();
        Assert.AreEqual(2, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void FieldAssignment_StringField() {
        var rt = Funny.Hardcore.BuildLang("s = {name = 'Alice', age = 25}\ns.age = 26\ny = s.age");
        rt.Run();
        Assert.AreEqual(26, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void FieldAssignment_MultipleFields() {
        var rt = Funny.Hardcore.BuildLang("p = {x = 0, y = 0}\np.x = 10\np.y = 20\nresult = p.x + p.y");
        rt.Run();
        Assert.AreEqual(30, Convert.ToInt32(rt["result"].Value));
    }

    [Test]
    public void FieldAssignment_CompoundAssignment() {
        var rt = Funny.Hardcore.BuildLang("s = {value = 10}\ns.value += 5\ny = s.value");
        rt.Run();
        Assert.AreEqual(15, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void FieldAssignment_InForLoop() {
        var rt = Funny.Hardcore.BuildLang(
            "s = {count = 0}\nfor i in [1,2,3,4,5]:\n    s.count = s.count + 1\ny = s.count");
        rt.Run();
        Assert.AreEqual(5, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void ExpressionMode_StructStillImmutable() {
        // In expression mode, structs should still work (read-only access)
        var rt = Funny.Hardcore.Build("y = {a = 1}.a");
        rt.Run();
        Assert.AreEqual(1, Convert.ToInt32(rt["y"].Value));
    }
}
