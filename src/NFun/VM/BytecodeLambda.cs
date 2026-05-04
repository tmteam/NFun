using NFun.Interpretation.Functions;
using NFun.Types;

namespace NFun.VM;

/// <summary>
/// IConcreteFunction wrapper that re-enters the VM to execute a lambda's bytecode.
/// Created by MakeClosure opcode. Passed to map/filter/fold as the callback.
/// </summary>
internal sealed class BytecodeLambda1 : FunctionWithSingleArg {
    private readonly CompiledProgram _program;
    private readonly int _funcId;
    private readonly FunValue[] _captured;
    private readonly int _argCount;

    public BytecodeLambda1(CompiledProgram program, int funcId, FunValue[] captured) {
        _program = program;
        _funcId = funcId;
        ref var uf = ref program.UserFunctions[funcId];
        _argCount = uf.ArgTypes.Length;
        _captured = captured;
        Name = uf.Name;
        ArgTypes = new[] { uf.ArgTypes[0] };
        ReturnType = uf.ReturnType;
    }

    public override object Calc(object a) {
        ref var uf = ref _program.UserFunctions[_funcId];
        var locals = new FunValue[uf.LocalsCount];
        locals[0] = FunValue.Unbox(a, uf.ArgTypes[0]);
        if (_captured != null)
            for (int i = 0; i < _captured.Length; i++)
                locals[_argCount + i] = _captured[i];

        var stack = new FunValue[8];
        VirtualMachine.ExecuteSubroutine(_program, locals, stack, uf.EntryIP);
        return stack[0].Box(uf.ReturnType);
    }
}

/// <summary>Arity-2 variant for fold(seed, rule(acc, elem) = ...).</summary>
internal sealed class BytecodeLambda2 : FunctionWithTwoArgs {
    private readonly CompiledProgram _program;
    private readonly int _funcId;
    private readonly FunValue[] _captured;
    private readonly int _argCount;

    public BytecodeLambda2(CompiledProgram program, int funcId, FunValue[] captured) {
        _program = program;
        _funcId = funcId;
        ref var uf = ref program.UserFunctions[funcId];
        _argCount = uf.ArgTypes.Length;
        _captured = captured;
        Name = uf.Name;
        ArgTypes = uf.ArgTypes;
        ReturnType = uf.ReturnType;
    }

    public override object Calc(object a, object b) {
        ref var uf = ref _program.UserFunctions[_funcId];
        var locals = new FunValue[uf.LocalsCount];
        locals[0] = FunValue.Unbox(a, uf.ArgTypes[0]);
        locals[1] = FunValue.Unbox(b, uf.ArgTypes[1]);
        if (_captured != null)
            for (int i = 0; i < _captured.Length; i++)
                locals[_argCount + i] = _captured[i];

        var stack = new FunValue[8];
        VirtualMachine.ExecuteSubroutine(_program, locals, stack, uf.EntryIP);
        return stack[0].Box(uf.ReturnType);
    }
}
