using System;
using System.Linq;
using NFun.ParseErrors;
using NFun.Tic.SolvingStates;
using NFun.Types;

namespace NFun.Interpritation.Functions
{
    public struct GenericConstrains
    {
        public readonly Primitive Ancestor;
        public readonly Primitive Descendant;
        public bool IsComparable;

        public static readonly GenericConstrains Comparable =new GenericConstrains(null,null,true);
        public static readonly GenericConstrains Any 
            = new GenericConstrains(null, null, false);
        public static readonly GenericConstrains Arithmetical
            = new GenericConstrains(Primitive.Real, Primitive.U24, false);

        public GenericConstrains(Primitive ancestor = null, Primitive descendant = null, bool isComparable = false)
        {
            Ancestor = ancestor;
            Descendant = descendant;
            IsComparable = isComparable;
        }
    }
    public abstract class GenericFunctionBase: IFunctionSignature
    {
        public GenericConstrains[] GenericDefenitions { get; }
        protected readonly int _maxGenericId;
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
        }

        protected GenericFunctionBase(string name, GenericConstrains genericDefenition, VarType returnType,
            params VarType[] argTypes)
        {
            Name = name;
            ArgTypes = argTypes;
            ReturnType = returnType;
            GenericDefenitions = new []{genericDefenition};
        }


        public VarType ReturnType { get; }
        
        public abstract object Calc(object[] args);

        public virtual FunctionBase CreateConcrete(VarType[] genericTypes)
        {
            return new ConcreteGenericFunction(
                calc: Calc,
                name: Name,
                returnType: VarType.SubstituteConcreteTypes(ReturnType, genericTypes),
                argTypes: ArgTypes.Select(a => VarType.SubstituteConcreteTypes(a, genericTypes))
                    .ToArray());
        }

        public FunctionBase CreateConcreteOrNull(VarType outputType, params VarType[] concreteArgTypes)
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
                argTypes: ArgTypes.Select(a=>VarType.SubstituteConcreteTypes(a,solvingParams))
                    .ToArray());
        }

        public override string ToString()
            => TypeHelper.GetFunSignature(Name, ReturnType, ArgTypes);


        public class ConcreteGenericFunction: FunctionBase
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