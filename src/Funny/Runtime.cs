using System;
using System.Collections.Generic;
using System.Linq;
using Funny.Take2;

namespace Funny
{
    public class Runtime
    {
        private readonly string _name;
        private IExpressionNode Node;
        

        public string[] Variables => _variables.Keys.ToArray();

        private Dictionary<string, VariableExpressionNode> _variables;
        private Dictionary<string, IExpressionNode> _equatations 
            = new Dictionary<string, IExpressionNode>();

        public Runtime(string name, IExpressionNode node, Dictionary<string, VariableExpressionNode> variables)
        {
            _name = name;
            Node = node;
            _variables = variables;
        }


        public CalculationResult Calculate(params Variable[] variables)
        {
            foreach (var value in variables)
            {
                var varName = value.Name;
                if (_variables.TryGetValue(varName, out var varNode))
                    varNode.SetValue(value.Value);
                else
                    throw new ArgumentException(value.Name);
            }
            var val = Node.Calc();
            return new CalculationResult(new []{Variable.New(_name, val), });
        }
    }
}