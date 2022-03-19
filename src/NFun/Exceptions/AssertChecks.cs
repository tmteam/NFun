namespace NFun.Exceptions {

internal static class AssertChecks {
    
    /// <summary>
    /// Assert condition
    /// </summary>
    /// <exception cref="NFunImpossibleException"></exception>
    public static void IfFalseThrow(this bool condition, string message) {
        if (!condition)
            throw new NFunImpossibleException("Condition not satisfied: "+ message);        
    }
    /// <summary>
    /// Assert not null
    /// </summary>
    /// <exception cref="NFunImpossibleException"></exception>
    public static void IfNullThrow(this object element, string message) {
        if (element == null)
            throw new NFunImpossibleException("Condition not satisfied: "+ message);        
    }
}

}