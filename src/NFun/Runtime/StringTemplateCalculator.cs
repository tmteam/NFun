using System.Collections.Generic;
using System.Linq;
using System.Text;
using NFun.Types;

namespace NFun.Runtime;

public class StringTemplateCalculator {
    private readonly FunnyRuntime _runtime;
    private readonly IReadOnlyList<string> _texts;
    private readonly IReadOnlyList<IFunnyVar> _outputVariables;

    internal StringTemplateCalculator(
        FunnyRuntime runtime, IReadOnlyList<string> texts, IReadOnlyList<IFunnyVar> outputVariables) {
        _runtime = runtime;
        _texts = texts;
        _outputVariables = outputVariables;
    }

    /// <summary>
    /// Returns input variable with given name (ignore case). Returns null if variable is not found
    /// </summary>
    /// <param name="name"></param>
    public IFunnyVar this[string name] {
        get {
            var variable = _runtime[name];
            return variable is not { IsOutput: true }
                ? variable
                : null;
        }
    }

    /// <summary>
    /// Input variable
    /// </summary>
    public IEnumerable<IFunnyVar> Variables => _runtime.Variables.Where(i => !i.IsOutput);

    /// <summary>
    /// Calculates a string based on the values of input variables. Not-thread-safe.
    /// Use Clone to run in parallel
    /// </summary>
    public string Calculate() {
        _runtime.Run();
        var sb = new StringBuilder(_texts[0]);
        for (int i = 0; i < _outputVariables.Count; i++)
        {
            sb.Append(TypeHelper.GetFunText(_outputVariables[i].FunnyValue));
            sb.Append(_texts[i + 1]);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Creates deep copy of current calculator, that can be used in different thread
    /// </summary>
    public StringTemplateCalculator Clone() {
        var clone = _runtime.Clone();
        var cloneOutputs = new List<VariableSource>(_outputVariables.Count);
        foreach (var outputVariable in _outputVariables)
            cloneOutputs.Add(clone.VariableDictionary.GetOrNull(outputVariable.Name));

        return new StringTemplateCalculator(clone, _texts, cloneOutputs);
    }
}
