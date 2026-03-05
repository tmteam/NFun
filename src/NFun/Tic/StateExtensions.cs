namespace NFun.Tic;

using System;
using System.Collections.Generic;
using System.Linq;
using SolvingStates;
using static SolvingStates.StatePrimitive;

public static class StateExtensions {
    #region lca

    public static ITicNodeState Lca(this ITicNodeState a, ITicNodeState b) {
        if (a is StateRefTo aref)
            return Lca(aref.Element, b);
        if (b is StateRefTo bref)
            return Lca(a, bref.Element);
        if (a is ConstraintsState ac && b is ConstraintsState bc)
        {
            // Both are constraints — compute LCA of their concretest forms
            var descA = ac.HasDescendant ? ac.Descendant : null;
            var descB = bc.HasDescendant ? bc.Descendant : null;
            ITicNodeState lcaDesc = (descA, descB) switch {
                (null, null) => null,
                (null, _)    => Concretest(descB),
                (_, null)    => Concretest(descA),
                _            => Lca(descA, descB)
            };
            var comparable = ac.IsComparable && bc.IsComparable;
            if (lcaDesc == null)
                return ConstraintsState.Of(isComparable: comparable);
            // If result is a solved concrete type and no comparable constraint — return it directly
            if (lcaDesc is ITypeState { IsSolved: true } && !comparable)
                return lcaDesc;
            return ConstraintsState.Of(lcaDesc, null, comparable);
        }
        if (b is ConstraintsState bc2)
            return bc2.HasDescendant ? Lca(a, bc2.Descendant) : Concretest(a);
        if (a is ConstraintsState)
            return Lca(b, a);
        if (a is StatePrimitive ap)
            return b is StatePrimitive bp ? ap.GetLastCommonPrimitiveAncestor(bp) : Any;
        if (b is StatePrimitive)
            return Any;
        if (a is StateArray aarr)
            return b is StateArray barr ? StateArray.Of(Lca(aarr.Element, barr.Element)) : Any;
        if (a is StateFun af)
            return b is StateFun bf ? Lca(af, bf) : Any;
        if (a is StateStruct astruct)
            return b is StateStruct bstruct ? Lca(astruct, bstruct) : Any;
        return Any;
    }

    private static ITicNodeState Lca(this StateStruct a, StateStruct b) {
        // Struct LCA = intersection of field names, with covariant field type LCA.
        // Covariance is sound because NFun structs are immutable (read-only fields).
        var nodes = new Dictionary<string, TicNode>();
        foreach (var aField in a.Fields)
        {
            var bField = b.GetFieldOrNull(aField.Key);
            if (bField == null) continue;

            // When at least one field is solved, use covariant Lca.
            // When both are unsolved constraints, use UnifyOrNull to
            // correctly intersect constraint intervals.
            ITicNodeState fieldType;
            if (aField.Value.IsSolved || bField.IsSolved)
                fieldType = Lca(aField.Value.State, bField.State);
            else
                fieldType = UnifyOrNull(aField.Value.State, bField.State);

            if (fieldType == null) continue;
            nodes.Add(aField.Key, TicNode.CreateInvisibleNode(fieldType));
        }

        return new StateStruct(nodes, true);
    }

    private static ITicNodeState Lca(this StateFun a, StateFun b) {
        if (a.ArgsCount != b.ArgsCount)
            return Any;
        var returnState = Lca(a.ReturnType, b.ReturnType);
        var argNodes = new TicNode[a.ArgsCount];

        for (var i = 0; i < a.ArgNodes.Length; i++)
        {
            var aNode = a.ArgNodes[i];
            var bNode = b.ArgNodes[i];
            var gcd = Gcd(aNode.State, bNode.State);
            if (gcd == null)
                return Any;
            argNodes[i] = TicNode.CreateInvisibleNode(gcd);
        }

        return StateFun.Of(argNodes, TicNode.CreateInvisibleNode(returnState));
    }

