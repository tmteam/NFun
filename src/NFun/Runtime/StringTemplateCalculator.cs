using System.Collections.Generic;
using System.Linq;
using System.Text;
using NFun.Types;

namespace NFun.Runtime; 

public class StringTemplateCalculator {
    private readonly FunnyRuntime _runtime;
    private readonly IReadOnlyList<string> _texts;
    private readonly IReadOnlyList<IFunnyVar> _variables;

    public StringTemplateCalculator(
        FunnyRuntime runtime, IReadOnlyList<string> texts, IReadOnlyList<IFunnyVar> variables) {
        _runtime = runtime;
        _texts = texts;
        _variables = variables;
    }

    /// <summary>
    /// Returns input variable with given name (ignore case). Returns null if variable is not found
    /// </summary>
    /// <param name="name"></param>
    public IFunnyVar this[string name]
    {
        get
        {
            var variable = _runtime[name];
            return variable is not { IsOutput: true }
                ? variable
                : null;
        }
    }

    /// <summary>
    /// Input variable
    /// </summary>
    public IEnumerable<IFunnyVar> Variables => _runtime.VariableDictionary.GetAllSources().Where(i => !i.IsOutput);

    public string Calculate() {
        _runtime.Run();
        var sb = new StringBuilder(_texts[0]);
        for (int i = 0; i < _variables.Count; i++)
        {
            sb.Append(TypeHelper.GetFunText(_variables[i].FunnyValue));
            sb.Append(_texts[i + 1]);
        }

        return sb.ToString();
    }
}