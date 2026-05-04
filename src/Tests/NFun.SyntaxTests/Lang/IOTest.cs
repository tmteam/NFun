using System.IO;
using NUnit.Framework;

namespace NFun.SyntaxTests.Lang;

/// <summary>Tests for print/readLine/readChar with FunnyIO mock.</summary>
[TestFixture]
public class IOTest {

    // ═══════════════════════════════════════════════
    //  print — output capture
    // ═══════════════════════════════════════════════

    [Test]
    public void Print_Int() {
        var rt = Funny.Hardcore.BuildLang("fun f():\n    print(42)\n    return 0\ny = f()");
        rt.IO.Output = new StringWriter();
        rt.Run();
        Assert.That(rt.IO.Output.ToString(), Does.Contain("42"));
    }

    [Test]
    public void Print_Text() {
        var rt = Funny.Hardcore.BuildLang("fun f():\n    print('hello')\n    return 0\ny = f()");
        rt.IO.Output = new StringWriter();
        rt.Run();
        StringAssert.Contains("hello", rt.IO.Output.ToString());
    }

    [Test]
    public void Print_Bool() {
        var rt = Funny.Hardcore.BuildLang("fun f():\n    print(true)\n    return 0\ny = f()");
        rt.IO.Output = new StringWriter();
        rt.Run();
        StringAssert.Contains("true", rt.IO.Output.ToString().ToLower());
    }