    #endregion

    #region gcd

    public static ITicNodeState Gcd(this ITicNodeState a, ITicNodeState b) {
        if (b is StateRefTo bref)
            return Gcd(a, bref.Element);
        if (a is StateRefTo aref)
            return Gcd(aref.Element, b);
        if (a is ConstraintsState ac)
            return ac.Ancestor != null ? Gcd(ac.Ancestor, b) : Abstractest(b);
        if (b is ConstraintsState bc)
            return bc.Ancestor != null ? Gcd(a, bc.Ancestor) : Abstractest(a);
        if (a is StatePrimitive ap)
            return b is StatePrimitive bp ? ap.GetFirstCommonDescendantOrNull(bp) :
                a.Equals(Any) ? Abstractest(b) : null;
        if (b.Equals(Any))
            return Abstractest(a);
        if (a.GetType() != b.GetType())
            return null;
        if (a is StateArray arrA)
            return Gcd(arrA, (StateArray)b);
        if (a is StateFun funA)
            return Gcd(funA, (StateFun)b);
        if (a is StateStruct astruct)
            return Gcd(astruct, (StateStruct)b);
        throw new NotSupportedException($"GCD is not supported for types {a} and {b}");
    }

    private static ITicNodeState Gcd(this StateArray arrA, StateArray arrB) {
        var lcd = Gcd(arrA.Element, arrB.Element);
        if (lcd == null)
            return null;
        return StateArray.Of(lcd);
    }

    private static ITicNodeState Gcd(this StateStruct a, StateStruct b) {
        var nodes = new Dictionary<string, TicNode>();
        // GCD of structs = union of all fields. For shared fields, compute GCD of field types.
        var keys = a.Fields.Select(f => f.Key).Union(b.Fields.Select(f => f.Key));
        foreach (var name in keys)
        {
            var aField = a.GetFieldOrNull(name);
            var bField = b.GetFieldOrNull(name);
            if (aField == null)
                nodes.Add(name, bField);
            else if (bField == null)
                nodes.Add(name, aField);
            else
            {
                var fieldGcd = Gcd(aField.State, bField.State);
                if (fieldGcd == null)
                    return null;
                nodes.Add(name, TicNode.CreateInvisibleNode(fieldGcd));
            }
        }

        return new StateStruct(nodes, isFrozen: true);
    }

    private static ITicNodeState Gcd(this StateFun funA, StateFun funB) {
        if (funA.ArgsCount != funB.ArgsCount)
            return null;
        var args = new ITicNodeState[funA.ArgsCount];
        for (int i = 0; i < funA.ArgsCount; i++)
            args[i] = Lca(funA.ArgNodes[i].State, funB.ArgNodes[i].State);

        var retType = Gcd(funA.ReturnType, funB.ReturnType);
        if (retType == null)
            return null;
        return StateFun.Of(args, retType);
    }

    #endregion

    #region  concretest

    /// <summary>
    /// Returns most possible concrete type, that can be represented by current state (without convertion)
    /// </summary>
    public static ITicNodeState Concretest(this ITicNodeState a) =>
        a switch {
            StatePrimitive => a,
            ConstraintsState cs => cs.HasDescendant
                ? cs.Descendant.Concretest()
                : ConstraintsState.Of(isComparable: cs.IsComparable),
            StateArray arr => StateArray.Of(arr.Element.Concretest()),
            StateRefTo aref => aref.Element.Concretest(),
            StateFun f => f.Concretest(),
            StateStruct s => s.ConcretestFields(),
            _ => a
        };

    private static ITicNodeState Concretest(this StateFun f) {
        // return type is classic, but arg type is contravariant
        var returnNode = TicNode.CreateInvisibleNode(Concretest(f.ReturnType));
        var argNodes = new TicNode[f.ArgsCount];
        var i = 0;
        foreach (var node in f.ArgNodes)
        {
            argNodes[i] = TicNode.CreateInvisibleNode(node.State.Abstractest());
            i++;
        }

        return StateFun.Of(argNodes, returnNode);
    }

