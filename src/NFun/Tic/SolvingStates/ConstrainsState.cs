using System;
using System.Linq;

namespace NFun.Tic.SolvingStates
{
    public class ConstrainsState: ITicNodeState
    {
        public ConstrainsState(ITypeState desc = null, StatePrimitive anc = null)
        {
            Descedant = desc;
            Ancestor = anc;
        }

        public ConstrainsState GetCopy()
        {
            var result = new ConstrainsState(Descedant, Ancestor)
            {
                IsComparable = this.IsComparable,
                Prefered = this.Prefered
            };
            return result;
        }

        public bool Fits(ITypeState type)
        {
            if (HasAncestor)
            {
                if (!type.CanBeImplicitlyConvertedTo(Ancestor))
                    return false;
            }

            if (type is StatePrimitive primitive)
            {
                if (HasDescendant)
                {
                    if (!Descedant.CanBeImplicitlyConvertedTo(primitive))
                        return false;
                }

                if (IsComparable && !primitive.IsComparable)
                    return false;
                return true;
            }
            else if (type is StateArray array)
            {
                if (IsComparable)
                    return false;
                if (!HasDescendant)
                    return true;
                if (!(Descedant is StateArray descArray))
                    return false;
                if (array.Element.Equals(descArray.Element))
                    return true;
                if (!array.IsSolved || !descArray.IsSolved)
                    return false;
                return false;
            }
            else if (type is StateFun fun)
            {
                if (IsComparable)
                    return false;
                if (!HasDescendant)
                    return true;
                if (!(Descedant is StateFun descfun))
                    return false;
                if (fun.Members.SequenceEqual(descfun.Members))
                    return true;
                if (!fun.IsSolved || !descfun.IsSolved)
                    return false;
            }
            throw new NotSupportedException();
        }

        public bool Fits(StatePrimitive primitiveState)
        {
            if (HasAncestor)
            {
                if (!primitiveState.CanBeImplicitlyConvertedTo(Ancestor))
                    return false;
            }

            if (HasDescendant)
            {
                if (!Descedant.CanBeImplicitlyConvertedTo(primitiveState))
                    return false;
            }

            if (IsComparable && !primitiveState.IsComparable)
                return false;
            return true;
        }

        public StatePrimitive Ancestor { get; private set; }
        public ITypeState Descedant { get; private set; }

        public bool HasAncestor => Ancestor!=null;
        public bool HasDescendant => Descedant!=null;

        public bool TryAddAncestor(StatePrimitive type)
        {
            if (type == null)
                return true;

            if (Ancestor == null)
                Ancestor = type;
            else
            {
                var res = Ancestor.GetFirstCommonDescendantOrNull(type);
                if (res == null)
                    return false;
                Ancestor = res;
            }

            return true;
        }
        public void AddAncestor(StatePrimitive type)
        {
            if(!TryAddAncestor(type))
                throw new InvalidOperationException();
        }
        public void AddDescedant(StatePrimitive type)
        {

            if (type == null)
                return;
            if (Descedant == null)
                Descedant = type;
            else
            {
                var ancestor = Descedant.GetLastCommonAncestorOrNull(type);
                if (ancestor != null)
                    Descedant = ancestor;
            }
        }
        public void AddDescedant(ITypeState type)
        {
            
            if(type==null)
                return;
            if(!type.IsSolved)
                return;

            if (Descedant == null)
                Descedant = type;
            else
            {
                var ancestor = Descedant.GetLastCommonAncestorOrNull(type);
                if(ancestor!=null)
                    Descedant = ancestor;
            }
        }
        public StatePrimitive Prefered { get; set; }
        public bool IsComparable { get; set; }
        public bool NoConstrains => !HasDescendant && !HasAncestor && !IsComparable;

