using Funny.Types;
using NUnit.Framework;

namespace Funny.Tests.UnitTests
{
    [TestFixture]
    public class VarTypeTest
    {
        #region Equals
        [Test]
        public void TwoEqualPrimitiveTypes_Equals_ReturnsTrue()
        {
            var typeA = VarType.PrimitiveOf(BaseVarType.Int);
            var typeB = VarType.PrimitiveOf(BaseVarType.Int);
            Assert.IsTrue(typeA== typeB);
        }
        
        [Test]
        public void TwoNotEqualPrimitiveTypes_Equals_ReturnsFalse()
        {
            var typeA = VarType.PrimitiveOf(BaseVarType.Real);
            var typeB = VarType.PrimitiveOf(BaseVarType.Int);
            Assert.IsFalse(typeA== typeB);
        }
        
        [Test]
        public void TwoEqualArrayTypes_Equals_ReturnsTrue()
        {
            var typeA = VarType.ArrayOf(VarType.Int);
            var typeB = VarType.ArrayOf(VarType.Int);
            Assert.IsTrue(typeA== typeB);
        }
        
        [Test]
        public void TwoNotEqualArrayTypes_Equals_ReturnsFalse()
        {
            var typeA = VarType.ArrayOf(VarType.Int);
            var typeB = VarType.ArrayOf(VarType.Real);
            Assert.IsFalse(typeA== typeB);
        }
        
        [Test]
        public void TwoEqualFunTypes_Equals_ReturnsTrue()
        {
            var typeA = VarType.Fun(VarType.Bool, VarType.Int);
            var typeB = VarType.Fun(VarType.Bool, VarType.Int);
            Assert.IsTrue(typeA== typeB);
        }
        
        [Test]
        public void TwoFunTypesWithDifferentInputs_Equals_ReturnsFalse()
        {
            var typeA = VarType.Fun(VarType.Bool, VarType.Int, VarType.Int);
            var typeB = VarType.Fun(VarType.Bool, VarType.Text, VarType.Int);
            Assert.IsFalse(typeA== typeB);
        }
        
        
        [Test]
        public void TwoFunTypesWithDifferentOutputs_Equals_ReturnsFalse()
        {
            var typeA = VarType.Fun(VarType.Int, VarType.Text);
            var typeB = VarType.Fun(VarType.Bool, VarType.Text);
            Assert.IsFalse(typeA== typeB);
        }
        
        
        [Test]
        public void TwoComplexHiOrderEqualFunTypes_Equals_ReturnsTrue()
        {
            var typeA = VarType.Fun(
                outputType: VarType.ArrayOf(VarType.Fun(VarType.Int, VarType.Text)),
                inputTypes: 
                        new []{
                            VarType.ArrayOf(VarType.Any),
                            VarType.Fun(
                                outputType: VarType.ArrayOf(VarType.Real), 
                                inputTypes: VarType.Fun(
                                                outputType: VarType.Fun(
                                                                outputType: VarType.ArrayOf(VarType.Bool),
                                                                inputTypes: VarType.Bool),
                                                inputTypes: VarType.Text))
                            });
            var typeB = VarType.Fun(
                outputType: VarType.ArrayOf(VarType.Fun(VarType.Int, VarType.Text)),
                inputTypes: 
                new []{
                    VarType.ArrayOf(VarType.Any),
                    VarType.Fun(
                        outputType: VarType.ArrayOf(VarType.Real), 
                        inputTypes: VarType.Fun(
                            outputType: VarType.Fun(
                                outputType: VarType.ArrayOf(VarType.Bool),
                                inputTypes: VarType.Bool),
                            inputTypes: VarType.Text))
                });
            Assert.IsTrue(typeA== typeB);
        }
        [Test]
        public void TwoEqualGenerics_Equals_ReturnsTrue()
        {
            var typeA = VarType.Generic(1);
            var typeB = VarType.Generic(1);
            Assert.AreEqual(typeA, typeB);
        }
        [Test]
        public void TwoDifferentGenerics_Equals_ReturnsFalse()
        {
            var typeA = VarType.Generic(1);
            var typeB = VarType.Generic(2);
            Assert.AreNotEqual(typeA, typeB);
        }
        [Test]
        public void ArrayAndPrimitiveTypes_Equals_ReturnsFalse()
        {
            var typeA = VarType.ArrayOf(VarType.Int);
            var typeB = VarType.Int;
            Assert.IsFalse(typeA== typeB);
        }

        
        [Test]
        public void TwoEqualArrayOfArrayTypes_Equals_ReturnsTrue()
        {
            var typeA = VarType.ArrayOf(VarType.ArrayOf(VarType.Int));
            var typeB = VarType.ArrayOf(VarType.ArrayOf(VarType.Int));
            Assert.IsTrue(typeA== typeB);
        }
        
        [Test]
        public void TwoNotEqualArrayOfArrayTypes_Equals_ReturnsTrue()
        {
            var typeA = VarType.ArrayOf(VarType.ArrayOf(VarType.Int));
            var typeB = VarType.ArrayOf(VarType.ArrayOf(VarType.Real));
            Assert.IsFalse(typeA== typeB);
        }
        #endregion
        
        #region CanBeConverted
        [TestCase(BaseVarType.Int, BaseVarType.Bool, false)]
        [TestCase(BaseVarType.Int, BaseVarType.Int, true)]
        [TestCase(BaseVarType.Int, BaseVarType.Real, true)]
        [TestCase(BaseVarType.Int, BaseVarType.Text, true)]
        [TestCase(BaseVarType.Int, BaseVarType.Any, true)]

