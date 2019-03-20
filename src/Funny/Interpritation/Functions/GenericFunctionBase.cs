using System;
using Funny.Types;

namespace Funny.Interpritation.Functions
{
    public abstract class GenericFunctionBase
    {
        public string Name { get; }
        public VarType[] ArgTypes { get; }
        
        
        protected GenericFunctionBase(string name, VarType outputType, params VarType[] argTypes)
        {
            Name = name;
            ArgTypes = argTypes;
            OutputType = outputType;
                        
        }
        
        public VarType OutputType { get; }
        
        public abstract object Calc(object[] args);

        public FunctionBase MakeConcreteOrNull(params VarType[] concreteArgTypes)
        {
            if (concreteArgTypes.Length != ArgTypes.Length)
                return null;
            VarType? genericArgType = null;
            for (int i = 0; i < ArgTypes.Length; i++)
            {
                if (TryMakeGeneric(ArgTypes[i], concreteArgTypes[i], out var genericArg))
                {
                    if (genericArgType.HasValue && genericArgType != genericArg)
                        return null;
                    genericArgType = genericArg;
                }
            }

            if (genericArgType != null)
            {
                return new ConcreteGenericFunction(
                    functionBase: this, 
                    outputType: MakeConcrete(OutputType, genericArgType.Value), 
                    argTypes: concreteArgTypes);
            }

            return null;
        }

        private static VarType MakeConcrete(VarType genericOrNot, VarType arg)
        {
            if (genericOrNot.BaseType == BaseVarType.Generic)
                return arg;
            if (genericOrNot.BaseType == BaseVarType.ArrayOf)
            {
                var elementType = MakeConcrete(genericOrNot.ArrayTypeSpecification.VarType, arg);
                return VarType.ArrayOf(elementType);
            }
            return genericOrNot;
        }
        private static bool TryMakeGeneric(VarType genericTypeDefenition, VarType targetType,
            out VarType genericArgument)
        {
            if (genericTypeDefenition.BaseType == BaseVarType.Generic)
            {
                genericArgument = targetType;
                return true;
            }

            if (genericTypeDefenition.BaseType == BaseVarType.ArrayOf)
            {
                if (targetType.BaseType != BaseVarType.ArrayOf)
                {
                    genericArgument = new VarType();
                    return false;
                }
                return TryMakeGeneric(
                    genericTypeDefenition.ArrayTypeSpecification.VarType, 
                    targetType.ArrayTypeSpecification.VarType, 
                    out genericArgument);
                
            }
            genericArgument = new VarType();
            return false;
        }
        class ConcreteGenericFunction: FunctionBase
        {
            private readonly GenericFunctionBase _functionBase;

            public ConcreteGenericFunction(GenericFunctionBase functionBase,  VarType outputType, params VarType[] argTypes) 
                : base(functionBase+"_"+ string.Join("->", argTypes)+"->"+outputType, outputType, argTypes)
            {
                _functionBase = functionBase;
            }

            public override object Calc(object[] args)
                =>_functionBase.Calc(args);
        }
    }
}