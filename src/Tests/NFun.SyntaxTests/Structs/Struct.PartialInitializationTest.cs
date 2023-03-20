namespace NFun.SyntaxTests.Structs;

using System.Collections.Generic;
using NUnit.Framework;
using TestTools;
using Tic;

public class Struct_PartialInitializationTest {

    [Test]
    public void PartialInitialization() {
        var res = Funny.Calc<ContractOutputModel>("{items = ['1','2','3','4']; price = 42; taxes = 1.5}");
        FunnyAssert.AreSame(
            expected: new ContractOutputModel {
                Items = new[] { "1", "2", "3", "4" }, Price = 42, Taxes = new decimal(1.5)
            },
            actual: res);
    }

    [Test]
    public void PartialInitialization2() {
        var res = Funny.Calc<ContractOutputModel>("{items = ['1','2','3','4']; price = 42}");
        FunnyAssert.AreSame(new ContractOutputModel { Items = new[] { "1", "2", "3", "4" }, Price = 42 },res);
    }

    [Test]
    public void PartialInitialization3() {
        var res = Funny.Calc<ContractOutputModel>("{price = 42}");
        FunnyAssert.AreSame(new ContractOutputModel { Price = 42 }, res);
    }

    [Test]
    public void PartialInitialization4() {
        var res = Funny.Calc<ContractOutputModel>("{}");
        FunnyAssert.AreSame(new ContractOutputModel(), res);
    }

    [Test]
    public void PartialArrayInitialization1() {
        var res = Funny.Calc<ContractOutputModel[]>("[{}]");
        FunnyAssert.AreSame(new[]{new ContractOutputModel()}, res);
    }

    [Test]
    public void PartialArrayInitialization2() {
        var res = Funny.Calc<ContractOutputModel[]>("[{},{}]");
        FunnyAssert.AreSame(new[]{new ContractOutputModel(),new ContractOutputModel()}, res);
    }

    [Test]
    public void PartialArrayInitialization3() {
        var res = Funny.Calc<ComplexModel[]>("[{},{a = {id = 1}}]");
        FunnyAssert.AreSame(new[]{new ComplexModel(),new ComplexModel{a = new ModelWithInt(){id = 1}}}, res);
    }

    [Test]
    public void PartialArrayInitialization4() {
        using var _ = TraceLog.Scope;
        var res = Funny.Calc<ComplexModel[]>("[{a = {id = 1}}, {a = { id = 2 }}]");
        FunnyAssert.AreSame(new[]{new ComplexModel{a = new ModelWithInt(){id = 1}},new ComplexModel{a = new ModelWithInt(){id = 2}}}, res);
    }

    [Test]
    public void PartialArrayInitialization5() {
        using var _ = TraceLog.Scope;
        var res = Funny.Calc<ComplexModel[]>("[{},{a = {id =2}, b = {id = 3}}]");
        FunnyAssert.AreSame(new[] {
            new ComplexModel(),
            new ComplexModel{a = new ModelWithInt(){id = 2}, b = new ModelWithInt(){id = 3}}
        }, res);
    }

    [Test]
    public void PartialArrayInitialization6() {
        using var _ = TraceLog.Scope;
        var res = Funny.Calc<ComplexModel[]>("[{a = {id =2}}, {b = {id = 3}}]");
        FunnyAssert.AreSame(new[] {
            new ComplexModel{a = new ModelWithInt(){id = 2}},
            new ComplexModel{b = new ModelWithInt(){id = 3}}
        }, res);
    }

    [Test]
    public void PartialArrayInitialization7() {
        using var _ = TraceLog.Scope;
        var res = Funny.Calc<ComplexModel[]>("[{a = {id = 1}},{},{a = {id =2}, b = {id = 3}}]");
        FunnyAssert.AreSame(new[] {
            new ComplexModel{a = new ModelWithInt(){id = 1}},
            new ComplexModel(),
            new ComplexModel{a = new ModelWithInt(){id = 2}, b = new ModelWithInt(){id = 3}}
        }, res);
    }


