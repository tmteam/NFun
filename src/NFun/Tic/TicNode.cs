using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NFun.Tic.SolvingStates;
using NFun.TypeInferenceCalculator;

namespace NFun.Tic
{
    public enum TicNodeType
    {
        Named,
        SyntaxNode,
        TypeVariable
    }

    public class TicNode
    {
        internal bool Registrated = false;
        private ITicNodeState _state;
        public int GraphId { get; set; } = -1;
        public static TicNode CreateTypeNode(ITypeState type) 
            => new TicNode(type.ToString(), type, TicNodeType.TypeVariable);

        private static int _interlockedId = 0;
        private readonly int Uid = 0;

        public TicNode(string name, ITicNodeState state, TicNodeType type)
        {
            Uid =  Interlocked.Increment(ref _interlockedId);
            
            Name = name;
            State = state;
            Type = type;
        }

        public TicNodeType Type { get; }
        public List<TicNode> Ancestors { get; } = new List<TicNode>();
        public bool IsMemberOfAnything { get; set; }
        public bool IsSolved => _state is StatePrimitive || (_state as StateArray)?.IsSolved == true;

        public ITicNodeState State
        {
            get => _state;
            set
            {
                Debug.Assert(value != null);
                Debug.Assert(!(IsSolved && !value.Equals(_state)),"Node is already solved");

                if (value is StateArray array)
                    array.ElementNode.IsMemberOfAnything = true;
                else
                {
                    Debug.Assert(!(value is StateRefTo refTo && refTo.Node== this),"Self referencing node");
                }
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


        public bool TryBecomeConcrete(StatePrimitive primitiveState)
        {
            if (this.State is StatePrimitive oldConcrete)
                return oldConcrete.Equals(primitiveState);
            if (this.State is ConstrainsState constrains)
            {
                if (!constrains.Fits(primitiveState))
                    return false;
                this.State = primitiveState;
                return true;
            }

            return false;
        }
        public bool TrySetAncestor(StatePrimitive anc)
        {
            if (anc.Equals(StatePrimitive.Any))
                return true;
            var node = this;
            if (node.State is StateRefTo)
                node = node.GetNonReference();

            if (node.State is StatePrimitive oldConcrete)
            {
                return oldConcrete.CanBeImplicitlyConvertedTo(anc);
            }
            else if (node.State is ConstrainsState constrains)
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
        public TicNode GetNonReference()
        {
            var result = this;
            if (result.State is StateRefTo referenceA)
            {
                result = referenceA.Node;
                if (result.State is StateRefTo)
                    return result.GetNonReference();
            }

            return result;
        }
        public override int GetHashCode() => Uid;
    }
}
