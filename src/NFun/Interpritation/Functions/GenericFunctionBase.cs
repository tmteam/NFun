using System;
using System.Linq;
using NFun.ParseErrors;
using NFun.Types;

namespace NFun.Interpritation.Functions
{
    public abstract class GenericFunctionBase
    {
        protected readonly int _maxGenericId;
        public string Name { get; }
        public VarType[] ArgTypes { get; }
        
        protected GenericFunctionBase(string name, VarType returnType, params VarType[] argTypes)
        {
            
            Name = name;
            ArgTypes = argTypes;
            ReturnType = returnType;
            var maxGenericId  = argTypes
                .Append(returnType)
                .Max(i => i.SearchMaxGenericTypeId());
            if(!maxGenericId.HasValue)
                throw new InvalidOperationException($"Type {name} has wrong generic defenition");
            
            _maxGenericId = maxGenericId.Value;
        }
        
        public VarType ReturnType { get; }
        
        public abstract object Calc(object[] args);

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