using System;
using System.Linq;
using NFun.Types;

namespace NFun.Interpritation.Functions
{
    public abstract class GenericFunctionBase: IGenericFunction
    {
        public GenericConstrains[] Constrainses { get; }
        
        private readonly int _maxGenericId;
        public string Name { get; }
        public VarType[] ArgTypes { get; }

        protected GenericFunctionBase(string name, VarType returnType, 
            params VarType[] argTypes)
        {
            Name = name;
            ArgTypes = argTypes;
            ReturnType = returnType;
                var maxGenericId = argTypes
                    .Append(returnType)
                    .Max(i => i.SearchMaxGenericTypeId());
                if (!maxGenericId.HasValue)
                    throw new InvalidOperationException($"Type {name} has wrong generic defenition");

                Constrainses = new GenericConstrains[maxGenericId.Value + 1];

                for (int i = 0; i <= maxGenericId; i++)
                    Constrainses[i] = GenericConstrains.Any;
                _maxGenericId = maxGenericId.Value;
        }
        protected GenericFunctionBase(string name, GenericConstrains[] constrainses, VarType returnType,
            params VarType[] argTypes)
        {
            Name = name;
            ArgTypes = argTypes;
            ReturnType = returnType;
            Constrainses = constrainses;
            var maxGenericId = argTypes
                .Append(returnType)
                .Max(i => i.SearchMaxGenericTypeId());
            if (!maxGenericId.HasValue)
                throw new InvalidOperationException($"Type {name} has wrong generic defenition");
        }

        protected GenericFunctionBase(string name, GenericConstrains constrains, VarType returnType,
            params VarType[] argTypes)
        {
            Name = name;
            ArgTypes = argTypes;
            ReturnType = returnType;
            Constrainses = new []{constrains};
            var maxGenericId = argTypes
                .Append(returnType)
                .Max(i => i.SearchMaxGenericTypeId());
            if (!maxGenericId.HasValue)
                throw new InvalidOperationException($"Type {name} has wrong generic defenition");
        }


        public VarType ReturnType { get; }

        protected virtual object Calc(object[] args) => throw new NotImplementedException();

        public virtual IConcreteFunction CreateConcrete(VarType[] concreteTypesMap) =>
            new ConcreteGenericFunction(
                calc: Calc,
                name: Name,
                returnType: VarType.SubstituteConcreteTypes(ReturnType, concreteTypesMap),
                argTypes:   SubstitudeArgTypes(concreteTypesMap));

        protected VarType[] SubstitudeArgTypes(VarType[] concreteTypes)
        {
            var concreteArgTypes = new VarType[ArgTypes.Length];
            for (int i = 0; i < concreteArgTypes.Length; i++)
                concreteArgTypes[i] = VarType.SubstituteConcreteTypes(ArgTypes[i], concreteTypes);
            return concreteArgTypes;
        }

        public IConcreteFunction CreateConcreteOrNull(VarType outputType, params VarType[] concreteArgTypes)
        {
            if (concreteArgTypes.Length != ArgTypes.Length)
                return null;
            
            var solvingParams = new VarType[_maxGenericId+1];

            for (int i = 0; i < ArgTypes.Length; i++)
            {
                if (!VarType.TrySolveGenericTypes(
                    genericArguments: solvingParams,
                    genericType: ArgTypes[i],
                    concreteType: concreteArgTypes[i],
                    strict: false
                ))
                    return null;
            }

            if (!VarType.TrySolveGenericTypes(
                genericArguments: solvingParams, 
                genericType: ReturnType, 
                concreteType: outputType, 
                strict:true))
                return null;

            foreach (var solvingParam in solvingParams)
            {
                if(solvingParam.BaseType== BaseVarType.Empty)
                    throw new InvalidOperationException($"Incorrect function defenition: {TypeHelper.GetFunSignature(ReturnType,ArgTypes)}. Not all generic types can be solved");
            }     
            
            return new ConcreteGenericFunction(
                calc: Calc, 
                name: Name, 
                returnType:  VarType.SubstituteConcreteTypes(ReturnType, solvingParams), 
                argTypes: SubstitudeArgTypes(solvingParams));
        }
        /// <summary>
        /// calculates generic call arguments  based on a concrete call signature
        /// </summary> 
        public VarType[] CalcGenericArgTypeList(FunTypeSpecification funTypeSpecification)
        {
            var result = new VarType[Constrainses.Length];
            SubstitudeType(ReturnType, funTypeSpecification.Output);

            for (int i = 0; i < funTypeSpecification.Inputs.Length; i++)
            {
                SubstitudeType(ArgTypes[i], funTypeSpecification.Inputs[i]);
            }

            return result;

            bool SubstitudeType(VarType genericOrConcrete, VarType concrete)
            {
                var id = genericOrConcrete.GenericId;
                if (id.HasValue)
                {
                    result[id.Value] = concrete;
                    return true;
                }

                if (genericOrConcrete.ArrayTypeSpecification != null)
                    return SubstitudeType(genericOrConcrete.ArrayTypeSpecification.VarType,
                        concrete.ArrayTypeSpecification.VarType);

                if (genericOrConcrete.FunTypeSpecification != null)
                {
                    SubstitudeType(genericOrConcrete.FunTypeSpecification.Output, concrete.FunTypeSpecification.Output);
                    for (int i = 0; i < genericOrConcrete.FunTypeSpecification.Inputs.Length; i++)
                    {
                        SubstitudeType(genericOrConcrete.FunTypeSpecification.Inputs[i],
                            concrete.FunTypeSpecification.Inputs[i]);
                    }
                    return true;
                }

                return false;
            }
        }

        public override string ToString()
            => TypeHelper.GetFunSignature(Name, ReturnType, ArgTypes);


        public class ConcreteGenericFunction: FunctionWithManyArguments
        {
            private readonly Func<object[], object> _calc;

            public ConcreteGenericFunction(Func<object[],object> calc, string name,  VarType returnType, params VarType[] argTypes) 
                : base(TypeHelper.GetFunSignature(name,returnType,argTypes), returnType, argTypes)
            {
                _calc = calc;
            }

            public override object Calc(object[] args) => _calc(args);
        }

      
    }
}