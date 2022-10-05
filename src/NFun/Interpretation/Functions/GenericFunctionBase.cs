using System;
using System.Linq;
using NFun.Interpretation.Nodes;
using NFun.Types;

namespace NFun.Interpretation.Functions; 

public abstract class GenericFunctionBase : IGenericFunction {
    public GenericConstrains[] Constrains { get; }

    private readonly int _maxGenericId;
    public string Name { get; }
    public FunnyType[] ArgTypes { get; }

    protected GenericFunctionBase(
        string name, FunnyType returnType,
        params FunnyType[] argTypes) {
        Name = name;
        ArgTypes = argTypes;
        ReturnType = returnType;
        var maxGenericId = argTypes
                           .Append(returnType)
                           .Max(i => i.SearchMaxGenericTypeId());
        if (!maxGenericId.HasValue)
            throw new InvalidOperationException($"Type {name} has wrong generic definition");

        Constrains = new GenericConstrains[maxGenericId.Value + 1];

        for (int i = 0; i <= maxGenericId; i++)
            Constrains[i] = GenericConstrains.Any;
        _maxGenericId = maxGenericId.Value;
    }

    protected GenericFunctionBase(
        string name, GenericConstrains[] constrains, FunnyType returnType,
        params FunnyType[] argTypes) {
        Name = name;
        ArgTypes = argTypes;
        ReturnType = returnType;
        Constrains = constrains;
        var maxGenericId = argTypes
                           .Append(returnType)
                           .Max(i => i.SearchMaxGenericTypeId());
        if (!maxGenericId.HasValue)
            throw new InvalidOperationException($"Type {name} has wrong generic definition");
    }

    protected GenericFunctionBase(
        string name, GenericConstrains constrains, FunnyType returnType,
        params FunnyType[] argTypes) {
        Name = name;
        ArgTypes = argTypes;
        ReturnType = returnType;
        Constrains = new[] { constrains };
        var maxGenericId = argTypes
                           .Append(returnType)
                           .Max(i => i.SearchMaxGenericTypeId());
        if (!maxGenericId.HasValue)
            throw new InvalidOperationException($"Type {name} has wrong generic definition");
    }
    
    public FunnyType ReturnType { get; }

    protected virtual object Calc(object[] args) => throw new NotImplementedException();

    public virtual IConcreteFunction CreateConcrete(FunnyType[] concreteTypesMap, IFunctionSelectorContext context) =>
        new ConcreteGenericFunction(
            calc: Calc,
            name: Name,
            returnType: FunnyType.SubstituteConcreteTypes(ReturnType, concreteTypesMap),
            argTypes: SubstitudeArgTypes(concreteTypesMap));

    protected FunnyType[] SubstitudeArgTypes(FunnyType[] concreteTypes) {
        var concreteArgTypes = new FunnyType[ArgTypes.Length];
        for (int i = 0; i < concreteArgTypes.Length; i++)
            concreteArgTypes[i] = FunnyType.SubstituteConcreteTypes(ArgTypes[i], concreteTypes);
        return concreteArgTypes;
    }

    public IConcreteFunction CreateConcreteOrNull(FunnyType outputType, params FunnyType[] concreteArgTypes) {
        if (concreteArgTypes.Length != ArgTypes.Length)
            return null;

        var solvingParams = new FunnyType[_maxGenericId + 1];

        for (int i = 0; i < ArgTypes.Length; i++)
        {
            if (!FunnyType.TrySolveGenericTypes(
                genericArguments: solvingParams,
                genericType: ArgTypes[i],
                concreteType: concreteArgTypes[i],
                strict: false
            ))
                return null;
        }

        if (!FunnyType.TrySolveGenericTypes(
            genericArguments: solvingParams,
            genericType: ReturnType,
            concreteType: outputType,
            strict: true))
            return null;

        foreach (var solvingParam in solvingParams)
        {
            if (solvingParam.BaseType == BaseFunnyType.Empty)
                throw new InvalidOperationException(
                    $"Incorrect function definition: {TypeHelper.GetFunSignature(ReturnType, ArgTypes)}. Not all generic types can be solved");
        }

        return new ConcreteGenericFunction(
            calc: Calc,
            name: Name,
            returnType: FunnyType.SubstituteConcreteTypes(ReturnType, solvingParams),
            argTypes: SubstitudeArgTypes(solvingParams));
    }

    /// <summary>
    /// calculates generic call arguments  based on a concrete call signature
    /// </summary> 
    public FunnyType[] CalcGenericArgTypeList(FunTypeSpecification funTypeSpecification) {
        var result = new FunnyType[Constrains.Length];
        SubstitudeType(ReturnType, funTypeSpecification.Output);

        for (int i = 0; i < funTypeSpecification.Inputs.Length; i++)
        {
            SubstitudeType(ArgTypes[i], funTypeSpecification.Inputs[i]);
        }

        return result;

        bool SubstitudeType(FunnyType genericOrConcrete, FunnyType concrete) {
            var id = genericOrConcrete.GenericId;
            if (id.HasValue)
            {
                result[id.Value] = concrete;
                return true;
            }

            if (genericOrConcrete.ArrayTypeSpecification != null)
                return SubstitudeType(
                    genericOrConcrete.ArrayTypeSpecification.FunnyType,
                    concrete.ArrayTypeSpecification.FunnyType);

            if (genericOrConcrete.FunTypeSpecification != null)
            {
                SubstitudeType(genericOrConcrete.FunTypeSpecification.Output, concrete.FunTypeSpecification.Output);
                for (int i = 0; i < genericOrConcrete.FunTypeSpecification.Inputs.Length; i++)
                {
                    SubstitudeType(
                        genericOrConcrete.FunTypeSpecification.Inputs[i],
                        concrete.FunTypeSpecification.Inputs[i]);
                }

                return true;
            }

            return false;
        }
    }

    public override string ToString()
        => TypeHelper.GetFunSignature(Name, ReturnType, ArgTypes);


    private class ConcreteGenericFunction : FunctionWithManyArguments {
        private readonly Func<object[], object> _calc;

        public ConcreteGenericFunction(
            Func<object[], object> calc, string name, FunnyType returnType, params FunnyType[] argTypes)
            : base(TypeHelper.GetFunSignature(name, returnType, argTypes), returnType, argTypes) {
            _calc = calc;
        }

        public override object Calc(object[] args) => _calc(args);
        public override IConcreteFunction Clone(ICloneContext context) => new ConcreteGenericFunction(_calc, Name, ReturnType, ArgTypes);
        
        public override string ToString()
            => $"FUN-concrete-generic {TypeHelper.GetFunSignature(Name, ReturnType, ArgTypes)}";
    }
}