        [TestCase(BaseVarType.Real, BaseVarType.Bool, false)]
        [TestCase(BaseVarType.Real, BaseVarType.Int, false)]
        [TestCase(BaseVarType.Real, BaseVarType.Real, true)]
        [TestCase(BaseVarType.Real, BaseVarType.Text, true)]
        [TestCase(BaseVarType.Real, BaseVarType.Any, true)]

        [TestCase(BaseVarType.Bool, BaseVarType.Bool, true)]
        [TestCase(BaseVarType.Bool, BaseVarType.Int, false)]
        [TestCase(BaseVarType.Bool, BaseVarType.Real, false)]
        [TestCase(BaseVarType.Bool, BaseVarType.Text, true)]
        [TestCase(BaseVarType.Bool, BaseVarType.Any, true)]
        
        [TestCase(BaseVarType.Text, BaseVarType.Bool, false)]
        [TestCase(BaseVarType.Text, BaseVarType.Int, false)]
        [TestCase(BaseVarType.Text, BaseVarType.Real, false)]
        [TestCase(BaseVarType.Text, BaseVarType.Text, true)]
        [TestCase(BaseVarType.Text, BaseVarType.Any, true)]
        
        [TestCase(BaseVarType.Any, BaseVarType.Bool, false)]
        [TestCase(BaseVarType.Any, BaseVarType.Int, false)]
        [TestCase(BaseVarType.Any, BaseVarType.Real, false)]
        [TestCase(BaseVarType.Any, BaseVarType.Text, true)]
        [TestCase(BaseVarType.Any, BaseVarType.Any, true)]
        public void PrimitiveTypes_CanBeConverted(BaseVarType from, BaseVarType to, bool canBeConverted)
        {
            var typeFrom = VarType.PrimitiveOf(from);
            var typeTo = VarType.PrimitiveOf(to);
            Assert.AreEqual(canBeConverted, typeFrom.CanBeConverted(typeTo));
        }
        
        
        [TestCase(BaseVarType.Int, BaseVarType.Bool, false)]
        [TestCase(BaseVarType.Int, BaseVarType.Int, true)]
        [TestCase(BaseVarType.Int, BaseVarType.Real, true)]
        [TestCase(BaseVarType.Int, BaseVarType.Text, true)]
        [TestCase(BaseVarType.Int, BaseVarType.Any, true)]

        [TestCase(BaseVarType.Real, BaseVarType.Bool, false)]
        [TestCase(BaseVarType.Real, BaseVarType.Int, false)]
        [TestCase(BaseVarType.Real, BaseVarType.Real, true)]
        [TestCase(BaseVarType.Real, BaseVarType.Text, true)]
        [TestCase(BaseVarType.Real, BaseVarType.Any, true)]

        [TestCase(BaseVarType.Bool, BaseVarType.Bool, true)]
        [TestCase(BaseVarType.Bool, BaseVarType.Int, false)]
        [TestCase(BaseVarType.Bool, BaseVarType.Real, false)]
        [TestCase(BaseVarType.Bool, BaseVarType.Text, true)]
        [TestCase(BaseVarType.Bool, BaseVarType.Any, true)]
        
        [TestCase(BaseVarType.Text, BaseVarType.Bool, false)]
        [TestCase(BaseVarType.Text, BaseVarType.Int, false)]
        [TestCase(BaseVarType.Text, BaseVarType.Real, false)]
        [TestCase(BaseVarType.Text, BaseVarType.Text, true)]
        [TestCase(BaseVarType.Text, BaseVarType.Any, true)]
        
        [TestCase(BaseVarType.Any, BaseVarType.Bool, false)]
        [TestCase(BaseVarType.Any, BaseVarType.Int, false)]
        [TestCase(BaseVarType.Any, BaseVarType.Real, false)]
        [TestCase(BaseVarType.Any, BaseVarType.Text, true)]
        [TestCase(BaseVarType.Any, BaseVarType.Any, true)]
        public void ArrayTypes_CanBeConverted(BaseVarType from, BaseVarType to, bool canBeConverted)
        {
            var typeFrom = VarType.ArrayOf(VarType.PrimitiveOf(from));
            var typeTo = VarType.ArrayOf(VarType.PrimitiveOf(to));
            Assert.AreEqual(canBeConverted, typeFrom.CanBeConverted(typeTo));
        }
        
        [Test]
        public void FromPrimitiveToArray_CanBeConvertedReturnsFalse()
        {
            var typeFrom = VarType.PrimitiveOf(BaseVarType.Int);
            var typeTo = VarType.ArrayOf(VarType.PrimitiveOf(BaseVarType.Int));
            Assert.IsFalse(typeFrom.CanBeConverted(typeTo));
        }
        
        [TestCase(BaseVarType.Bool, false)]
        [TestCase(BaseVarType.Int, false)]
        [TestCase(BaseVarType.Real, false)]
        [TestCase(BaseVarType.Text, true)]
        [TestCase(BaseVarType.Any, true)]
        public void FromArrayToPrimitive_CanBeConvertedReturnsFalse(BaseVarType to, bool canBeConverted)
        {
            var typeFrom = VarType.ArrayOf(VarType.PrimitiveOf(BaseVarType.Int));
            var typeTo = VarType.PrimitiveOf(to);
            Assert.AreEqual(canBeConverted, typeFrom.CanBeConverted(typeTo));
        }
        
        #endregion
    }
}