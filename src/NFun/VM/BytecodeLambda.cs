using NFun.Interpretation.Functions;
using NFun.Types;

namespace NFun.VM;

/// <summary>
/// IConcreteFunction wrapper that re-enters the VM to execute a lambda's bytecode.
/// Created by MakeClosure opcode. Passed to map/filter/fold as the callback.
/// Locals and stack are cached — zero allocation per Calc() call.
/// </summary>
internal sealed class BytecodeLambda1 : FunctionWithSingleArg {
    private readonly CompiledProgram _program;
    private readonly int _entryIP;
    private readonly FunnyType _argType;
    private readonly FunnyType _retType;
    private readonly FunValue[] _captured;
    private readonly int _captureOffset; // slot offset where captured vars start
    private readonly FunValue[] _locals;
    private readonly FunValue[] _stack;

    public BytecodeLambda1(CompiledProgram program, int funcId, FunValue[] captured) {
        _program = program;
        ref var uf = ref program.UserFunctions[funcId];
        _entryIP = uf.EntryIP;
        _argType = uf.ArgTypes[0];
        _retType = uf.ReturnType;
        _captured = captured;
        _captureOffset = uf.ArgTypes.Length;
        _locals = new FunValue[uf.LocalsCount];
        _stack = new FunValue[8];
        Name = uf.Name;
        ArgTypes = new[] { _argType };
        ReturnType = _retType;
    }

    public override object Calc(object a) {
        _locals[0] = FunValue.Unbox(a, _argType);
        if (_captured != null)
            for (int i = 0; i < _captured.Length; i++)
                _locals[_captureOffset + i] = _captured[i];
        VirtualMachine.ExecuteSubroutine(_program, _locals, _stack, _entryIP);
        return _stack[0].Box(_retType);
    }

    /// <summary>Zero-boxing fast path: FunValue in → FunValue out. For future VM-native HOF.</summary>
    internal FunValue CalcDirect(FunValue a) {
        _locals[0] = a;
        if (_captured != null)
            for (int i = 0; i < _captured.Length; i++)
                _locals[_captureOffset + i] = _captured[i];
        VirtualMachine.ExecuteSubroutine(_program, _locals, _stack, _entryIP);
        return _stack[0];
    }
}

/// <summary>Arity-2 variant for fold(seed, rule(acc, elem) = ...).</summary>
internal sealed class BytecodeLambda2 : FunctionWithTwoArgs {
    private readonly CompiledProgram _program;
    private readonly int _entryIP;
    private readonly FunnyType _argType0, _argType1;
    private readonly FunnyType _retType;
    private readonly FunValue[] _captured;
    private readonly int _captureOffset;
    private readonly FunValue[] _locals;
    private readonly FunValue[] _stack;

    public BytecodeLambda2(CompiledProgram program, int funcId, FunValue[] captured) {
        _program = program;
        ref var uf = ref program.UserFunctions[funcId];
        _entryIP = uf.EntryIP;
        _argType0 = uf.ArgTypes[0];
        _argType1 = uf.ArgTypes[1];
        _retType = uf.ReturnType;
        _captured = captured;
        _captureOffset = uf.ArgTypes.Length;
        _locals = new FunValue[uf.LocalsCount];
        _stack = new FunValue[8];
        Name = uf.Name;
        ArgTypes = uf.ArgTypes;
        ReturnType = _retType;
    }

    public override object Calc(object a, object b) {
        _locals[0] = FunValue.Unbox(a, _argType0);
        _locals[1] = FunValue.Unbox(b, _argType1);
        if (_captured != null)
            for (int i = 0; i < _captured.Length; i++)
                _locals[_captureOffset + i] = _captured[i];
        VirtualMachine.ExecuteSubroutine(_program, _locals, _stack, _entryIP);
        return _stack[0].Box(_retType);
    }

    /// <summary>Zero-boxing fast path.</summary>
    internal FunValue CalcDirect(FunValue a, FunValue b) {
        _locals[0] = a;
        _locals[1] = b;
        if (_captured != null)
            for (int i = 0; i < _captured.Length; i++)
                _locals[_captureOffset + i] = _captured[i];
        VirtualMachine.ExecuteSubroutine(_program, _locals, _stack, _entryIP);
        return _stack[0];
    }
}
