using System;
using NFun.Types;

namespace NFun.Interpritation.Functions
{
    public abstract class GenericMetafunction : GenericFunctionBase
    {
        public GenericMetafunction(string name, VarType returnType, params VarType[] argTypes) : base(name, returnType, argTypes)
        {

        }
    }
    public abstract class Metafunction : FunctionBase
    {
        public Metafunction(string name, VarType returnType, params VarType[] argTypes) : base(name, returnType, argTypes)
        {

        }
    }
}