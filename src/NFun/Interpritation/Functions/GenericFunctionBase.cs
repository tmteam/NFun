using System;
using System.Linq;
using NFun.Types;

namespace NFun.Interpritation.Functions
{
    public abstract class GenericFunctionBase
    {
        private int _maxGenericId;
        public string Name { get; }
        public VarType[] ArgTypes { get; }
        
        protected GenericFunctionBase(string name, VarType specifiedType, params VarType[] argTypes)
        {
            
            Name = name;
            ArgTypes = argTypes;
            SpecifiedType = specifiedType;
            var maxGenericId  = argTypes
                .Append(specifiedType)
                .Max(i => i.SearchMaxGenericTypeId());
            if(!maxGenericId.HasValue)
                throw new InvalidOperationException($"Type {name} has wrong generic defenition");
            
            _maxGenericId = maxGenericId.Value;
        }
        
        public VarType SpecifiedType { get; }
        
        public abstract object Calc(object[] args);

        public FunctionBase CreateConcreteOrNull(params VarType[] concreteArgTypes)
        {
            if (concreteArgTypes.Length != ArgTypes.Length)
                return null;
            
            var solvingParams = new VarType[_maxGenericId+1];

            for (int i = 0; i < ArgTypes.Length; i++)
            {
                if (!VarType.TrySolveGenericTypes(solvingParams, ArgTypes[i], concreteArgTypes[i]))
                    return null;
            }

            foreach (var solvingParam in solvingParams)
            {
                if(solvingParam.BaseType== BaseVarType.Empty)
                    throw new InvalidOperationException($"Incorrect function defenition: ({string.Join(",", ArgTypes)}) -> {SpecifiedType}). Not all generic types can be solved");
            }     
            return new ConcreteGenericFunction(
                functionBase: this, 
                returnType:  VarType.SubstituteConcreteTypes(SpecifiedType, solvingParams), 
                argTypes: concreteArgTypes);
        }
     
     
        class ConcreteGenericFunction: FunctionBase
        {
            private readonly GenericFunctionBase _functionBase;

            public ConcreteGenericFunction(GenericFunctionBase functionBase,  VarType returnType, params VarType[] argTypes) 
                : base(functionBase+"_"+ string.Join("->", argTypes)+"->"+returnType, returnType, argTypes)
            {
                _functionBase = functionBase;
            }

            public override object Calc(object[] args)
                =>_functionBase.Calc(args);
        }
    }
}