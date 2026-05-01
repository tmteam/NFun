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
}