        public ITicNodeState MergeOrNull(ConstrainsState constrainsState)
        {
            var result = new ConstrainsState(Descedant,Ancestor)
            {
                IsComparable = this.IsComparable || constrainsState.IsComparable
            };
            result.AddDescedant(constrainsState.Descedant);

            if (!result.TryAddAncestor(constrainsState.Ancestor))
                return null;

            if (result.HasAncestor && result.HasDescendant)
            {
                var anc = result.Ancestor;
                var des = result.Descedant;
                if (anc.Equals(des))
                {
                    if (result.IsComparable && !anc.IsComparable)
                        return null;
                    return anc;
                }
                if (!des.CanBeImplicitlyConvertedTo(anc))
                    return null;
            }

            if (Prefered == null)
                result.Prefered = constrainsState.Prefered;
            else if (constrainsState.Prefered == null)
                result.Prefered = Prefered;
            else if (constrainsState.Prefered.Equals(Prefered))
                result.Prefered = Prefered;
            else if (result.Fits(Prefered) && !result.Fits(constrainsState.Prefered))
                result.Prefered = Prefered;
            else
                result.Prefered = constrainsState.Prefered;
            
            if(result.Prefered!=null)
                if (!result.Fits(result.Prefered))
                    result.Prefered = null;

            return result;
        }
        /// <summary>
        /// Try to infer most generic type if is possible
        /// Return self otherwise
        ///
        /// For most cases it means that ancestor type will be used
        /// </summary>
        public ITicNodeState SolveCovariant()
        {
            if (Prefered != null && Fits(Prefered))
                return Prefered;
            var ancestor = Ancestor ?? StatePrimitive.Any;
            if (IsComparable)
            {
                if (ancestor.IsComparable)
                    return ancestor;
                else 
                    return this;
            }
            if (Descedant is StateArray)
                return Descedant;
            return ancestor;
        }
        /// <summary>
        /// Try to infer most CONCRETE type if is possible
        /// Return self otherwise
        ///
        /// For most cases it means that descedant type will be used
        /// </summary>

        public ITicNodeState SolveContravariant()
        {
            if (Prefered != null && Fits(Prefered))
                return Prefered;
            if (!HasDescendant)
                return this;

            if (IsComparable)
            {
                //todo
                //char[] is comparable!
                if (!(Descedant is StatePrimitive p) || !p.IsComparable)
                    return this;
            }
            return Descedant;
        }

        public ITicNodeState GetOptimizedOrNull()
        {
            if (IsComparable)
            {
                if (Descedant != null)
                {
                    if (Descedant.Equals(StatePrimitive.Char))
                        return StatePrimitive.Char;

                    if (Descedant is StatePrimitive primitive && primitive.IsNumeric)
                    {
                        if (!TryAddAncestor(StatePrimitive.Real))
                            return null;
                    }
                    else if (Descedant is StateArray a && a.Element.Equals(StatePrimitive.Char))
                        return Descedant;
                    else
                        return null;
                }
            }

            if (HasAncestor && HasDescendant)
            {
                if (Ancestor.Equals(Descedant))
                    return Ancestor;
                if (!Descedant.CanBeImplicitlyConvertedTo(Ancestor))
                    return null;

            }

            if (Descedant?.Equals(StatePrimitive.Any) == true)
                return StatePrimitive.Any;

            return this;
        }
   
        public override string ToString()
        {
            var res = $"[{Descedant}..{Ancestor}]";
            if (IsComparable)
                res += "<>";
            if (Prefered != null)
                res += Prefered + "!";
            return res;
        }

        public string Description => ToString();

        public bool ApplyDescendant(IStateFunctionsSet visitor, TicNode ancestorNode, TicNode descendantNode) =>
            descendantNode.State.Apply(visitor, ancestorNode, descendantNode, this);
        public bool Apply(IStateFunctionsSet visitor, TicNode ancestorNode, TicNode descendantNode, StatePrimitive ancestor)
            => visitor.Apply(ancestor,this,ancestorNode, descendantNode);
        public bool Apply(IStateFunctionsSet visitor, TicNode ancestorNode, TicNode descendantNode, ConstrainsState ancestor)
            => visitor.Apply( ancestor,this,ancestorNode, descendantNode);
        public bool Apply(IStateFunctionsSet visitor, TicNode ancestorNode, TicNode descendantNode, ICompositeState ancestor)
            => visitor.Apply(ancestor,this,ancestorNode, descendantNode);
    }
}