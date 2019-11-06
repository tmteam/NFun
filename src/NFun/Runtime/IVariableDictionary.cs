using NFun.Interpritation.Nodes;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Runtime
{
    public interface IVariableDictionary
    {

        bool Contains(string id);
        /// <summary>
        /// Returns false if variable is already registered
        /// </summary>
        bool TryAdd(VariableSource source);

        /// <summary>
        /// Returns false if variable is already registered
        /// </summary>
        bool TryAdd(VariableUsages usages);

        VariableSource GetSourceOrNull(string id);
        VariableExpressionNode CreateVarNode(string id, Interval interval, VarType type);
        VariableUsages GetUsages(string id);
        VariableUsages[] GetAllUsages();
        VariableSource[] GetAllSources();
    }
}