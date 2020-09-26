using System;
using System.Linq;
using System.Security.Cryptography;
using NFun.ParseErrors;
using NFun.Types;

namespace NFun.Interpritation.Functions
{
    public interface IGenericFunction : IFunctionSignature
    {
        IConcreteFunction CreateConcrete(VarType[] concreteTypesMap);

        /// <summary>
        /// calculates generic call arguments  based on a concrete call signature
        /// </summary> 
        VarType[] CalcGenericArgTypeList(FunTypeSpecification funTypeSpecification);
    }

    public abstract class GenericFunctionWithSingleArgument : GenericFunctionBase
    {
        protected GenericFunctionWithSingleArgument(string name, VarType returnType, params VarType[] argTypes) : base(
            name, returnType, argTypes)
        {
        }

        protected GenericFunctionWithSingleArgument(string name, GenericConstrains[] genericDefenitions,
            VarType returnType, params VarType[] argTypes) : base(name, genericDefenitions, returnType, argTypes)
        {
        }

        protected GenericFunctionWithSingleArgument(string name, GenericConstrains genericDefenition,
            VarType returnType, params VarType[] argTypes) : base(name, genericDefenition, returnType, argTypes)
        {
        }

        protected abstract object Calc(object a);

        public override IConcreteFunction CreateConcrete(VarType[] concreteTypesMap) =>
            new ConcreteImplementationWithSingleArg(
                calc: Calc,
                name: Name,
                returnType: VarType.SubstituteConcreteTypes(ReturnType, concreteTypesMap),
                argType: SubstitudeArgTypes(concreteTypesMap)[0]);

        private class ConcreteImplementationWithSingleArg : FunctionWithSingleArg
        {
            private readonly Func<object, object> _calc;

            public ConcreteImplementationWithSingleArg(
                Func<object, object> calc,
                string name, VarType returnType, VarType argType) : base(name, returnType, argType)
            {
                _calc = calc;
            }

            public override object Calc(object a) => _calc(a);
        }
    }

    public abstract class GenericFunctionWithTwoArguments : GenericFunctionBase
    {
        protected GenericFunctionWithTwoArguments(string name, VarType returnType, params VarType[] argTypes) : base(name, returnType, argTypes)
        {
        }

        protected GenericFunctionWithTwoArguments(string name, GenericConstrains[] genericDefenitions, VarType returnType, params VarType[] argTypes) : base(name, genericDefenitions, returnType, argTypes)
        {
        }

        protected GenericFunctionWithTwoArguments(string name, GenericConstrains genericDefenition, VarType returnType, params VarType[] argTypes) : base(name, genericDefenition, returnType, argTypes)
        {
        }

        protected override object Calc(object[] args) => Calc(new[] {args[0], args[1]});

        protected abstract object Calc(object a, object b);
        
        public override IConcreteFunction CreateConcrete(VarType[] concreteTypesMap) =>
            new ConcreteImplementationWithTwoArgs(
                calc: Calc,
                name: Name,
                returnType: VarType.SubstituteConcreteTypes(ReturnType, concreteTypesMap),
                argTypes:   SubstitudeArgTypes(concreteTypesMap));

        private class ConcreteImplementationWithTwoArgs : FunctionWithTwoArgs
        {
            private readonly Func<object, object, object> _calc;

            public ConcreteImplementationWithTwoArgs( 
                Func<object,object,object> calc,
                string name, VarType returnType, params VarType[] argTypes) : base(name, returnType, argTypes)
            {
                _calc = calc;
            }

            public override object Calc(object a, object b) => _calc(a, b);
        }
    }
    
    public abstract class GenericFunctionBase: IGenericFunction
    {
        public GenericConstrains[] GenericDefenitions { get; }
        
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

                GenericDefenitions = new GenericConstrains[maxGenericId.Value + 1];

                for (int i = 0; i <= maxGenericId; i++)
                    GenericDefenitions[i] = GenericConstrains.Any;
                _maxGenericId = maxGenericId.Value;
        }
        protected GenericFunctionBase(string name, GenericConstrains[] genericDefenitions, VarType returnType,
            params VarType[] argTypes)
        {
            Name = name;
            ArgTypes = argTypes;
            ReturnType = returnType;
            GenericDefenitions = genericDefenitions;
            var maxGenericId = argTypes
                .Append(returnType)
                .Max(i => i.SearchMaxGenericTypeId());
            if (!maxGenericId.HasValue)
                throw new InvalidOperationException($"Type {name} has wrong generic defenition");
        }

        protected GenericFunctionBase(string name, GenericConstrains genericDefenition, VarType returnType,
            params VarType[] argTypes)
        {
            Name = name;
            ArgTypes = argTypes;
            ReturnType = returnType;
            GenericDefenitions = new []{genericDefenition};
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
            var result = new VarType[GenericDefenitions.Length];
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
            private Func<object[], object> _calc;

            public ConcreteGenericFunction(Func<object[],object> calc, string name,  VarType returnType, params VarType[] argTypes) 
                : base(TypeHelper.GetFunSignature(name,returnType,argTypes), returnType, argTypes)
            {
                _calc = calc;
            }

            public override object Calc(object[] args) => _calc(args);
        }

      
    }
}