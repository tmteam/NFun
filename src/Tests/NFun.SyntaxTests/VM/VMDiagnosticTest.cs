using System;
using System.Text;
using NFun.VM;
using NUnit.Framework;

namespace NFun.SyntaxTests.VM;

[TestFixture]
public class VMDiagnosticTest {

    [Test]
    public void DumpBytecode_SimpleArithmetic() {
        var expr = "y = 2 * x + 1";
        var vm = Funny.Hardcore.WithApriori("x", FunnyType.Int32).BuildVM(expr);

        // Access program via reflection
        var field = typeof(VMRuntime).GetField("_program",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var program = (CompiledProgram)field.GetValue(vm);

        var sb = new StringBuilder();
        sb.AppendLine($"Expression: {expr}");
        sb.AppendLine($"Code length: {program.Code.Length} bytes");
        sb.AppendLine($"Constants: {program.Constants.Length}");
        sb.AppendLine($"ExternFunctions: {program.ExternFunctions.Length}");
        sb.AppendLine($"Locals: {program.LocalsCount}");
        sb.AppendLine();

        // Dump bytecode
        int ip = 0;
        while (ip < program.Code.Length) {
            var op = (Op)program.Code[ip];
            sb.Append($"  [{ip:D4}] {op}");

            switch (op) {
                case Op.LoadConstI:
                case Op.LoadConstR:
                case Op.LoadConstRef:
                case Op.LoadLocal:
                case Op.StoreLocal:
                    sb.Append($" #{program.Code[ip + 1]}");
                    ip += 2;
                    break;
                case Op.CallExtern:
                    var funcId = program.Code[ip + 1];
                    var argc = program.Code[ip + 2];
                    var funcName = program.ExternFunctions[funcId].Function?.ToString() ?? "?";
                    sb.Append($" func#{funcId}({argc} args) [{funcName}]");
                    ip += 3;
                    break;
                case Op.Jump:
                case Op.JumpIfFalse:
                case Op.JumpIfTrue:
                    var addr = program.Code[ip + 1] | (program.Code[ip + 2] << 8);
                    sb.Append($" → {addr}");
                    ip += 3;
                    break;
                default:
                    ip++;
                    break;
            }
            sb.AppendLine();
        }

        // Dump variables
        sb.AppendLine("Variables:");
        foreach (var v in program.Variables)
            sb.AppendLine($"  slot#{v.Slot} {v.Name}:{v.Type} {(v.IsOutput ? "[OUT]" : "[IN]")}");

        // Dump extern functions
        sb.AppendLine("Extern functions:");
        for (int i = 0; i < program.ExternFunctions.Length; i++)
            sb.AppendLine($"  #{i}: {program.ExternFunctions[i].Function} → {program.ExternFunctions[i].ReturnType}");

        TestContext.WriteLine(sb.ToString());
    }

    [Test]
    public void AnalyzeSimpleBenchScripts() {
        var scripts = new[] {
            "y = 2 * x + 1",
            "y = (a - b) / 2.0",
            "y = a // 3 + a % 3",
            "y = a ** 2.0",
            "y = x > 0 and x < 100",
            "y = a == b or c != d",
            "y = not flag",
            "y = a xor b",
            "y = if(x > 0) x else -x",
            "y = max(a, b)",
            "y = 42.toText()",
            "y = 'hello world'",
            "x1:int\r x2:int\r o1 = x1 * x1 + x1 * 2\r o2 = if(x2 > 0 and o1 != 0) x1 else o1",
            "a:int\r b:int\r c:real\r o1 = a + b\r o2:real = a * c\r o3 = if(a > b) c else -c",
            "x:int\r y = x + 1",
            "x:int\r y = x * 2 - 3",
            "x:int\r y = x > 0 and x < 100",
            "x:int\r y = if(x > 0) x else -x",
            "x:int\r y = max(x, 0)",
            "x:int\r y = x.toText()",
            "x:int\r y = x % 7 + x / 3",
            "x:int\r y:real = x * 3.14 + 1.0",
        };
        var sb = new StringBuilder();
        foreach (var script in scripts) {
            try {
                var vm = Funny.Hardcore.BuildVM(script);
                var field = typeof(VMRuntime).GetField("_program",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var program = (CompiledProgram)field.GetValue(vm);

                int callExternCount = 0, nativeOpCount = 0;
                int ip2 = 0;
                while (ip2 < program.Code.Length) {
                    var op = (Op)program.Code[ip2];
                    if (op == Op.CallExtern) { callExternCount++; ip2 += 3; }
                    else if (op == Op.Halt || op == Op.StoreHalt) { ip2 += (op == Op.StoreHalt ? 2 : 1); }
                    else if (op >= Op.LoadConstI && op <= Op.LoadNone) { ip2 += 2; nativeOpCount++; }
                    else if (op >= Op.AddInt && op <= Op.BoxBool) { ip2++; nativeOpCount++; }
                    else if (op == Op.MaxInt || op == Op.MinInt || op == Op.AbsInt ||
                             op == Op.MaxReal || op == Op.MinReal || op == Op.AbsReal) { ip2++; nativeOpCount++; }
                    else { ip2++; nativeOpCount++; }
                }

                // Also try to run and check if it crashes
                try { vm.Run(); } catch (Exception ex) { sb.AppendLine($"  RUN FAIL: {ex.GetType().Name}: {ex.Message.Split('\n')[0]}"); }

                sb.AppendLine($"{script.Replace("\r","\\r"),-60} | code={program.Code.Length,3}B | extern={callExternCount} native={nativeOpCount} | externs=[{string.Join(", ", System.Linq.Enumerable.Select(program.ExternFunctions, e => e.Function?.GetType()?.Name ?? "?"))}]");
            } catch (Exception ex) {
                sb.AppendLine($"{script.Replace("\r","\\r"),-60} | BUILD FAIL: {ex.Message.Split('\n')[0]}");
            }
        }
        TestContext.WriteLine(sb.ToString());
    }

    [Test]
    public void Debug_MaxX0() {
        var vm = Funny.Hardcore.BuildVM("x:int\r y = max(x, 0)");
        vm.Run();
        Assert.AreEqual(0, vm.GetOutput("y"));
    }

    [Test]
    public void Debug_SubDiv_NoApriori() {
        var vm = Funny.Hardcore.BuildVM("y = (a - b) / 2.0");
        vm.Run();
        Assert.AreEqual(0.0, (double)vm.GetOutput("y"), 0.001);
    }

    [Test]
    public void Debug_MaxX0_NoSetInput() {
        // Build with x:int declared inline, don't call SetInput — x defaults to 0
        var vm = Funny.Hardcore.BuildVM("x:int\r y = max(x, 0)");
        vm.Run(); // should NOT crash
        Assert.AreEqual(0, vm.GetOutput("y"));
    }

    [Test]
    public void Debug_MaxX0_WithApriori() {
        var vm = Funny.Hardcore.WithApriori("x", FunnyType.Int32).BuildVM("x:int\r y = max(x, 0)");
        vm.Run();
        Assert.AreEqual(0, vm.GetOutput("y"));
    }
}
