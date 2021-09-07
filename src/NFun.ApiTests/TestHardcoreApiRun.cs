using System;
using System.Collections.Generic;
using System.Linq;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.ApiTests {

class RuntimeApiTest {
    [TestCase("y = 2*x", 3, 6)]
    [TestCase("y = 2.0*x", 3.5, 7.0)]
    [TestCase("y = 4/x", 2, 2)]
    [TestCase("y = x/4", 10, 2.5)]
    [TestCase("y = 4- x", 3, 1)]
    [TestCase("y = x- x", 3, 0)]
    [TestCase("y = 4+ x", 3, 7)]
    [TestCase("y = (x + 4/x)", 2, 4)]
    [TestCase("y = x**3", 2, 8)]
    [TestCase("y = x % 3", 2, 2)]
    [TestCase("y = x % 4", 5, 1)]
    [TestCase("y = x % -4", 5, 1)]
    [TestCase("y = x % 4", -5, -1)]
    [TestCase("y = x % -4", -5, -1)]
    [TestCase("y = -x ", 0.3, -0.3)]
    [TestCase("y = -(-(-x))", 2, -2)]
    [TestCase("y = x/0.2", 1, 5)]
    public void SingleVariableEquation(string expr, double arg, double expected) {
        var runtime = expr.Build();
        var ySource = runtime["y"];
        var xSource = runtime["x"];
        Assert.IsTrue(ySource.IsOutput);
        Assert.IsFalse(xSource.IsOutput);

        xSource.Value = arg;
        runtime.Run();
        Assert.AreEqual(expected, ySource.FunnyValue);
    }

    [TestCase("y = 4.0 + x", 3, 7)]
    [TestCase("y = (x + 4/x)", 2, 4)]
    [TestCase("y = x**3", 2, 8)]
    public void TypedSingleVariableEquation(string expr, double arg, double expected) {
        var runtime = expr.Build();
        var ySource = runtime["y"];
        var xSource = runtime["x"];

        xSource.GetTypedSetter<double>()(arg);
        runtime.Run();
        Assert.AreEqual(expected, ySource.GetTypedGetter<double>()());
    }

    [TestCase("y = 2*x", 0.0)]
    [TestCase("y:real = 2*x", 0.0)]
    [TestCase("y:int64 = 2*x", (Int64)0)]
    [TestCase("y:int32 = 2*x", (Int32)0)]
    [TestCase("y:int16 = 2&x", (Int16)0)]
    [TestCase("y:uint64 = 2*x", (UInt64)0)]
    [TestCase("y:uint32 = 2*x", (UInt32)0)]
    [TestCase("y:uint16 = 2&x", (UInt16)0)]
    [TestCase("y:byte   = 2&x", (byte)0)]
    [TestCase("y:byte   = 2|x", (byte)2)]
    [TestCase("y:byte   = x1|x2|x3", (byte)0)]
    [TestCase("y = x/4", 0.0)]
    [TestCase("y = (x+1)/4", 0.25)]
    [TestCase("y = true or x", true)]
    [TestCase("x:bool; y = x or x", false)]
    [TestCase("x:text; y = x", "")]
    [TestCase("x:text; y = x.count()", 0)]
    [TestCase("x:text; y = x.reverse()", "")]
    [TestCase("x:int[]; y = x.count()", 0)]
    [TestCase("x:int[][]; y = x.count()", 0)]
    [TestCase("x:real[][]; y = x.count()", 0)]
    [TestCase("x:text[][]; y = x.count()", 0)]
    [TestCase("y = -(-(-x))", 0.0)]
    public void InputNotSet_SingleVariableEquation(string expr, object expected) {
        var runtime = expr.Build();
        var ySource = runtime["y"];
        Assert.IsNotNull(ySource);
        runtime.Run();
        Assert.AreEqual(expected, ySource.Value);
    }

    [Test]
    public void RuntimeWithSimpleTypes_VariablesEnumerationTest() {
        // Hardcore mode 
        var runtime = Funny.Hardcore.Build(
            @"
                    out1 = in1-1
                    out2:int = in2.filter(fun it>out1).map(fun it*it)[1]
                    ");

        runtime["in1"].Value = 2;
        runtime["in2"].Value = new[] { 0, 1, 2, 3, 4 };

        runtime.Run();


        foreach (var v in runtime.Variables)
            Console.WriteLine($"Variable '{v.Name}'' of type {v.Type} equals {v.Value}");
        Assert.AreEqual(2, runtime.Variables.FirstOrDefault(i => i.Name == "in1")?.Value);
        Assert.AreEqual(2, runtime["in1"].Value);
    }

    [Test]
    public void RuntimeWithCompositeTypes_VariablesEnumerationTest() {
        // Hardcore mode 
        var runtime = Funny.Hardcore.Build(
            @"
                    out3 = {age = 1, name = 'vasa'}    
                    out4 = in3.field1
                    out5 = in3.field2.child    
");

        runtime["in3"].Value = new Dictionary<string, object> {
            { "field1", 123 }, {
                "field2", new Dictionary<string, object> {
                    { "child", "kavabanga" }
                }
            }
        };

        runtime.Run();

        foreach (var v in runtime.Variables)
            Console.WriteLine($"Variable '{v.Name}'' of type {v.Type} equals {v.Value}");
        Assert.AreEqual(123, runtime.Variables.FirstOrDefault(i => i.Name == "out4")?.Value);
        Assert.AreEqual(123, runtime["out4"].Value);

        Assert.AreEqual("kavabanga", runtime.Variables.FirstOrDefault(i => i.Name == "out5")?.Value);
        Assert.AreEqual("kavabanga", runtime["out5"].Value);

        Assert.IsInstanceOf<IDictionary<string, object>>(runtime["out3"].Value);
        Assert.IsInstanceOf<IDictionary<string, object>>(
            runtime.Variables.FirstOrDefault(i => i.Name == "out3")
                   ?.Value);
    }

    [Test]
    public void TypedRuntimeWithCompositeTypes_VariablesEnumerationTest() {
        // Hardcore mode 
        var runtime = Funny.Hardcore.Build(
            @"
                    out3 = {age = 1, name = 'vasa'}    
                    out4 = in3.field1 + 1
                    out5:text = in3.field2.child    
");

        runtime["in3"].GetTypedSetter<Dictionary<string, object>>()
        (
            new Dictionary<string, object> {
                { "field1", 123 }, {
                    "field2", new Dictionary<string, object> {
                        { "child", "kavabanga" }
                    }
                }
            });

        runtime.Run();

        Assert.AreEqual(124, runtime.Variables.FirstOrDefault(i => i.Name == "out4")?.GetTypedGetter<int>()());
        Assert.AreEqual(124, runtime["out4"]?.GetTypedGetter<int>()());

        Assert.AreEqual(
            "kavabanga",
            runtime.Variables.FirstOrDefault(i => i.Name == "out5")?.GetTypedGetter<string>()());
        Assert.AreEqual("kavabanga", runtime["out5"]?.GetTypedGetter<string>()());

        Assert.IsNotNull(runtime["out3"].GetTypedGetter<Dictionary<string, object>>());
        Assert.IsNotNull(
            runtime.Variables.FirstOrDefault(i => i.Name == "out3")
                   ?.GetTypedGetter<Dictionary<string, object>>());
    }

    [Test]
    public void RuntimeWithVeryComplexCompositeTypes_VariablesEnumerationTest() {
        // Hardcore mode 
        var runtime = Funny.Hardcore.Build("{age = 1, items = [{f1 = 1, f2 = 'hi', f3 = [1,2,3]}]}");

        runtime.Run();

        foreach (var v in runtime.Variables)
            Console.WriteLine(
                $"Variable '{v.Name}'' of type {v.Type} equals {v.Value} with internal value = {v.FunnyValue}");

        Assert.IsInstanceOf<IDictionary<string, object>>(runtime["out"].Value);
        var value = runtime.Variables.FirstOrDefault(i => i.Name == "out");
        Assert.IsInstanceOf<IDictionary<string, object>>(value.Value);
        var str = (IDictionary<string, object>)value.Value;
        Assert.AreEqual(1, str["age"]);
        var items = (IDictionary<string, object>[])str["items"];
        Assert.AreEqual(1, items.Length);
        var item = items[0];
        Assert.AreEqual(1, item["f1"]);
        Assert.AreEqual("hi", item["f2"]);
        CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, (int[])item["f3"]);
    }

    [Test]
    public void TypedRuntimeWithVeryComplexCompositeTypes_VariablesEnumerationTest() {
        // Hardcore mode 
        var runtime = Funny.Hardcore.Build("{age = 1, items = [{f1 = 1, f2 = 'hi', f3 = [1,2,3]}]}");

        runtime.Run();

        var value = runtime.Variables.FirstOrDefault(i => i.Name == "out")
                           ?.GetTypedGetter<Dictionary<string, object>>()();
        AssertValue(value);

        var getter = runtime["out"].GetTypedGetter<IDictionary<string, object>>();

        AssertValue(getter());
        AssertValue(getter());

        void AssertValue(IDictionary<string, object> value) {
            Assert.AreEqual(1, value["age"]);
            var items = (IDictionary<string, object>[])value["items"];
            Assert.AreEqual(1, items.Length);
            var item = items[0];
            Assert.AreEqual(1, item["f1"]);
            Assert.AreEqual("hi", item["f2"]);
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, (int[])item["f3"]);
        }
    }
}

}