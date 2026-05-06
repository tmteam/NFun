using System.Collections.Generic;
using NFun.SyntaxParsing.SyntaxNodes;

namespace NFun.SyntaxParsing.Visitors;

public class FindFunctionDependenciesVisitor : EnterVisitorBase {
    private readonly string _functionAlias;
    private readonly Dictionary<string, int> _userFunctionsNames;
    private readonly List<int> _dependencies;
    private readonly bool _extensionSeparation;

    public FindFunctionDependenciesVisitor(
        string functionAlias,
        Dictionary<string, int> userFunctionsNames,
        bool extensionSeparation = false) {
        _functionAlias = functionAlias;
        _userFunctionsNames = userFunctionsNames;
        _dependencies = new List<int>(userFunctionsNames.Count);
        _extensionSeparation = extensionSeparation;
    }

    public bool HasSelfRecursion { get; private set; } = false;
    public int[] GetFoundDependencies() => _dependencies.ToArray();

    public override DfsEnterResult Visit(FunCallSyntaxNode node) {
        var nodeName = $"{node.Id}({node.Args.Length})";

        // When extension separation is enabled, piped calls reference extension functions
        // (stored with "." prefix) and direct calls reference regular functions.
        if (_extensionSeparation && node.IsPipeForward)
            nodeName = "." + nodeName;

        if (nodeName == _functionAlias)
            HasSelfRecursion = true;
        else if (_userFunctionsNames.TryGetValue(nodeName, out int id))
            _dependencies.Add(id);

        // When extension separation is enabled, also check the non-prefixed name
        // since piped calls can also call built-in functions (which aren't user functions,
        // so they won't be in the dictionary — this is a no-op for them).
        // For regular calls, also check if they reference extension functions (they shouldn't,
        // but the dependency is still relevant for ordering).
        if (_extensionSeparation)
        {
            var altName = node.IsPipeForward
                ? $"{node.Id}({node.Args.Length})"  // non-prefixed for piped call
                : "." + $"{node.Id}({node.Args.Length})"; // prefixed for direct call
            if (altName != nodeName && altName != _functionAlias
                && _userFunctionsNames.TryGetValue(altName, out int altId)
                && !_dependencies.Contains(altId))
                _dependencies.Add(altId);
        }

        return DfsEnterResult.Continue;
    }
}