    private static StateStruct ConcretestFields(this StateStruct s) {
        var nodes = new Dictionary<string, TicNode>(s.FieldsCount);
        bool changed = false;
        foreach (var (key, fieldNode) in s.Fields)
        {
            var nr = fieldNode.GetNonReference();
            if (nr != fieldNode)
                changed = true;
            nodes[key] = nr;
        }

        return changed ? new StateStruct(nodes, s.IsFrozen) : s;
    }

    #endregion

    #region abstractest

    /// <summary>
    /// Returns most possible abstract type, that can be represented by current state (without convertion)
    /// </summary>
    public static ITicNodeState Abstractest(this ITicNodeState a) =>
        a switch {
            StateRefTo aref => aref.Element.Abstractest(),
            ConstraintsState cs => cs.IsComparable ? cs : cs.HasAncestor ? cs.Ancestor : Any,
            StatePrimitive => a,
            StateArray arr => StateArray.Of(arr.Element.Abstractest()),
            StateFun f => f.Abstractest(),
            StateStruct => a,
            _ => a
        };

    private static ITicNodeState Abstractest(this StateFun f) {
        var returnNode = TicNode.CreateInvisibleNode(Abstractest(f.ReturnType));
        var argNodes = new TicNode[f.ArgsCount];
        var i = 0;
        foreach (var node in f.ArgNodes)
        {
            argNodes[i] = TicNode.CreateInvisibleNode(node.State.Concretest());
            i++;
        }

        return StateFun.Of(argNodes, returnNode);
    }

    #endregion

    #region uni

    public static ITicNodeState UnifyOrNull(this ITicNodeState a, ITicNodeState b) {
        if (a.Equals(b))
            return a;

        if (a is StateRefTo ar)
            return UnifyOrNull(ar.GetNonReference(), b);
        if (b is StateRefTo br)
            return UnifyOrNull(a, br.GetNonReference());

        // Any is the universal ancestor — any type is convertible to Any
        if (a.Equals(Any)) return Any;
        if (b.Equals(Any)) return Any;

        if (a is ConstraintsState ac)
        {
            if (b is ConstraintsState bc)
                return UnifyOrNull(ac, bc);
            else if (b.FitsInto(ac))
                return b;
            else
                return null;
        }

        if (b is ConstraintsState)
            return UnifyOrNull(b, a);
        if (a.GetType() != b.GetType())
            return null;
        if (a is StatePrimitive)
            return null;
        if (b is StatePrimitive)
            return null;
        if (a is StateArray aArr)
            return UnifyOrNull(aArr, b as StateArray);
        if (a is StateFun aFun)
            return UnifyOrNull(aFun, b as StateFun);
        if (a is StateStruct aStr)
            return UnifyOrNull(aStr, b as StateStruct);
        throw new NotSupportedException($"Unitype({a}, {b})");
    }

    private static ITicNodeState UnifyOrNull(this ConstraintsState a, ConstraintsState b) {
        var comparable = a.IsComparable || b.IsComparable;
        ITicNodeState descendant = null;
        if (!a.HasDescendant)
            descendant = b.Descendant;
        else if (!b.HasDescendant)
            descendant = a.Descendant;
        else
            descendant = Lca(a.Descendant, b.Descendant);

        StatePrimitive ancestor = null;
        if (!a.HasAncestor)
            ancestor = b.Ancestor;
        else if (!b.HasAncestor)
            ancestor = a.Ancestor;
        else
        {
            ancestor = a.Ancestor.GetFirstCommonDescendantOrNull(b.Ancestor);
            if (ancestor == null)
                return null;
        }

        return ConstraintsState.Of(descendant, ancestor, comparable).SimplifyOrNull();
    }

    private static ITicNodeState UnifyOrNull(this StateArray a, StateArray b) {
        var uniElement = UnifyOrNull(a.Element, b.Element);
        return uniElement == null ? null : StateArray.Of(uniElement);
    }

