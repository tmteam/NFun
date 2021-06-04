using System.Collections.Generic;
using NFun.Runtime;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Nodes
{
    public class StructInitExpressionNode : IExpressionNode
    {
        private readonly string[] _fieldNames;
        private readonly IExpressionNode[] _elements;
        
        public StructInitExpressionNode(string[] fieldNames, IExpressionNode[] elements, Interval interval, VarType type)
        {
            Type = type;
            _fieldNames = fieldNames;
            _elements = elements;
            Interval = interval;
        }
        public Interval Interval { get; }
        public VarType Type { get; }
        public object Calc() 
        {
            var fields = new Dictionary<string,object>(_fieldNames.Length);
            for (var i = 0; i < _fieldNames.Length; i++)
                fields.Add(_fieldNames[i],_elements[i].Calc());
            
            return new FunnyStruct(fields);
        }
    }
}