    [Test]
    public void Print_WithNewline() {
        var rt = Funny.Hardcore.BuildLang("fun f():\n    print('a')\n    print('b')\n    return 0\ny = f()");
        rt.IO.Output = new StringWriter();
        rt.Run();
        var lines = rt.IO.Output.ToString().Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None);
        Assert.AreEqual("a", lines[0]);
        Assert.AreEqual("b", lines[1]);
    }

    [Test]
    public void Print_WithEnd_Empty() {
        var rt = Funny.Hardcore.BuildLang("fun f():\n    print('hello', '')\n    print(' world', '')\n    return 0\ny = f()");
        rt.IO.Output = new StringWriter();
        rt.Run();
        Assert.AreEqual("hello world", rt.IO.Output.ToString());
    }

    [Test]
    public void Print_WithEnd_Custom() {
        var rt = Funny.Hardcore.BuildLang("fun f():\n    print('a', ', ')\n    print('b', ', ')\n    print('c', '!')\n    return 0\ny = f()");
        rt.IO.Output = new StringWriter();
        rt.Run();
        Assert.AreEqual("a, b, c!", rt.IO.Output.ToString());
    }

    [Test]
    public void Print_Multiple_InLoop_Equivalent() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    print('line1')\n    print('line2')\n    print('line3')\n    return 0\ny = f()");
        rt.IO.Output = new StringWriter();
        rt.Run();
        var output = rt.IO.Output.ToString();
        Assert.That(output, Does.Contain("line1"));
        Assert.That(output, Does.Contain("line2"));
        Assert.That(output, Does.Contain("line3"));
    }

    [Test]
    public void Print_ReturnsNone() {
        // print is a procedure — returns none, can't assign meaningfully
        var rt = Funny.Hardcore.BuildLang("fun f():\n    print('test')\n    return 42\ny = f()");
        rt.IO.Output = new StringWriter();
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    // ═══════════════════════════════════════════════
    //  readLine — input mock
    // ═══════════════════════════════════════════════

    [Test]
    public void ReadLine_Basic() {
        var rt = Funny.Hardcore.BuildLang("name = readLine()\ny = name");
        rt.IO.Input = new StringReader("Alice\n");
        rt.IO.Output = new StringWriter();
        rt.Run();
        Assert.AreEqual("Alice", rt["y"].Value?.ToString());
    }

    [Test]
    public void ReadLine_MultipleReads() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    a = readLine()\n    b = readLine()\n    return a\ny = f()");
        rt.IO.Input = new StringReader("hello\nworld\n");
        rt.IO.Output = new StringWriter();
        rt.Run();
        Assert.AreEqual("hello", rt["y"].Value?.ToString());
    }

    [Test]
    public void ReadLine_EmptyInput() {
        var rt = Funny.Hardcore.BuildLang("name = readLine()\ny = name");
        rt.IO.Input = new StringReader("\n");
        rt.IO.Output = new StringWriter();
        rt.Run();
        Assert.AreEqual("", rt["y"].Value?.ToString());
    }

    [Test]
    public void ReadLine_EOF_ReturnsEmpty() {
        var rt = Funny.Hardcore.BuildLang("name = readLine()\ny = name");
        rt.IO.Input = new StringReader(""); // EOF immediately
        rt.IO.Output = new StringWriter();
        rt.Run();
        Assert.AreEqual("", rt["y"].Value?.ToString());
    }

    [Test]
    public void ReadLine_InFunction() {
        var rt = Funny.Hardcore.BuildLang(
            "fun ask():\n    name = readLine()\n    return name\ny = ask()");
        rt.IO.Input = new StringReader("World\n");
        rt.IO.Output = new StringWriter();
        rt.Run();
        Assert.AreEqual("World", rt["y"].Value?.ToString());
    }

    // ═══════════════════════════════════════════════
    //  readChar — single character input
    // ═══════════════════════════════════════════════

    [Test]
    public void ReadChar_Basic() {
        var rt = Funny.Hardcore.BuildLang("c = readChar()\ny = c");
        rt.IO.Input = new StringReader("A");
        rt.IO.Output = new StringWriter();
        rt.Run();
        Assert.AreEqual('A', rt["y"].Value);
    }

    [Test]
    public void ReadChar_FirstCharOfStream() {
        var rt = Funny.Hardcore.BuildLang("c = readChar()\ny = c");
        rt.IO.Input = new StringReader("Hello");
        rt.IO.Output = new StringWriter();
        rt.Run();
        Assert.AreEqual('H', rt["y"].Value);
    }

    [Test]
    public void ReadChar_EOF_ReturnsNull() {
        var rt = Funny.Hardcore.BuildLang("c = readChar()\ny = c");
        rt.IO.Input = new StringReader("");
        rt.IO.Output = new StringWriter();
        rt.Run();
        Assert.AreEqual('\0', rt["y"].Value);
    }

    // ═══════════════════════════════════════════════
    //  Combined print + readLine
    // ═══════════════════════════════════════════════

    [Test]
    public void PrintAndRead_Interactive() {
        var rt = Funny.Hardcore.BuildLang(
            "fun ask(question):\n" +
            "    print(question, end = '')\n" +
            "    return readLine()\n\n" +
            "name = ask('Name? ')\n" +
            "age = ask('Age? ')");
        rt.IO.Input = new StringReader("Alice\n25\n");
        rt.IO.Output = new StringWriter();
        rt.Run();
        Assert.AreEqual("Alice", rt["name"].Value?.ToString());
        Assert.AreEqual("25", rt["age"].Value?.ToString());
        Assert.AreEqual("Name? Age? ", rt.IO.Output.ToString());
    }

    [Test]
    public void PrintAndRead_EchoUser() {
        var rt = Funny.Hardcore.BuildLang(
            "fun main():\n" +
            "    print('name: ', end = '')\n" +
            "    name = readLine()\n" +
            "    print(name)\n" +
            "    return name\n\n" +
            "result = main()");
        rt.IO.Input = new StringReader("Alice\n");
        rt.IO.Output = new StringWriter();
        rt.Run();
        Assert.AreEqual("Alice", rt["result"].Value?.ToString());
        var output = rt.IO.Output.ToString();
        Assert.That(output, Does.StartWith("name: "));
        Assert.That(output, Does.Contain("Alice"));
    }

    // ═══════════════════════════════════════════════
    //  IO isolation — per-runtime
    // ═══════════════════════════════════════════════

    [Test]
    public void IO_PerRuntime_Isolated() {
        var rt1 = Funny.Hardcore.BuildLang("fun f():\n    print('rt1')\n    return 0\ny = f()");
        var rt2 = Funny.Hardcore.BuildLang("fun f():\n    print('rt2')\n    return 0\ny = f()");

        rt1.IO.Output = new StringWriter();
        rt2.IO.Output = new StringWriter();

        rt1.Run();
        rt2.Run();

        Assert.That(rt1.IO.Output.ToString(), Does.Contain("rt1"));
        Assert.That(rt2.IO.Output.ToString(), Does.Contain("rt2"));
        Assert.That(rt1.IO.Output.ToString(), Does.Not.Contain("rt2"));
        Assert.That(rt2.IO.Output.ToString(), Does.Not.Contain("rt1"));
    }

    [Test]
    public void IO_DefaultIsConsole() {
        var rt = Funny.Hardcore.BuildLang("y = 42");
        Assert.IsNotNull(rt.IO);
        Assert.IsNotNull(rt.IO.Input);
        Assert.IsNotNull(rt.IO.Output);
    }
}