    private static ITicNodeState UnifyOrNull(this StateFun a, StateFun b) {
        if (a.ArgsCount != b.ArgsCount)
            return null;
        var argNodes = new TicNode[a.ArgsCount];
        for (int i = 0; i < a.ArgsCount; i++)
        {
            var uniArg = UnifyOrNull(a.ArgNodes[i].State, b.ArgNodes[i].State);
            if (uniArg == null)
                return null;
            argNodes[i] = TicNode.CreateInvisibleNode(uniArg);
        }

        var retArg = UnifyOrNull(a.ReturnType, b.ReturnType);
        if (retArg == null)
            return null;
        return StateFun.Of(argNodes, TicNode.CreateInvisibleNode(retArg));
    }

    private static ITicNodeState UnifyOrNull(this StateStruct a, StateStruct b) {
        if (a.FieldsCount != b.FieldsCount)
            return null;
        var fields = new Dictionary<string, TicNode>();
        foreach (var aField in a.Fields)
        {
            var bField = b.GetFieldOrNull(aField.Key);
            if (bField == null)
                return null;
            var uniField = UnifyOrNull(aField.Value.State, bField.State);
            if (uniField == null)
                return null;
            fields.Add(aField.Key, TicNode.CreateInvisibleNode(uniField));
        }

        return new StateStruct(fields, true);
    }

    #endregion

    #region convert

    /// <summary>
    /// `from` can be converted to `to` in SOME case
    /// </summary>
    public static bool CanBeConvertedOptimisticTo(this ITicNodeState from, ITicNodeState to) {
        if (from is StateRefTo fromRef)
            return CanBeConvertedOptimisticTo(fromRef.Element, to);
        if (to is StateRefTo toRef)
            return CanBeConvertedOptimisticTo(from, toRef.Element);
        if (to.Equals(Any))
            return true;
        if (to is ICompositeState compositeState)
            return CanBeConvertedOptimisticTo(from, compositeState);
        if (from is StatePrimitive)
            return to is StatePrimitive top2
                ? from.CanBePessimisticConvertedTo(top2)
                : to is ConstraintsState toConstraints2 &&
                  (!toConstraints2.HasAncestor || from.CanBePessimisticConvertedTo(toConstraints2.Ancestor));
        if (from is ConstraintsState fromConstraints)
        {
            if (fromConstraints.NoConstrains)
                return true;
            if (to is ConstraintsState toConstraints)
            {
                var ancestor = toConstraints.Ancestor;
                // if there is no ancestor, than anything can be possibly converted to 'to'
                if (ancestor == null || Equals(ancestor, Any))
                    return true;
                //if there is ancestor, then either 'from.ancestor` either `from.desc` has to be converted to it
                if (fromConstraints.HasAncestor && CanBeConvertedOptimisticTo(fromConstraints.Ancestor, ancestor))
                    return true;
                if (fromConstraints.HasDescendant && CanBeConvertedOptimisticTo(fromConstraints.Descendant, ancestor))
                    return true;
            }

            if (to is StatePrimitive toP)
                return CanBeConvertedOptimisticTo(fromConstraints, toP);
        }

        if (from is ICompositeState)
        {
            if (to is ConstraintsState constrainsState)
                // if there is no ancestor, than anything can be possibly converted to 'to'
                return constrainsState.Ancestor == null || Equals(constrainsState.Ancestor, Any);
            return false;
        }

        if (to is StatePrimitive toPrimitive)
            return from.CanBePessimisticConvertedTo(toPrimitive);
        return false;
    }

