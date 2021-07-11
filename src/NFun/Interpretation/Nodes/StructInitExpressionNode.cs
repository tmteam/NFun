using System.Collections.Generic;
using NFun.Runtime;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpretation.Nodes
{
    public class StructInitExpressionNode : IExpressionNode
    {
        private readonly string[] _fieldNames;
        private readonly IExpressionNode[] _elements;
        
        public StructInitExpressionNode(string[] fieldNames, IExpressionNode[] elements, Interval interval, FunnyType type)
        {
            Type = type;
            _fieldNames = fieldNames;
            _elements = elements;
            Interval = interval;
        }
        public Interval Interval { get; }
        public FunnyType Type { get; }
        public object Calc() 
        {
            var fields = new Dictionary<string,object>(_fieldNames.Length);
            for (var i = 0; i < _fieldNames.Length; i++)
                fields.Add(_fieldNames[i],_elements[i].Calc());
            
            return new FunnyStruct(fields);
        }
    }
}