    [Ignore("UB")]
    [Test]
    public void DefaultInitialization4() {
        var res = Funny.Calc<ContractOutputModel>("default");
        FunnyAssert.AreSame(new ContractOutputModel(), res);
    }

    [Test]
    public void NestedPartialInitialization() {
        var res = Funny.Calc<ComplexModel>("{}");
        FunnyAssert.AreSame(new ComplexModel(), res);
    }

    [Test]
    public void NestedPartialInitialization2() {
        var res = Funny.Calc<ComplexModel>("{a = {}}");
        FunnyAssert.AreSame(new ComplexModel { a = new ModelWithInt() },res);
    }

    [Test]
    public void NestedPartialInitialization3() {
        var res = Funny.Calc<ComplexModel>("{a = {}, b = {}}");
        FunnyAssert.AreSame(new ComplexModel { a = new ModelWithInt(), b = new ModelWithInt() },res);
    }

    [Test]
    public void NestedPartialInitialization4() {
        var res = Funny.Calc<ComplexModel>("{a = {id = 42}, b = {}}");
        FunnyAssert.AreSame(new ComplexModel { a = new ModelWithInt { id = 42 }, b = new ModelWithInt() }, res);
    }

    [Test]
    public void HardcoreNestedPartialInitialization1() {
        using var s = TraceLog.Scope;
        var runtime =
            Funny.Hardcore.WithApriori<ComplexModel>("foo").Build("foo = {a = {id = 42}}");
        runtime.Run();

        var barResult = runtime["foo"].Value as IDictionary<string, object>;
        var aid = (barResult["a"] as IDictionary<string, object>)["id"];
        Assert.AreEqual(aid, 42);
        Assert.IsFalse(barResult.TryGetValue("b", out _));
    }


    [Test]
    public void HardcoreNestedPartialInitialization2() {
        using var s = TraceLog.Scope;
        var runtime =
            Funny.Hardcore.WithApriori<ComplexModel>("foo").Build("bar = {a = {id = 42}}; foo = bar");
        runtime.Run();

        var barResult = runtime["foo"].Value as IDictionary<string, object>;
        var aid = (barResult["a"] as IDictionary<string, object>)["id"];
        Assert.AreEqual(aid, 42);
        Assert.IsFalse(barResult.TryGetValue("b", out _));
    }


    [Test]
    public void HardcorePartialArrayInitialization1() {
        using var _ = TraceLog.Scope;
        var runtime =
            Funny.Hardcore.WithApriori<ComplexModel[]>("foo").Build("foo = [{a = {id = 1}}, {a = { id = 2 }}]");
        runtime.Run();
        FunnyAssert.AreSame(new[]{new ComplexModel{a = new ModelWithInt {id = 1}},new ComplexModel{a = new ModelWithInt(){id = 2}}},
            runtime["foo"].CreateGetterOf<ComplexModel[]>()());
    }

    [Test]
    public void HardcorePartialArrayInitialization2() {
        using var _ = TraceLog.Scope;
        var runtime =
            Funny.Hardcore.WithApriori<ComplexModel[]>("foo").Build("foo = [{},{a = {id =2}, b = {id = 3}}]");
        runtime.Run();
        FunnyAssert.AreSame(
            new[] {
                new ComplexModel(),
                new ComplexModel{a = new ModelWithInt(){id = 2}, b = new ModelWithInt(){id = 3}}
            },
            runtime["foo"].Value);
    }

    [Test]
    public void HardcorePartialArrayInitialization3() {
        using var _ = TraceLog.Scope;
        var runtime =
            Funny.Hardcore.WithApriori<ComplexModel[]>("foo")
                .Build("foo = [{a = {id =2}}, {b = {id = 3}}]");
        runtime.Run();
        FunnyAssert.AreSame(
            new[] {
                new ComplexModel{a = new ModelWithInt(){id = 2}},
                new ComplexModel{b = new ModelWithInt(){id = 3}}
            },
            runtime["foo"].CreateGetterOf<ComplexModel[]>()());
    }
}
