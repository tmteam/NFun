using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Tic.SolvingStates;
using NFun.TypeInferenceCalculator;
using Array = NFun.Tic.SolvingStates.Array;

namespace NFun.Tic
{
    public enum SolvingNodeType
    {
        Named,
        SyntaxNode,
        TypeVariable
    }

    public class SolvingNode
    {
        internal bool Registrated = false;
        private IState _state;
        public int GraphId { get; set; } = -1;
        public static SolvingNode CreateTypeNode(IType type) 
            => new SolvingNode(type.ToString(), type, SolvingNodeType.TypeVariable);

        public SolvingNode(string name, IState state, SolvingNodeType type)
        {
            Name = name;
            State = state;
            Type = type;
        }

        public SolvingNodeType Type { get; }
        public List<SolvingNode> Ancestors { get; } = new List<SolvingNode>();
        public List<SolvingNode> MemberOf { get; } = new List<SolvingNode>();
        public bool IsSolved => _state is Primitive || (_state as Array)?.IsSolved == true;

        public IState State
        {
            get => _state;
            set
            {
                //todo perfomance hotspot
                
                if(value == null)
                    throw new InvalidOperationException();
                if(IsSolved && !value.Equals(_state))
                    throw new InvalidOperationException("Node is already solved");

                if (value is Array array)
                    array.ElementNode.MemberOf.Add(this);
                else if(value is RefTo refTo && refTo.Node== this)
                    throw new InvalidOperationException("Self referencing node");

                _state = value;
            }
        }

        public string Name { get; }
        public override string ToString()
        {
            if (Name == _state.ToString())
                return Name;
            else 
                return $"{Name}:{_state}";
        }

        public void PrintToConsole()
        {
            if(!TraceLog.IsEnabled)
                return;
            
#if DEBUG
            if (TraceLog.IsEnabled)
            {
                TraceLog.Write($"{Name}:", ConsoleColor.Green);
                TraceLog.Write(State.Description);
                if (Ancestors.Any())
                    TraceLog.Write("  <=" + string.Join(",", Ancestors.Select(a => a.Name)));
                TraceLog.WriteLine();
            }
#endif
        }


        public bool TryBecomeConcrete(Primitive primitive)
        {
            if (this.State is Primitive oldConcrete)
                return oldConcrete.Equals(primitive);
            if (this.State is Constrains constrains)
            {
                if (!constrains.Fits(primitive))
                    return false;
                this.State = primitive;
                return true;
            }

            return false;
        }
        public bool TrySetAncestor(Primitive anc)
        {
            if (anc.Equals(Primitive.Any))
                return true;
            var node = this;
            if (node.State is RefTo)
                node = node.GetNonReference();

            if (node.State is Primitive oldConcrete)
            {
                return oldConcrete.CanBeImplicitlyConvertedTo(anc);
            }
            else if (node.State is Constrains constrains)
            {
                if (!constrains.TryAddAncestor(anc))
                    return false;
                var optimized = constrains.GetOptimizedOrNull();
                if (optimized == null)
                    return false;
                State = optimized;
                return true;
            };
            return false;
        }
        public SolvingNode GetNonReference()
        {
            var result = this;
            if (result.State is RefTo referenceA)
            {
                result = referenceA.Node;
                if (result.State is RefTo)
                    return result.GetNonReference();
            }

            return result;
        }
    }
}