    public static bool CanBeConvertedOptimisticTo(this ConstraintsState from, StatePrimitive to) {
        if (from.Ancestor?.CanBePessimisticConvertedTo(to) == true)
            return true;

        if (from.HasDescendant)
        {
            var concretest = from.Descendant.Concretest();
            if (concretest is ConstraintsState { HasAncestor : false, HasDescendant: false })
                return true;
            return concretest.CanBePessimisticConvertedTo(to);
        }

        if (from.HasAncestor)
            return to.CanBePessimisticConvertedTo(from.Ancestor);

        if (from.IsComparable)
            return to.IsComparable || to.Equals(Any);
        else
            return true;
    }

    /// <summary>
    /// `from` can be converted to `to` in SOME case
    /// </summary>
    public static bool CanBeConvertedOptimisticTo(this ITicNodeState from, ICompositeState to) {
        if (from is StateRefTo r)
            return CanBeConvertedOptimisticTo(r.Element, to);
        if (from is ConstraintsState constrainsState)
            return !constrainsState.HasDescendant;
        if (from.GetType() != to.GetType())
            return false;
        return from switch {
            StateArray arrayFrom => CanBeConvertedOptimisticTo(arrayFrom.Element, ((StateArray)to).Element),
            StateFun funFrom => CanBeConvertedOptimisticTo(funFrom, (StateFun)to),
            StateStruct structFrom => CanBeConvertedOptimisticTo(structFrom, (StateStruct)to),
            _ => throw new NotSupportedException($"{from} is not supported for CanBeConvertedPessimistic ")
        };
    }

    public static bool CanBeConvertedOptimisticTo(this StateFun from, StateFun to) {
        if (from.ArgsCount != to.ArgsCount)
            return false;
        if (!from.ReturnType.CanBeConvertedOptimisticTo(to.ReturnType))
            return false;
        for (int i = 0; i < from.ArgsCount; i++)
        {
            var fromType = from.ArgNodes[i].State;
            var toType = to.ArgNodes[i].State;
            if (!toType.CanBeConvertedOptimisticTo(fromType))
                return false;
        }

        return true;
    }

    public static bool CanBeConvertedOptimisticTo(this StateStruct from, StateStruct to) {
        if (to.FieldsCount > from.FieldsCount)
            return false;
        foreach (var toField in to.Fields)
        {
            var fromField = from.GetFieldOrNull(toField.Key);
            if (fromField == null)
                return false;
            var unitype = UnifyOrNull(fromField.State, toField.Value.State);
            if (unitype == null)
                return false;
        }

        return true;
    }


    /// <summary>
    /// `from` can be converted to `to` in ANY case
    /// </summary>
    private static bool CanBeConvertedPessimisticTo(this ICompositeState from, ConstraintsState to) {
        if (to.NoConstrains)
            return true;
        if (to.Ancestor != null && !from.CanBePessimisticConvertedTo(to.Ancestor))
            return false;
        if (to.IsComparable)
            return from is StateArray array && CanBeConvertedPessimisticTo(from: StatePrimitive.Char, array.Element);
        // so state has to be converted to descendant, to allow this
        var toDescendant = to.Descendant;
        if (Equals(toDescendant, Any))
            return true;
        if (toDescendant is ICompositeState toComposite)
            return CanBeConvertedPessimisticTo(from, toComposite);
        return false;
    }

    /// <summary>
    /// `from` can be converted to `to` in ANY case
    /// </summary>
    public static bool CanBeConvertedPessimisticTo(this ITicNodeState from, ITicNodeState to) {
        if (to is StateRefTo ancRef)
            return CanBeConvertedPessimisticTo(from, ancRef.Element);
        if (to.Equals(Any))
            return true;
        return from switch {
            StateRefTo descRef => CanBeConvertedPessimisticTo(descRef.Element, to),
            StatePrimitive fromPrim => to switch {
                StatePrimitive p => fromPrim.CanBePessimisticConvertedTo(p),
                ConstraintsState c => (!c.IsComparable || fromPrim.IsComparable)
                                     && c.HasDescendant && CanBeConvertedPessimisticTo(fromPrim, c.Descendant),
                _ => false
            },
            ConstraintsState fromDesc => fromDesc.HasAncestor
                ? CanBeConvertedPessimisticTo(fromDesc.Ancestor, to)
                : CanBeConvertedPessimisticTo(Any, to),
            ICompositeState comp => to switch {
                ConstraintsState constrAnc => CanBeConvertedPessimisticTo(comp, constrAnc),
                ICompositeState composite => CanBeConvertedPessimisticTo(comp, composite),
                _ => false
            }
        };
    }

