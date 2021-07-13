using NFun.Runtime;
using NFun.TestTools;
using NFun.Types;
using NUnit.Framework;

namespace NFun.UnitTests.Converters
{
    public class ConvertInputTest
    {
        //todo explicit convertion test
        
        [Test]
        public void NestedConvertionDoesNotThrow2()
        {
            var result = FunnyTypeConverters.ConvertInputOrThrow(
                new { age = 42 },
                FunnyType.StructOf(("age", FunnyType.Any)));
            Assert.IsInstanceOf<FunnyStruct>(result);
        }

        [Test]
        public void NestedConvertionDoesNotThrow()
        {
            var result = FunnyTypeConverters.ConvertInputOrThrow(new
            {
                age = 42,
                size = 1.1,
                name = "vasa"
            }, FunnyType.StructOf(
                ("age", FunnyType.Int32),
                ("size", FunnyType.Real),
                ("name", FunnyType.Any)
            ));
            Assert.IsInstanceOf<FunnyStruct>(result);
        }
        
    }
}