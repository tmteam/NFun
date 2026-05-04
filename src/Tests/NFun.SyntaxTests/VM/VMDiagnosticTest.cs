using System;
using System.Collections.Generic;
using System.Text;
using NFun.VM;
using NUnit.Framework;

namespace NFun.SyntaxTests.VM;

[TestFixture]
public class VMDiagnosticTest {

    // ═══════════════════════════════════════════════════════════
    //  VMBenchCrash: test every benchmark script for Build/Run
    // ═══════════════════════════════════════════════════════════

    private record BenchScript(string Script, Dictionary<string, object> Inputs);

    private static BenchScript Pure(string script) =>
        new(script, new Dictionary<string, object>());

    private static BenchScript IntX(string script) =>
        new(script, new Dictionary<string, object> { ["x"] = 1 });

    [Test]
    public void VMBenchCrash_AllScripts() {
        var allScripts = new List<(string Subset, BenchScript Script)>();

        // PureArith
        foreach (var s in new[] {
            Pure("y = 2 * x + 1"),
            Pure("y = (a - b) / 2.0"),
            Pure("y = a // 3 + a % 3"),
            Pure("y = x > 0 and x < 100"),
            Pure("y = a == b or c != d"),
            Pure("y = not flag"),
            Pure("y = a xor b"),
            Pure("y = if(x > 0) x else -x"),
            IntX("x:int\r y = x + 1"),
            IntX("x:int\r y = x * 2 - 3"),
            IntX("x:int\r y = x > 0 and x < 100"),
            IntX("x:int\r y = if(x > 0) x else -x"),
            IntX("x:int\r y = x % 7 + x / 3"),
            IntX("x:int\r y:real = x * 3.14 + 1.0"),
        }) allScripts.Add(("PureArith", s));

        // CallExtern
        foreach (var s in new[] {
            Pure("y = max(a, b)"),
            Pure("y = 42.toText()"),
            Pure("y = 'hello world'"),
            Pure("y = a ** 2.0"),
            IntX("x:int\r y = max(x, 0)"),
            IntX("x:int\r y = x.toText()"),
        }) allScripts.Add(("CallExtern", s));

        // Simple (includes PureArith + CallExtern, but list them all for completeness)
        foreach (var s in new[] {
            Pure("y = 2 * x + 1"),
            Pure("y = (a - b) / 2.0"),
            Pure("y = a // 3 + a % 3"),
            Pure("y = a ** 2.0"),
            Pure("y = x > 0 and x < 100"),
            Pure("y = a == b or c != d"),
            Pure("y = not flag"),
            Pure("y = a xor b"),
            Pure("y = if(x > 0) x else -x"),
            Pure("y = max(a, b)"),
            Pure("y = 42.toText()"),
            Pure("y = 'hello world'"),
            Pure("x1:int\r x2:int\r o1 = x1 * x1 + x1 * 2\r o2 = if(x2 > 0 and o1 != 0) x1 else o1"),
            Pure("a:int\r b:int\r c:real\r o1 = a + b\r o2:real = a * c\r o3 = if(a > b) c else -c"),
            IntX("x:int\r y = x + 1"),
            IntX("x:int\r y = x * 2 - 3"),
            IntX("x:int\r y = x > 0 and x < 100"),
            IntX("x:int\r y = if(x > 0) x else -x"),
            IntX("x:int\r y = max(x, 0)"),
            IntX("x:int\r y = x.toText()"),
            IntX("x:int\r y = x % 7 + x / 3"),
            IntX("x:int\r y:real = x * 3.14 + 1.0"),
        }) allScripts.Add(("Simple", s));

        // Medium
        foreach (var s in new[] {
            Pure("y = [1,2,3,4,5].map(rule it * 2)"),
            Pure("y = [1,2,3,4,5,6,7,8,9,10].filter(rule it > 5).count()"),
            Pure("y = [1,2,3,4,5].fold(rule it1 + it2)"),
            Pure("abs(x) = if(x >= 0) x else -x\r y = abs(-42) + abs(7)"),
            Pure("multi(a,b) =\r   if(a.count()!=b.count()) []\r   else [0..a.count()-1].map(rule a[it]*b[it])\r a = [1,2,3]\r b = [4,5,6]\r expected = [4,10,18]\r passed = a.multi(b)==expected"),
            Pure("a = 'hello'\r b = a.reverse()\r y = a.concat(' ').concat(b)"),
            Pure("person = {name = 'Alice'; age = 30; active = true}\r y = {greeting = person.name; doubleAge = person.age * 2; flag = person.active}"),
            Pure("getInfo(p) = {summary = p.name; score = p.age * 2}\r input = {name = 'Bob'; age = 25; active = true}\r y = getInfo(input).score"),
            Pure("a:int\r b:int\r c:int\r d:real\r e:bool\r o1 = a + b + c\r o2:real = (a + b) * d\r o3 = if(e) a else b\r o4 = max(a, max(b, c))\r o5 = [a, b, c].map(rule it * 2)"),
            IntX("x:int\r abs(v) = if(v >= 0) v else -v\r y = abs(x) + abs(x - 10)"),
            IntX("x:int\r y = [1,2,3,4,5].map(rule it * x)"),
            IntX("x:int\r y = [1,2,3,4,5].filter(rule it > x).count()"),
            IntX("x:int\r y = [1,2,3,4,5].fold(rule it1 + it2) + x"),
        }) allScripts.Add(("Medium", s));

        // Complex
        foreach (var s in new[] {
            Pure("twiceSet(arr,i,j,ival,jval) = arr.set(i,ival).set(j,jval)\r swap(arr, i, j) = arr.twiceSet(i,j,arr[j], arr[i])\r swapIfNotSorted(c, i) = if(c[i]<c[i+1]) c else c.swap(i, i+1)\r onelineSort(input) = [0..input.count()-2].fold(input, swapIfNotSorted)\r bubbleSort(input) = [0..input.count()-1].fold(input, rule onelineSort(it1))\r i:int[]  = [1,4,3,2,5].bubbleSort()\r r:real[] = [1,4,3,2,5].bubbleSort()"),
            Pure("fact(n):int = if(n<=1) 1 else fact(n-1)*n\r fibrec(n, iter, p1, p2) = if(n > iter) fibrec(n, iter+1, p1+p2, p1) else p1+p2\r fib(n) = if(n<3) 1 else fibrec(n-1, 2, 1, 1)\r y1 = fact(7)\r y2 = fib(10)"),
            Pure("fact(n:int) = if(n<=1) {res = 1} else {res = fact(n-1).res * n}\r transform(p) = {name = p.name; score = p.age * 2; eligible = p.age >= 18 and p.active}\r people = [{name='Alice'; age=5; active=true}, {name='Bob'; age=3; active=true}]\r results = people.map(rule transform(it))\r totalScore = results.map(rule it.score).fold(rule it1 + it2)\r factResult = fact(7).res"),
            Pure("ins:int[] = [1,5,3,5,6,1,2,100,0,3,2,10]\r sorted = ins.sort()\r evens = ins.filter(rule it % 2 == 0)\r total = ins.fold(rule it1 + it2)\r texts = ins.map(rule it.toText())\r hasZero = 0 in ins"),
            IntX("x:int\r fact(n):int = if(n<=1) 1 else fact(n-1)*n\r y = fact(max(1, x % 10))"),
            IntX("x:int\r myMax(a,b) = if(a > b) a else b\r myMin(a,b) = if(a < b) a else b\r clamp(v, lo, hi) = myMax(lo, myMin(v, hi))\r y:int[] = [1,2,3,4,5,6,7,8,9,10].map(rule clamp(it + x, 3, 7))"),
        }) allScripts.Add(("Complex", s));

        // Deduplicate by script text (Simple overlaps PureArith/CallExtern)
        var seen = new HashSet<string>();
        var unique = new List<(string Subset, BenchScript Script)>();
        foreach (var item in allScripts) {
            if (seen.Add(item.Script.Script))
                unique.Add(item);
        }

        var sb = new StringBuilder();
        int buildFails = 0, runFails = 0, ok = 0;

        foreach (var (subset, bench) in unique) {
            var label = $"[{subset}] {bench.Script.Replace("\r", "\\r")}";
            string buildError = null;
            string runError = null;

            VMRuntime vm = null;
            try {
                if (bench.Inputs.Count > 0) {
                    var builder = Funny.Hardcore;
                    foreach (var kv in bench.Inputs)
                        builder = builder.WithApriori(kv.Key, FunnyType.Int32);
                    vm = builder.BuildVM(bench.Script);
                } else {
                    vm = Funny.Hardcore.BuildVM(bench.Script);
                }
            } catch (Exception ex) {
                buildError = $"{ex.GetType().Name}: {ex.Message}";
                if (ex.InnerException != null)
                    buildError += $" --> {ex.InnerException.GetType().Name}: {ex.InnerException.Message}";
            }

            // SetInput separately to distinguish build vs setinput errors
            string setInputError = null;
            if (vm != null && bench.Inputs.Count > 0) {
                try {
                    foreach (var kv in bench.Inputs)
                        vm.SetInput(kv.Key, kv.Value);
                } catch (Exception ex) {
                    setInputError = $"{ex.GetType().Name}: {ex.Message}";
                }
            }

            if (vm != null && setInputError == null) {
                try {
                    vm.Run();
                } catch (Exception ex) {
                    runError = $"{ex.GetType().Name}: {ex.Message}";
                    if (ex.InnerException != null)
                        runError += $" --> {ex.InnerException.GetType().Name}: {ex.InnerException.Message}";
                    // Also grab stack trace top for root cause
                    var lines = ex.StackTrace?.Split('\n');
                    if (lines is { Length: > 0 })
                        runError += $"\n    at: {lines[0].Trim()}";

                    // Dump bytecode for diagnosis
                    try {
                        var field = typeof(VMRuntime).GetField("_program",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var program = (CompiledProgram)field.GetValue(vm);
                        var dump = new StringBuilder();
                        dump.AppendLine($"    Bytecode ({program.Code.Length}B, {program.Constants.Length} consts, {program.LocalsCount} locals, {program.ExternFunctions.Length} externs):");
                        dump.AppendLine($"    Raw bytes: [{string.Join(",", program.Code)}]");
                        int dip = 0;
                        while (dip < program.Code.Length && dip < 200) {
                            var dop = (Op)program.Code[dip];
                            dump.Append($"      [{dip:D4}] {dop}");
                            switch (dop) {
                                case Op.LoadConstI: case Op.LoadConstR: case Op.LoadConstRef:
                                case Op.LoadLocal: case Op.StoreLocal: case Op.LoadNone:
                                // Superinstructions with 1 arg (2 bytes total)
                                case Op.AddTopConstI: case Op.MulTopConstI:
                                case Op.AddTopConstR: case Op.MulTopConstR:
                                    var idx = program.Code[dip + 1];
                                    dump.Append($" #{idx}");
                                    dip += 2; break;
                                // Superinstructions with 2 args (3 bytes total)
                                case Op.AddLocalConstI: case Op.SubLocalConstI: case Op.MulLocalConstI:
                                case Op.AddConstConstI: case Op.MulConstConstI:
                                case Op.AddLocalConstR: case Op.MulLocalConstR:
                                    dump.Append($" #{program.Code[dip+1]},#{program.Code[dip+2]}");
                                    dip += 3; break;
                                case Op.StoreHalt:
                                    dump.Append($" #{program.Code[dip+1]}");
                                    dip += 2; break;
                                case Op.CallExtern:
                                    dump.Append($" func#{program.Code[dip+1]}({program.Code[dip+2]} args)");
                                    dip += 3; break;
                                case Op.Call: case Op.TailCall:
                                    dump.Append($" @{program.Code[dip+1] | (program.Code[dip+2]<<8)} ({program.Code[dip+3]} args)");
                                    dip += 4; break;
                                case Op.Jump: case Op.JumpIfFalse: case Op.JumpIfTrue:
                                    dump.Append($" -> {program.Code[dip+1] | (program.Code[dip+2]<<8)}");
                                    dip += 3; break;
                                default: dip++; break;
                            }
                            dump.AppendLine();
                        }
                        dump.AppendLine("    Constants:");
                        for (int ci = 0; ci < program.Constants.Length; ci++)
                            dump.AppendLine($"      [{ci}] I64={program.Constants[ci].I64} Real={program.Constants[ci].Real} Ref={program.Constants[ci].Ref}");
                        dump.AppendLine("    Variables:");
                        foreach (var v in program.Variables)
                            dump.AppendLine($"      slot#{v.Slot} {v.Name}:{v.Type} {(v.IsOutput ? "[OUT]" : "[IN]")}");
                        runError += "\n" + dump.ToString();
                    } catch { }
                }
            }

            if (buildError != null) {
                buildFails++;
                sb.AppendLine($"BUILD FAIL | {label}");
                sb.AppendLine($"  Error: {buildError}");
                sb.AppendLine();
            } else if (setInputError != null) {
                runFails++;
                sb.AppendLine($"SETINPUT FAIL | {label}");
                sb.AppendLine($"  Error: {setInputError}");
                sb.AppendLine($"  Available vars: [{string.Join(", ", vm.VariableNames)}]");
                sb.AppendLine();
            } else if (runError != null) {
                runFails++;
                sb.AppendLine($"RUN FAIL   | {label}");
                sb.AppendLine($"  Error: {runError}");
                sb.AppendLine();
            } else {
                ok++;
                sb.AppendLine($"OK         | {label}");
            }
        }

        sb.AppendLine();
        sb.AppendLine($"=== SUMMARY: {ok} OK, {buildFails} build fails, {runFails} run fails (total {unique.Count}) ===");

        TestContext.WriteLine(sb.ToString());

        // Write to a temp file so output is always accessible
        var reportPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "vm_bench_crash_report.txt");
        System.IO.File.WriteAllText(reportPath, sb.ToString());
        Console.WriteLine(sb.ToString());

        // Fail the test if any scripts crashed, so the report is shown
        if (buildFails + runFails > 0)
            Assert.Fail($"{buildFails} build fails + {runFails} run fails. Report at: {reportPath}");
    }

    [Test]
    public void DumpBytecode_SimpleArithmetic() {
        var expr = "y = 2 * x + 1";
        var vm = Funny.Hardcore.WithApriori("x", FunnyType.Int32).BuildVM(expr);

        // Access program via reflection
        var field = typeof(VMRuntime).GetField("_program",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var program = (CompiledProgram)field.GetValue(vm);

        // Check if using register VM (code starts with register opcodes)
        var regField = typeof(VMRuntime).GetField("_regCode",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (regField?.GetValue(vm) is byte[] regCode) {
            var sb2 = new StringBuilder();
            sb2.AppendLine($"Expression: {expr} (REGISTER VM)");
            sb2.AppendLine($"Code length: {regCode.Length} bytes");
            for (int i = 0; i < regCode.Length; i += 4)
                sb2.AppendLine($"  [{i:D4}] op={regCode[i]:X2} dst={regCode[i+1]} src1={regCode[i+2]} src2={regCode[i+3]}");
            TestContext.WriteLine(sb2.ToString());
            return;
        }

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
