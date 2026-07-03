using NFun.Exceptions;
using NFun.Runtime;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpretation;

/// <summary>
/// Spec §Reassignment: "Reassigning a value that doesn't widen into a single type is
/// a parse error." TIC silently LCAs incompatible reassignments to `Any`; these guards
/// reject that post-solve. ONE implementation for all sites (top-level equation, block
/// equation, field assignment) — the three inlined copies had already diverged on the
/// declared-`any` exemption.
///
/// KNOWN LIMIT (review N3): `Any` carries no provenance at the FunnyType level, so a
/// legitimately-inferred `Any` (heterogeneous join) is indistinguishable from an
/// incompatible-reassignment `Any`. Variables use the declared-annotation interval as
/// provenance; named-struct fields consult the named-type registry. The clean fix is
/// TIC-level provenance — tracked with the adapter-heuristics family.
/// </summary>
internal static class ReassignmentGuard {

    /// <summary>Throws iff the existing variable slot widened to Any without a user
    /// annotation while the new RHS is intrinsically non-Any.</summary>
    public static void ThrowIfIncompatibleVariableReassignment(
        string name, VariableSource existing, FunnyType newValueType, Interval errorInterval) {
        if (existing.Type.BaseType == BaseFunnyType.Any
            && existing.TypeSpecificationIntervalOrNull == null
            && newValueType.BaseType != BaseFunnyType.Any)
            throw new FunnyParseException(0,
                $"Cannot reassign '{name}' to a value of incompatible type "
                + $"'{newValueType}' — reassignment requires types that widen into one type.",
                errorInterval);
    }

    /// <summary>Throws iff the struct field widened to Any while the assigned value is
    /// intrinsically non-Any — except when the struct's shape matches a registered
    /// named type whose field is DECLARED `any` (a declared any-field accepts
    /// everything; without this exemption `type t = {v:any}; x.v = 'text'` was
    /// rejected as a false positive).</summary>
    public static void ThrowIfIncompatibleFieldAssignment(
        string fieldName, FunnyType sourceType, FunnyType valueType,
        INamedTypeFieldRegistry namedTypes, Interval errorInterval) {
        if (sourceType.BaseType != BaseFunnyType.Struct
            || sourceType.StructTypeSpecification == null
            || !sourceType.StructTypeSpecification.TryGetValue(fieldName, out var fieldType)
            || fieldType.BaseType != BaseFunnyType.Any
            || valueType.BaseType == BaseFunnyType.Any)
            return;
        if (FieldDeclaredAnyInMatchingNamedType(fieldName, sourceType, namedTypes))
            return;
        throw new FunnyParseException(0,
            $"Cannot assign value of type '{valueType}' to field '.{fieldName}' — "
            + $"field type widened to 'Any' due to incompatible assignments. "
            + $"Reassignment requires types that widen into one type.",
            errorInterval);
    }

    /// <summary>True iff some registered named type structurally matches the source's
    /// field set AND declares <paramref name="fieldName"/> as `any`. FunnyType cannot
    /// carry TypeName and field types at once (TechnicalDebt #8), so declared-ness is
    /// recovered structurally; ambiguity is safe here — the exemption only relaxes a
    /// heuristic rejection.</summary>
    private static bool FieldDeclaredAnyInMatchingNamedType(
        string fieldName, FunnyType sourceType, INamedTypeFieldRegistry namedTypes) {
        if (namedTypes == null)
            return false;
        var spec = sourceType.StructTypeSpecification;
        foreach (var (_, fields) in namedTypes.All) {
            if (fields.Length != spec.Count)
                continue;
            bool matches = true;
            bool fieldIsDeclaredAny = false;
            foreach (var (name, type) in fields) {
                if (!spec.TryGetValue(name, out var specType)
                    || !specType.Equals(type)) {
                    matches = false;
                    break;
                }
                if (string.Equals(name, fieldName, System.StringComparison.OrdinalIgnoreCase)
                    && type.BaseType == BaseFunnyType.Any)
                    fieldIsDeclaredAny = true;
            }
            if (matches && fieldIsDeclaredAny)
                return true;
        }
        return false;
    }
}
