using System;
using NFun.Types;

namespace NFun.Interpritation.Functions
{
   
    public static class LambdaWrapperFactory
    {
        public static IConcreteFunction Create<Tin, Tout>(string name, Func<Tin, Tout> function) 
            => new ConcreteLambdaWithSingleArgumentsWrapperFunction<Tin, Tout>(name, function);

        public static IConcreteFunction Create<Tin1, Tin2, Tout>(string name, Func<Tin1, Tin2, Tout> function)
            => throw new NotImplementedException();
        
        public static IConcreteFunction Create<Tin1, Tin2,Tin3, Tout>(string name, Func<Tin1, Tin2,Tin3, Tout> function)
            => throw new NotImplementedException();
        
        public static IConcreteFunction Create<Tin1, Tin2,Tin3, Tin4, Tout>(string name, Func<Tin1, Tin2,Tin3,Tin4, Tout> function)
            => throw new NotImplementedException();
        
        public static IConcreteFunction Create<Tin1, Tin2,Tin3, Tin4,Tin5, Tout>(string name, Func<Tin1, Tin2,Tin3,Tin4,Tin5, Tout> function)
            => throw new NotImplementedException();
        
        public static IConcreteFunction Create<Tin1, Tin2,Tin3, Tin4,Tin5,Tin6, Tout>(string name, Func<Tin1, Tin2,Tin3,Tin4,Tin5,Tin6, Tout> function)
            => throw new NotImplementedException();
        
        public static IConcreteFunction Create<Tin1, Tin2,Tin3, Tin4,Tin5,Tin6, Tin7, Tout>(string name, Func<Tin1, Tin2,Tin3,Tin4,Tin5,Tin6,Tin7, Tout> function)
            => throw new NotImplementedException();
    }
    class ConcreteLambdaWithSingleArgumentsWrapperFunction<Tin,Tout> :FunctionWithSingleArg
    {
        private readonly Func<Tin, Tout> _function;
        private readonly IOutputFunnyConverter _argConverter;
        private readonly IinputFunnyConverter _resultConverter;

        public ConcreteLambdaWithSingleArgumentsWrapperFunction(string id,Func<Tin,Tout> function)
        {
            Name = id;
            _function = function;
            _argConverter = FunnyTypeConverters.GetOutputConverter(typeof(Tin));
            _resultConverter = FunnyTypeConverters.GetInputConverter(typeof(Tout));
            ArgTypes = new[] {_argConverter.FunnyType};
            ReturnType = _resultConverter.FunnyType;
        }
        public override object Calc(object a) => _resultConverter.ToFunObject(_function((Tin)_argConverter.ToClrObject(a)));
    }

}