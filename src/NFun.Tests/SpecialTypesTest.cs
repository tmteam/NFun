using System;
using System.Linq;
using NFun;
using NFun.Interpritation.Functions;
using NFun.Runtime;
using NFun.Types;
using NUnit.Framework;

namespace Funny.Tests
{
    [TestFixture]
    public class SpecialTypesTest
    {
        [Test]
        public void SpecialPrimitiveReal_AdditionalValueReads()
        {
            var runtime =FunBuilder
                .With("z = x*2 \r" +
                      "y = x.getA()")
                .WithFunctions(new GetAdditionalFunMock())
                .Build();
            var res = runtime.Calculate(Var.New("x", 
                new PrimitiveWithAdditional(36.6)
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
                      "y = x.getA()")
                .WithFunctions(new GetAdditionalFunMock())
                .Build();
            var res = runtime.Calculate(Var.New("x", 
                new PrimitiveWithAdditional(36)
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
                .With("x:text[] \r z = x.concat(x) \r")
                .Build();
            
            var res = runtime.Calculate(Var.New("x", 
                new ArrayWithAdditional(new[]{"a","b"}) {
                    AdditionalContent = 42
                }));
            res.AssertReturns(
                Var.New("z", new[]{"a","b","a","b"}));
        }
        [Test]
        public void SpecialArrayOfStrings_AdditionalValueReads()
        {
            var runtime =FunBuilder
                .With("x:text[] \r y = x.getA()")
                .WithFunctions(new GetAdditionalFunMock())
                .Build();
            
            var res = runtime.Calculate(Var.New("x", 
                new ArrayWithAdditional(new[]{"a","b"}) {
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
                      "y = x.getA()")
                .WithFunctions(new GetAdditionalFunMock())
                .Build();
            var res = runtime.Calculate(Var.New("x", 
                new PrimitiveWithAdditional(36)
                {
                    AdditionalContent = 42
                }));
            res.AssertReturns(
                Var.New("z", 72.0), 
                Var.New("y",42));

        }
        [Test]
        public void getAdd_SeveralVariablesInArray_AdditionalValuesSaved()
        {
            var runtime =FunBuilder
                .With("y = [x1,x2].map(i->getA(i)).sum()")
                .WithFunctions(new GetAdditionalFunMock())
                .Build();
            var res = runtime.Calculate(Var.New("x1", 
                new PrimitiveWithAdditional(54.0) {
                    AdditionalContent = 42
                }),
                Var.New("x2", 
                    new PrimitiveWithAdditional(38.0) {
                        AdditionalContent = 69
                    })
                );
            res.AssertReturns(Var.New("y", 69+42));
        }
        
        [Test]
        public void setAdd_TypeSavedAndAdditionalValueSetted()
        {
            var runtime =FunBuilder
                .With("y = setA(1,36)")
                .WithFunctions(new SetAdditionalFunMock())
                .Build();
            var res = runtime.Calculate();
            var output =res.Results.Single();
            Assert.IsInstanceOf<PrimitiveWithAdditional>(output.Value);
            var typed = output.Value as PrimitiveWithAdditional;
            Assert.AreEqual(VarType.Int32, output.Type);
            Assert.AreEqual(1, typed.GetValue());
            Assert.AreEqual(36, typed.AdditionalContent);
        }
    }

    public class SetAdditionalFunMock : GenericFunctionBase
    {
        public SetAdditionalFunMock() : base("setA", VarType.Generic(0), VarType.Generic(0),VarType.Int32)
        {
            
        }

        public override object Calc(object[] args)
        {
            return new PrimitiveWithAdditional(args[0].To<object>())
            {
                AdditionalContent = args[1].To<int>()
            };
        }
    }
    public class GetAdditionalFunMock : FunctionBase
    {
        public GetAdditionalFunMock() : base("getA", VarType.Int32, VarType.Anything)
        {
            
        }

        public override object Calc(object[] args)
        {
            if (args[0] is IWithAdditionalContent add)
                return add.AdditionalContent;

            return 0;
        }
    }
    public class ArrayWithAdditional : FunArray, IWithAdditionalContent
    {
        public ArrayWithAdditional(Array val):base(val)
        {
            
        }
        public int AdditionalContent { get; set; }
    }

    public interface IWithAdditionalContent
    {
        int AdditionalContent { get; set; }
    }
    public class PrimitiveWithAdditional: IFunConvertable, IWithAdditionalContent
    {
        private readonly object _value;

        public PrimitiveWithAdditional(object value)
        {
            _value = value;
        }

        public object GetValue() => _value;

        public T GetOrThrowValue<T>() => (T) _value;
        public int AdditionalContent { get; set; }
    }
}