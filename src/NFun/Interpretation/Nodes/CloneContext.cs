using System.Collections.Generic;
using NFun.Exceptions;
using NFun.Interpretation.Functions;
using NFun.Runtime;

namespace NFun.Interpretation.Nodes;

public interface ICloneContext {
    VariableSource GetVariableSourceClone(VariableSource origin);
    ICloneContext GetScopedContext(VariableSource[] sources);
    IConcreteFunction GetUserFunctionClone(IConcreteFunction concreteUserFunction);
    void AddUserFunctionClone(IConcreteFunction origin, IConcreteFunction clone);
}

public class CloneContext : ICloneContext {
    private readonly IReadonlyVariableDictionary _dictionaryCopy;
    private readonly Dictionary<IConcreteFunction, IConcreteFunction> _userFunctions = new();
    internal CloneContext(IReadonlyVariableDictionary dictionaryCopy) => _dictionaryCopy = dictionaryCopy;

    public VariableSource GetVariableSourceClone(VariableSource origin) => 
        _dictionaryCopy.GetOrNull(origin.Name) ?? throw new NFunImpossibleException($"Source '{origin.Name}' is not found");

    public ICloneContext GetScopedContext(VariableSource[] sources)=>
        new ScopeCloneContext(this, new VariableDictionary(sources, sources.Length));

    public IConcreteFunction GetUserFunctionClone(IConcreteFunction concreteUserFunction)
    {
        _userFunctions.TryGetValue(concreteUserFunction, out var result);
        return result;
    }

    public void AddUserFunctionClone(IConcreteFunction origin, IConcreteFunction clone) => 
        _userFunctions.Add(origin,clone);


    class ScopeCloneContext: ICloneContext {
        private readonly ICloneContext _outerScopeContext;
        private readonly IReadonlyVariableDictionary _scopeReadonlyVariables;

        public ScopeCloneContext(ICloneContext outerScopeContext, IReadonlyVariableDictionary scopeReadonlyVariables)
        {
            _outerScopeContext = outerScopeContext;
            _scopeReadonlyVariables = scopeReadonlyVariables;
        }

        public VariableSource GetVariableSourceClone(VariableSource origin) => 
            _scopeReadonlyVariables.GetOrNull(origin.Name) ?? _outerScopeContext.GetVariableSourceClone(origin);

        public ICloneContext GetScopedContext(VariableSource[] sources) =>
            new ScopeCloneContext(this, new VariableDictionary(sources, sources.Length));

        public IConcreteFunction GetUserFunctionClone(IConcreteFunction concreteUserFunction) => 
            _outerScopeContext.GetUserFunctionClone(concreteUserFunction);

        public void AddUserFunctionClone(IConcreteFunction origin, IConcreteFunction clone) => 
            _outerScopeContext.AddUserFunctionClone(origin, clone);
    }
}