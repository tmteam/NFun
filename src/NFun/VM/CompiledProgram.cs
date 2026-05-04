using NFun.Interpretation.Functions;
using NFun.Types;

namespace NFun.VM;

/// <summary>
/// Immutable compiled bytecode program. Shared across all executions (read-only).
/// </summary>
public class CompiledProgram {
    public byte[] Code { get; init; }
    public FunValue[] Constants { get; init; }
    public StructLayout[] StructLayouts { get; init; }
    public ExternFunc[] ExternFunctions { get; init; }
    public UserFunc[] UserFunctions { get; init; }
    public VariableSlot[] Variables { get; init; }
    public ExceptionHandler[] ExceptionHandlers { get; init; }
    public FunnyType[] TypeTable { get; init; }
    public int LocalsCount { get; init; }
    public int MaxStackDepth { get; init; }
    public bool HasExceptionHandlers { get; init; }

}

/// <summary>Struct type layout: field names and types, resolved at compile time.</summary>
public class StructLayout {
    public string[] FieldNames { get; init; }
    public FunnyType[] FieldTypes { get; init; }
}

/// <summary>External (.NET) function reference.</summary>
public struct ExternFunc {
    public IConcreteFunction Function;
    public FunnyType ReturnType;
    public FunnyType[] ArgTypes;
    /// <summary>Cached arity dispatch: 1=FunctionWithSingleArg, 2=FunctionWithTwoArgs, 0=generic</summary>
    public byte ArityKind;
}

/// <summary>User-defined function metadata.</summary>
public struct UserFunc {
    public int EntryIP;
    public int LocalsCount;
    public string Name;
    public FunnyType ReturnType;
    public FunnyType[] ArgTypes;
    /// <summary>Pre-allocated locals for non-recursive calls. Avoids allocation per call.</summary>
    public FunValue[] CachedLocals;
    /// <summary>True while CachedLocals is in use (detects recursion).</summary>
    public bool CachedLocalsInUse;
}

/// <summary>Input/output variable slot.</summary>
public struct VariableSlot {
    public string Name;
    public int Slot;
    public FunnyType Type;
    public bool IsOutput;
}

/// <summary>Exception handler entry (try-catch).</summary>
public struct ExceptionHandler {
    public int TryStartIP;
    public int TryEndIP;
    public int CatchStartIP;
    public int ErrorVarSlot;
    public int SavedSP;
}

/// <summary>Call frame for VM call stack.</summary>
public struct CallFrame {
    public int ReturnIP;
    public int ReturnSP;
    public FunValue[] CallerLocals;
    public int FunctionId;
}