    private static bool CanBeConvertedPessimisticTo(ICompositeState from, ICompositeState to) =>
        from switch {
            StateArray arrayDesc => to switch {
                StateArray arrayAnc => CanBeConvertedPessimisticTo(arrayDesc.Element, arrayAnc.Element),
                _ => false
            },
            StateFun fromFun => to switch {
                StateFun toFun => CanBeConvertedPessimisticTo(fromFun, toFun),
                _ => false
            },
            StateStruct fromStr => to switch {
                StateStruct toStr => CanBeConvertedPessimisticTo(fromStr, toStr),
                _ => false
            },
            _ => throw new NotSupportedException($"type {from} is not supported for pessimistic convertion")
        };

    private static bool CanBeConvertedPessimisticTo(StateFun from, StateFun to) {
        if (from.ArgsCount != to.ArgsCount)
            return false;
        if (!from.ReturnType.CanBeConvertedPessimisticTo(to.ReturnType))
            return false;
        for (int i = 0; i < from.ArgsCount; i++)
        {
            if (!to.ArgNodes[i].State.CanBeConvertedPessimisticTo(from.ArgNodes[i].State))
                return false;
        }

        return true;
    }

    private static bool CanBeConvertedPessimisticTo(this StateStruct from, StateStruct to) {
        if (to.FieldsCount > from.FieldsCount)
            return false;
        foreach (var toField in to.Fields)
        {
            if (!toField.Value.IsSolved)
                return false;
            var fromField = from.GetFieldOrNull(toField.Key);
            if (fromField == null || !fromField.IsSolved)
                return false;
            if (toField.Value.State.StateDescription != fromField.State.StateDescription)
                return false;
        }

        return true;
    }

    #endregion

    #region fit

    public static bool FitsInto(this ITicNodeState target, ConstraintsState to) {
        if (to.HasAncestor && !target.CanBePessimisticConvertedTo(to.Ancestor))
            return false;
        if (to.IsComparable)
        {
            if (target is ICompositeState)
            {
                // the only comparable composite is arr(char)
                if (!(target is StateArray a))
                    return false;
                if (!a.Element.Equals(StatePrimitive.Char))
                    return false;
            }

            if (target is StatePrimitive tp)
                return tp.IsComparable;
        }
        return to.Descendant == null || CanBeFitConverted(to.Descendant, target);
    }


    public static bool FitsInto(this ITicNodeState target, ITicNodeState to) {
        if (to is StateRefTo toR)
            return FitsInto(target, toR.GetNonReference());
        if (to is ConstraintsState constrainsState)
            return target.FitsInto(constrainsState);

        return target switch {
            StateArray targetA => to is StateArray arrTo
                ? targetA.Element.FitsInto(arrTo.Element)
                : to.Equals(Any),
            StateFun targetF => to is StateFun funTo
                ? targetF.FitsInto(funTo)
                : to.Equals(Any),
            StateStruct targetS => to is StateStruct structTo
                ? targetS.FitsInto(structTo)
                : to.Equals(Any),
            StatePrimitive => to is StatePrimitive p && target.CanBePessimisticConvertedTo(p),
            ConstraintsState { HasDescendant: true } fc => FitsInto(fc.Descendant, to),
            ConstraintsState => true,
            StateRefTo targetR=> FitsInto(targetR.GetNonReference(), to),
            _ => throw new NotImplementedException($"Type {target} :> {to} is not supported in FIT")
        };
    }

