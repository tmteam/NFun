using System;
using System.Linq;

namespace NFun.Tic.SolvingStates
{
    public class Constrains: IState
    {
        public Constrains(IType desc = null, Primitive anc = null)
        {
            Descedant = desc;
            Ancestor = anc;
        }

        public Constrains GetCopy()
        {
            var result = new Constrains(Descedant, Ancestor)
            {
                IsComparable = this.IsComparable,
                Prefered = this.Prefered
            };
            return result;
        }

        public Primitive TrySolveOrNull()
        {
            if (Prefered != null && Fits(Prefered))
                return Prefered;
            if (IsComparable)
                return null;
            if (HasAncestor)
                return Ancestor;
            
            return Primitive.Any;
        }
        public bool Fits(IType type)
        {
            if (HasAncestor)
            {
                if (!type.CanBeImplicitlyConvertedTo(Ancestor))
                    return false;
            }

            if (type is Primitive primitive)
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
            else if (type is Array array)
            {
                if (IsComparable)
                    return false;
                if (!HasDescendant)
                    return true;
                if (!(Descedant is Array descArray))
                    return false;
                if (array.Element.Equals(descArray.Element))
                    return true;
                if (!array.IsSolved || !descArray.IsSolved)
                    return false;
                return false;
            }
            else if (type is Fun fun)
            {
                if (IsComparable)
                    return false;
                if (!HasDescendant)
                    return true;
                if (!(Descedant is Fun descfun))
                    return false;
                if (fun.Members.SequenceEqual(descfun.Members))
                    return true;
                if (!fun.IsSolved || !descfun.IsSolved)
                    return false;
            }
            throw new NotSupportedException();
        }

        public bool Fits(Primitive primitive)
        {
            if (HasAncestor)
            {
                if (!primitive.CanBeImplicitlyConvertedTo(Ancestor))
                    return false;
            }

            if (HasDescendant)
            {
                if (!Descedant.CanBeImplicitlyConvertedTo(primitive))
                    return false;
            }

            if (IsComparable && !primitive.IsComparable)
                return false;
            return true;
        }

        public Primitive Ancestor { get; private set; }
        public IType Descedant { get; private set; }

        public bool HasAncestor => Ancestor!=null;
        public bool HasDescendant => Descedant!=null;

        public bool TryAddAncestor(Primitive type)
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
        public void AddAncestor(Primitive type)
        {
            if(!TryAddAncestor(type))
                throw new InvalidOperationException();
        }
        public void AddDescedant(Primitive type)
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
        public void AddDescedant(IType type)
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
        public Primitive Prefered { get; set; }
        public bool IsComparable { get; set; }
        public bool NoConstrains => !HasDescendant && !HasAncestor && !IsComparable;

        public IState MergeOrNull(Constrains constrains)
        {
            var result = new Constrains(Descedant,Ancestor)
            {
                IsComparable = this.IsComparable || constrains.IsComparable
            };
            result.AddDescedant(constrains.Descedant);

            if (!result.TryAddAncestor(constrains.Ancestor))
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
                result.Prefered = constrains.Prefered;
            else if (constrains.Prefered == null)
                result.Prefered = Prefered;
            else if (constrains.Prefered.Equals(Prefered))
                result.Prefered = Prefered;
            else if (result.Fits(Prefered) && !result.Fits(constrains.Prefered))
                result.Prefered = Prefered;
            else
                result.Prefered = constrains.Prefered;
            
            if(result.Prefered!=null)
                if (!result.Fits(result.Prefered))
                    result.Prefered = null;

            return result;
        }
        /// <summary>
        /// Пытается вывести тип если это возможно
        /// Возвращает себя если это не возможно
        /// </summary>
        public IState Solve()
        {
            if (Prefered != null && Fits(Prefered))
                return Prefered;
            var ancestor = Ancestor ?? Primitive.Any;
            if (IsComparable)
            {
                if (ancestor.IsComparable)
                    return ancestor;
                else 
                    return this;
            }
            if (Descedant is Array)
                return Descedant;
            return ancestor;
        }
       

        public IState GetOptimizedOrThrow()
        {
            if (IsComparable)
            {
                if (Descedant != null)
                {
                    if (Descedant.Equals(Primitive.Char))
                        return Primitive.Char;
                    
                    if (Descedant is Primitive primitive && primitive.IsNumeric)
                    {
                        if(!TryAddAncestor(Primitive.Real))
                            throw new InvalidOperationException();
                    }
                    else if (Descedant is Array a && a.Element.Equals(Primitive.Char))
                        return Descedant;
                    else
                        throw new InvalidOperationException("Types cannot be compared");
                }
            }

            if (HasAncestor && HasDescendant)
            {
                if(Ancestor.Equals(Descedant))
                    return Ancestor;
                if (!Descedant.CanBeImplicitlyConvertedTo(Ancestor))
                    throw new InvalidOperationException();
            }

            if (Descedant?.Equals(Primitive.Any)==true)
                return Primitive.Any;

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
    }
}