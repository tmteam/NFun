namespace NFun;

/// <summary>
/// Represents a lazily evaluated value.
/// Lazy arguments in functions are passed as ILazyFunnyValue instead of
/// their computed result. The function calls Calc() when/if it needs the value.
/// </summary>
public interface ILazyFunnyValue {
    object Calc();
}