    private static bool FitsInto(this StateStruct from, StateStruct to) {
        // 'from' has to have every field from 'to'
        // every 'from' field has to fit into 'to' field
        foreach (var toField in to.Fields)
        {
            var fromField = from.GetFieldOrNull(toField.Key);
            if (fromField == null) return false;
            if (!FitsInto(fromField.State, toField.Value.State))
                return false;
        }
        return true;
    }

    private static bool FitsInto(this StateFun target, StateFun to) {
        if (target.ArgsCount != to.ArgsCount)
            return false;
        //var returnBottomDesc = from.ReturnType.Concretest();
        //var returnBottomAnc = to.ReturnType.Concretest();
        if (!FitsInto(target.ReturnType, to.ReturnType))
            return false;
        for (int i = 0; i < target.ArgsCount; i++)
        {
            //var dmin = stateFun.ArgNodes[i].State.Abstractest();
            //var amin = typeFun.ArgNodes[i].State.Abstractest();
            if (!to.ArgNodes[i].State.FitsInto(target.ArgNodes[i].State))
                return false;
        }
        return true;
    }

    /// <summary>
    /// For any 'to' value, there exist 'desc' value, that can be pessimisticly converted to 'to'
    /// </summary>
    private static bool CanBeFitConverted(ITicNodeState desc, ITicNodeState to) {
        if (to is StateRefTo rto)
            return CanBeFitConverted(desc, rto.Element);
        if (to.Equals(Any))
            return true;

        return desc switch {
            StateRefTo descRef => CanBeFitConverted(descRef.Element, to),
            StatePrimitive descP => to switch {
                StatePrimitive toP => descP.CanBePessimisticConvertedTo(toP),
                ConstraintsState toC => toC.Descendant!=null && descP.CanBeConvertedPessimisticTo(toC.Descendant),
                _ => false
            },
            ConstraintsState fromDesc => fromDesc.Descendant==null || CanBeFitConverted(fromDesc.Descendant, to),
            ICompositeState comp => to switch {
                ConstraintsState constrAnc => constrAnc.Descendant is ICompositeState toComposite && CanBeFitConverted(comp, toComposite),
                ICompositeState composite => CanBeFitConverted(comp, composite),
                _ => false
            },
            _ => throw new NotSupportedException($"CBFC does not support {desc} to {to}")
        };
    }

    /// <summary>
    /// For any 'to' value, there exist 'desc' value, that can be pessimisticly converted to 'to'
    /// </summary>
    private static bool CanBeFitConverted(ICompositeState desc, ICompositeState to) {
        if (desc.GetType() != to.GetType())
            return false;
        return desc switch {
            StateArray descA  => CanBeFitConverted(descA.Element, ((StateArray)to).Element),
            StateFun descF    => CanBeFitConverted(descF, (StateFun)to),
            StateStruct descS => CanBeFitConverted(descS, (StateStruct)to),
            _ => false
        };
    }

    /// <summary>
    /// For any 'to' value, there exist 'desc' value, that can be pessimisticly converted to 'to'
    /// </summary>
    private static bool CanBeFitConverted(StateStruct desc, StateStruct to) {
        //'to' has to contains all the fields from desc.
        foreach (var (dname, dstate) in desc.Fields)
        {
            var astate = to.GetFieldOrNull(dname);
            if (astate == null)
                return false;
            if (!dstate.State.CanBeConvertedPessimisticTo(astate.State))
                return false;
        }

        return desc.FitsInto(to);
    }


    private static bool CanBeFitConverted(StateFun desc, StateFun to) {
        if (desc.ArgsCount != to.ArgsCount) return false;
        if (!CanBeFitConverted(desc.ReturnType, to.ReturnType))
            return false;
        for (int i = 0; i < to.ArgsCount; i++)
        {
            var descArg = desc.ArgNodes[i].State;
            var toArg = desc.ArgNodes[i].State;
            if (!CanBeFitConverted(toArg, descArg))
                return false;
        }
        return true;
    }

    #endregion
}
