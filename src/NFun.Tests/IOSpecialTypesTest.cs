using System;
using NFun;
using NFun.Interpritation.Functions;
using NFun.Runtime;
using NFun.Types;
using NUnit.Framework;

namespace Funny.Tests
{
    [TestFixture]
    public class IOSpecialTypesTest
    {
        [Test]
        public void SpecialPrimitiveReal_AdditionalValueReads()
        {
            var runtime =FunBuilder
                .With("z = x*2 \r" +
                      "y = x.getAdd()")
                .WithFunctions(new GetAddFunMock())
                .Build();
            var res = runtime.Calculate(Var.New("x", 
                new InputType(36.6)
                {
                    AdditionalContent = 42
                }));
            res.AssertReturns(
                Var.New("z", 36.6*2), 
                Var.New("y",42));

        }
        
        [Test]
        public void SpecialPrimitiveInt_AdditionalValueReads()
        {
            var runtime =FunBuilder
                .With("x:int \r z = x*2 \r" +
                      "y = x.getAdd()")
                .WithFunctions(new GetAddFunMock())
                .Build();
            var res = runtime.Calculate(Var.New("x", 
                new InputType(36)
                {
                    AdditionalContent = 42
                }));
            res.AssertReturns(
                Var.New("z", 36*2), 
                Var.New("y",42));

        }
        
        [Test]
        public void SpecialArrayOfStrings_OriginValueReads()
        {
            var runtime =FunBuilder
                .With("x:text[] \r z = x @ x \r")
                .Build();
            
            var res = runtime.Calculate(Var.New("x", 
                new InputArray(new[]{"a","b"}) {
                    AdditionalContent = 42
                }));
            res.AssertReturns(
                Var.New("z", new[]{"a","b","a","b"}));

        }
        [Test]
        public void SpecialArrayOfStrings_AdditionalValueReads()
        {
            var runtime =FunBuilder
                .With("x:text[] \r y = x.getAdd()")
                .WithFunctions(new GetAddFunMock())
                .Build();
            
            var res = runtime.Calculate(Var.New("x", 
                new InputArray(new[]{"a","b"}) {
                    AdditionalContent = 42
                }));
            res.AssertReturns(Var.New("y",42));
        }
        [Test]
        public void SpecialPrimitiveInt_CastToReal_AdditionalValueReads()
        {
            var runtime =FunBuilder
                .With("x:int \r" +
                      "z = x*2.0 \r" +
                      "y = x.getAdd()")
                .WithFunctions(new GetAddFunMock())
                .Build();
            var res = runtime.Calculate(Var.New("x", 
                new InputType(36)
                {
                    AdditionalContent = 42
                }));
            res.AssertReturns(
                Var.New("z", 72.0), 
                Var.New("y",42));

        }
    }
/*
    public class SetAddFunMock : FunctionBase
    {
        public SetAddFunMock() : base("getAdd", VarType.Int, VarType.Anything)
        {
            
        }

        public override object Calc(object[] args)
        {
            if (args[0] is IWithAdditionalContent add)
                return add.AdditionalContent;

            return 0;
        }
    }*/
    public class GetAddFunMock : FunctionBase
    {
        public GetAddFunMock() : base("getAdd", VarType.Int, VarType.Anything)
        {
            
        }

        public override object Calc(object[] args)
        {
            if (args[0] is IWithAdditionalContent add)
                return add.AdditionalContent;

            return 0;
        }
    }
    public class InputArray : FunArray, IWithAdditionalContent
    {
        public InputArray(Array val):base(val)
        {
            
        }
        public int AdditionalContent { get; set; }
    }

    public interface IWithAdditionalContent
    {
        int AdditionalContent { get; set; }
    }
    public class InputType: IFunConvertable, IWithAdditionalContent
    {
        private readonly object _value;

        public InputType(object value)
        {
            _value = value;
        }

        public object GetValue() => _value;

        public T GetOrThrowValue<T>() => (T) _value;
        public int AdditionalContent { get; set; }
    }
}