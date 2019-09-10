using NFun.Types;
using NUnit.Framework;

namespace Funny.Tests.UnitTests
{
    [TestFixture]
    public class VarTypeTest
    {
        [Test]
        public void Empty_BaseTypeEqualsEmpty()
        {
            var typeA = VarType.Empty;
            Assert.AreEqual(BaseVarType.Empty, typeA.BaseType);
        }
        #region Equals
        
        [Test]
        public void TwoEqualPrimitiveTypes_Equals_ReturnsTrue()
        {
            var typeA = VarType.PrimitiveOf(BaseVarType.Int32);
            var typeB = VarType.PrimitiveOf(BaseVarType.Int32);
            Assert.IsTrue(typeA== typeB);
        }
        
        [Test]
        public void TwoNotEqualPrimitiveTypes_Equals_ReturnsFalse()
        {
            var typeA = VarType.PrimitiveOf(BaseVarType.Real);
            var typeB = VarType.PrimitiveOf(BaseVarType.Int32);
            Assert.IsFalse(typeA== typeB);
        }
        
        [Test]
        public void TwoEqualArrayTypes_Equals_ReturnsTrue()
        {
            var typeA = VarType.ArrayOf(VarType.Int32);
            var typeB = VarType.ArrayOf(VarType.Int32);
            Assert.IsTrue(typeA== typeB);
        }
        
        [Test]
        public void TwoNotEqualArrayTypes_Equals_ReturnsFalse()
        {
            var typeA = VarType.ArrayOf(VarType.Int32);
            var typeB = VarType.ArrayOf(VarType.Real);
            Assert.IsFalse(typeA== typeB);
        }
        
        [Test]
        public void TwoEqualFunTypes_Equals_ReturnsTrue()
        {
            var typeA = VarType.Fun(VarType.Bool, VarType.Int32);
            var typeB = VarType.Fun(VarType.Bool, VarType.Int32);
            Assert.IsTrue(typeA== typeB);
        }
        
        [Test]
        public void TwoFunTypesWithDifferentInputs_Equals_ReturnsFalse()
        {
            var typeA = VarType.Fun(VarType.Bool, VarType.Int32, VarType.Int32);
            var typeB = VarType.Fun(VarType.Bool, VarType.Text, VarType.Int32);
            Assert.IsFalse(typeA== typeB);
        }
        
        
        [Test]
        public void TwoFunTypesWithDifferentOutputs_Equals_ReturnsFalse()
        {
            var typeA = VarType.Fun(VarType.Int32, VarType.Text);
            var typeB = VarType.Fun(VarType.Bool, VarType.Text);
            Assert.IsFalse(typeA== typeB);
        }
        
        
        [Test]
        public void TwoComplexHiOrderEqualFunTypes_Equals_ReturnsTrue()
        {
            var typeA = VarType.Fun(
                outputType: VarType.ArrayOf(VarType.Fun(VarType.Int32, VarType.Text)),
                inputTypes: 
                        new []{
                            VarType.ArrayOf(VarType.Anything),
                            VarType.Fun(
                                outputType: VarType.ArrayOf(VarType.Real), 
                                inputTypes: VarType.Fun(
                                                outputType: VarType.Fun(
                                                                outputType: VarType.ArrayOf(VarType.Bool),
                                                                inputTypes: VarType.Bool),
                                                inputTypes: VarType.Text))
                            });
            var typeB = VarType.Fun(
                outputType: VarType.ArrayOf(VarType.Fun(VarType.Int32, VarType.Text)),
                inputTypes: 
                new []{
                    VarType.ArrayOf(VarType.Anything),
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
            var typeA = VarType.ArrayOf(VarType.Int32);
            var typeB = VarType.Int32;
            Assert.IsFalse(typeA== typeB);
        }

        
        [Test]
        public void TwoEqualArrayOfArrayTypes_Equals_ReturnsTrue()
        {
            var typeA = VarType.ArrayOf(VarType.ArrayOf(VarType.Int32));
            var typeB = VarType.ArrayOf(VarType.ArrayOf(VarType.Int32));
            Assert.IsTrue(typeA== typeB);
        }
        
        [Test]
        public void TwoNotEqualArrayOfArrayTypes_Equals_ReturnsTrue()
        {
            var typeA = VarType.ArrayOf(VarType.ArrayOf(VarType.Int32));
            var typeB = VarType.ArrayOf(VarType.ArrayOf(VarType.Real));
            Assert.IsFalse(typeA== typeB);
        }
        #endregion
        
        #region CanBeConverted
        [TestCase(BaseVarType.UInt8, BaseVarType.Any, true)]
        [TestCase(BaseVarType.UInt8, BaseVarType.Real, true)]
        [TestCase(BaseVarType.UInt8, BaseVarType.Bool, false)]
        [TestCase(BaseVarType.UInt8, BaseVarType.UInt64, true)]
        [TestCase(BaseVarType.UInt8, BaseVarType.UInt32, true)]
        [TestCase(BaseVarType.UInt8, BaseVarType.UInt16, true)]
        [TestCase(BaseVarType.UInt8, BaseVarType.UInt8, true)]
        [TestCase(BaseVarType.UInt8, BaseVarType.Int64, true)]
        [TestCase(BaseVarType.UInt8, BaseVarType.Int32, true)]
        [TestCase(BaseVarType.UInt8, BaseVarType.Int16, true)]
        
        [TestCase(BaseVarType.UInt16, BaseVarType.Any, true)]
        [TestCase(BaseVarType.UInt16, BaseVarType.Real, true)]
        [TestCase(BaseVarType.UInt16, BaseVarType.Bool, false)]
        [TestCase(BaseVarType.UInt16, BaseVarType.UInt64, true)]
        [TestCase(BaseVarType.UInt16, BaseVarType.UInt32, true)]
        [TestCase(BaseVarType.UInt16, BaseVarType.UInt16, true)]
        [TestCase(BaseVarType.UInt16, BaseVarType.UInt8, false)]
        [TestCase(BaseVarType.UInt16, BaseVarType.Int64, true)]
        [TestCase(BaseVarType.UInt16, BaseVarType.Int32, true)]
        [TestCase(BaseVarType.UInt16, BaseVarType.Int16, false)]
        
        
        [TestCase(BaseVarType.UInt32, BaseVarType.Any, true)]
        [TestCase(BaseVarType.UInt32, BaseVarType.Real, true)]
        [TestCase(BaseVarType.UInt32, BaseVarType.Bool, false)]
        [TestCase(BaseVarType.UInt32, BaseVarType.UInt64, true)]
        [TestCase(BaseVarType.UInt32, BaseVarType.UInt32, true)]
        [TestCase(BaseVarType.UInt32, BaseVarType.UInt16, false)]
        [TestCase(BaseVarType.UInt32, BaseVarType.UInt8, false)]
        [TestCase(BaseVarType.UInt32, BaseVarType.Int64, true)]
        [TestCase(BaseVarType.UInt32, BaseVarType.Int32, false)]
        [TestCase(BaseVarType.UInt32, BaseVarType.Int16, false)]
        
        [TestCase(BaseVarType.UInt64, BaseVarType.Any, true)]
        [TestCase(BaseVarType.UInt64, BaseVarType.Real, true)]
        [TestCase(BaseVarType.UInt64, BaseVarType.Bool, false)]
        [TestCase(BaseVarType.UInt64, BaseVarType.UInt64, true)]
        [TestCase(BaseVarType.UInt64, BaseVarType.UInt32, false)]
        [TestCase(BaseVarType.UInt64, BaseVarType.UInt16, false)]
        [TestCase(BaseVarType.UInt64, BaseVarType.UInt8, false)]
        [TestCase(BaseVarType.UInt64, BaseVarType.Int64, false)]
        [TestCase(BaseVarType.UInt64, BaseVarType.Int32, false)]
        [TestCase(BaseVarType.UInt64, BaseVarType.Int16, false)]
        
        [TestCase(BaseVarType.Int16, BaseVarType.Any, true)]
        [TestCase(BaseVarType.Int16, BaseVarType.Real, true)]
        [TestCase(BaseVarType.Int16, BaseVarType.Bool, false)]
        [TestCase(BaseVarType.Int16, BaseVarType.UInt64, false)]
        [TestCase(BaseVarType.Int16, BaseVarType.UInt32, false)]
        [TestCase(BaseVarType.Int16, BaseVarType.UInt16, false)]
        [TestCase(BaseVarType.Int16, BaseVarType.UInt8, false)]
        [TestCase(BaseVarType.Int16, BaseVarType.Int64, true)]
        [TestCase(BaseVarType.Int16, BaseVarType.Int32, true)]
        [TestCase(BaseVarType.Int16, BaseVarType.Int16, true)]
        
        [TestCase(BaseVarType.Int32, BaseVarType.Any, true)]
        [TestCase(BaseVarType.Int32, BaseVarType.Real, true)]

        [TestCase(BaseVarType.Int32, BaseVarType.Bool, false)]
        [TestCase(BaseVarType.Int32, BaseVarType.UInt64, false)]
        [TestCase(BaseVarType.Int32, BaseVarType.UInt32, false)]
        [TestCase(BaseVarType.Int32, BaseVarType.UInt16, false)]
        [TestCase(BaseVarType.Int32, BaseVarType.UInt8, false)]
        [TestCase(BaseVarType.Int32, BaseVarType.Int64, true)]
        [TestCase(BaseVarType.Int32, BaseVarType.Int32, true)]
        [TestCase(BaseVarType.Int32, BaseVarType.Int16, false)]

        [TestCase(BaseVarType.Int64, BaseVarType.Any, true)]
        [TestCase(BaseVarType.Int64, BaseVarType.Real, true)]
        [TestCase(BaseVarType.Int64, BaseVarType.Bool, false)]
        [TestCase(BaseVarType.Int64, BaseVarType.UInt64, false)]
        [TestCase(BaseVarType.Int64, BaseVarType.UInt32, false)]
        [TestCase(BaseVarType.Int64, BaseVarType.UInt16, false)]
        [TestCase(BaseVarType.Int64, BaseVarType.UInt8,  false)]
        [TestCase(BaseVarType.Int64, BaseVarType.Int64,  true)]
        [TestCase(BaseVarType.Int64, BaseVarType.Int32,  false)]
        [TestCase(BaseVarType.Int64, BaseVarType.Int16,  false)]
        
        [TestCase(BaseVarType.Real, BaseVarType.Any,    true)]
        [TestCase(BaseVarType.Real, BaseVarType.Real,   true)]
        [TestCase(BaseVarType.Real, BaseVarType.Bool,   false)]
        [TestCase(BaseVarType.Real, BaseVarType.UInt64, false)]
        [TestCase(BaseVarType.Real, BaseVarType.UInt32, false)]
        [TestCase(BaseVarType.Real, BaseVarType.UInt16, false)]
        [TestCase(BaseVarType.Real, BaseVarType.UInt8,  false)]
        [TestCase(BaseVarType.Real, BaseVarType.Int64,  false)]
        [TestCase(BaseVarType.Real, BaseVarType.Int32,  false)]
        [TestCase(BaseVarType.Real, BaseVarType.Int16,  false)]
        
        [TestCase(BaseVarType.Bool, BaseVarType.Any,    true)]
        [TestCase(BaseVarType.Bool, BaseVarType.Bool,   true)]
        [TestCase(BaseVarType.Bool, BaseVarType.Real,   false)]
        [TestCase(BaseVarType.Bool, BaseVarType.UInt64, false)]
        [TestCase(BaseVarType.Bool, BaseVarType.UInt32, false)]
        [TestCase(BaseVarType.Bool, BaseVarType.UInt16, false)]
        [TestCase(BaseVarType.Bool, BaseVarType.UInt8,  false)]
        [TestCase(BaseVarType.Bool, BaseVarType.Int64,  false)]
        [TestCase(BaseVarType.Bool, BaseVarType.Int32,  false)]
        [TestCase(BaseVarType.Bool, BaseVarType.Int16,  false)]
 
        [TestCase(BaseVarType.Any, BaseVarType.Any,    true)]
        [TestCase(BaseVarType.Any, BaseVarType.Bool,   false)]
        [TestCase(BaseVarType.Any, BaseVarType.Real,   false)]
        [TestCase(BaseVarType.Any, BaseVarType.UInt64, false)]
        [TestCase(BaseVarType.Any, BaseVarType.UInt32, false)]
        [TestCase(BaseVarType.Any, BaseVarType.UInt16, false)]
        [TestCase(BaseVarType.Any, BaseVarType.UInt8,  false)]
        [TestCase(BaseVarType.Any, BaseVarType.Int64,  false)]
        [TestCase(BaseVarType.Any, BaseVarType.Int32,  false)]
        [TestCase(BaseVarType.Any, BaseVarType.Int16,  false)]
        public void PrimitiveTypes_CanBeConverted(BaseVarType from, BaseVarType to, bool canBeConverted)
        {
            var typeFrom = VarType.PrimitiveOf(from);
            var typeTo = VarType.PrimitiveOf(to);
            Assert.AreEqual(canBeConverted, typeFrom.CanBeConvertedTo(typeTo));
        }
        
        
        [TestCase( BaseVarType.Any,    true)]
        [TestCase( BaseVarType.Bool,   false)]
        [TestCase( BaseVarType.Real,   false)]
        [TestCase( BaseVarType.UInt64, false)]
        [TestCase( BaseVarType.UInt32, false)]
        [TestCase( BaseVarType.UInt16, false)]
        [TestCase( BaseVarType.UInt8,  false)]
        [TestCase( BaseVarType.Int64,  false)]
        [TestCase( BaseVarType.Int32,  false)]
        [TestCase( BaseVarType.Int16,  false)]
        public void TextType_CanBeConvertedTo(BaseVarType to, bool canBeConverted)
        {
            var typeFrom = VarType.Text;
            var typeTo = VarType.PrimitiveOf(to);
            Assert.AreEqual(canBeConverted, typeFrom.CanBeConvertedTo(typeTo));
        }
        
        [TestCase( BaseVarType.Any,    true)]
        [TestCase( BaseVarType.Bool,   true)]
        [TestCase( BaseVarType.Real,   true)]
        [TestCase( BaseVarType.UInt64, true)]
        [TestCase( BaseVarType.UInt32, true)]
        [TestCase( BaseVarType.UInt16, true)]
        [TestCase( BaseVarType.UInt8,  true)]
        [TestCase( BaseVarType.Int64,  true)]
        [TestCase( BaseVarType.Int32,  true)]
        [TestCase( BaseVarType.Int16,  true)]
        public void TextType_CanBeConvertedFrom(BaseVarType from, bool canBeConverted)
        {
            var typeTo = VarType.Text;
            var typeFrom = VarType.PrimitiveOf(from);
            Assert.AreEqual(canBeConverted, typeFrom.CanBeConvertedTo(typeTo));
        }

        
        [TestCase(BaseVarType.Int32, BaseVarType.Bool, false)]
        [TestCase(BaseVarType.Int32, BaseVarType.Int32, true)]
        [TestCase(BaseVarType.Int32, BaseVarType.Int64, true)]
        [TestCase(BaseVarType.Int32, BaseVarType.Real, true)]
        [TestCase(BaseVarType.Int32, BaseVarType.Any, true)]

        [TestCase(BaseVarType.Real, BaseVarType.Bool, false)]
        [TestCase(BaseVarType.Real, BaseVarType.Int32, false)]
        [TestCase(BaseVarType.Real, BaseVarType.Int64, false)]
        [TestCase(BaseVarType.Real, BaseVarType.Real, true)]
        [TestCase(BaseVarType.Real, BaseVarType.Any, true)]

        [TestCase(BaseVarType.Bool, BaseVarType.Bool, true)]
        [TestCase(BaseVarType.Bool, BaseVarType.Int32, false)]
        [TestCase(BaseVarType.Bool, BaseVarType.Int64, false)]
        [TestCase(BaseVarType.Bool, BaseVarType.Real, false)]
        [TestCase(BaseVarType.Bool, BaseVarType.Any, true)]
      
        [TestCase(BaseVarType.Any, BaseVarType.Bool, false)]
        [TestCase(BaseVarType.Any, BaseVarType.Int32, false)]
        [TestCase(BaseVarType.Any, BaseVarType.Int64, false)]
        [TestCase(BaseVarType.Any, BaseVarType.Real, false)]
        [TestCase(BaseVarType.Any, BaseVarType.Any, true)]
        public void ArrayTypes_CanBeConverted(BaseVarType from, BaseVarType to, bool canBeConverted)
        {
            var typeFrom = VarType.ArrayOf(VarType.PrimitiveOf(from));
            var typeTo = VarType.ArrayOf(VarType.PrimitiveOf(to));
            Assert.AreEqual(canBeConverted, typeFrom.CanBeConvertedTo(typeTo));
        }
        
        [Test]
        public void FromPrimitiveToArray_CanBeConvertedReturnsFalse()
        {
            var typeFrom = VarType.PrimitiveOf(BaseVarType.Int32);
            var typeTo = VarType.ArrayOf(VarType.PrimitiveOf(BaseVarType.Int32));
            Assert.IsFalse(typeFrom.CanBeConvertedTo(typeTo));
        }
        
        [TestCase(BaseVarType.Bool, false)]
        [TestCase(BaseVarType.Int32, false)]
        [TestCase(BaseVarType.Int64, false)]
        [TestCase(BaseVarType.Real, false)]
        [TestCase(BaseVarType.Any, true)]
        public void FromArrayOfInt32ToPrimitive_CanBeConvertedReturnsFalse(BaseVarType to, bool canBeConverted)
        {
            var typeFrom = VarType.ArrayOf(VarType.PrimitiveOf(BaseVarType.Int32));
            var typeTo = VarType.PrimitiveOf(to);
            Assert.AreEqual(canBeConverted, typeFrom.CanBeConvertedTo(typeTo));
        }
        
        #endregion
        
        [Test]
        public void SolveGenericTypes_SingleTGenericType_SolvedTypeIsT()
        {
            //Solving  T
            var solvingTypes =   new VarType[1];
            var concrete = VarType.Int32;
            VarType.TrySolveGenericTypes(solvingTypes, VarType.Generic(0), concrete);
            Assert.AreEqual(concrete, solvingTypes[0]);
        }
        
        [Test]
        public void SolveGenericTypes_ArrayOfT_SolvedTypeIsT()
        {
            //Solving  T[]
            var solvingTypes =   new VarType[1];
            var concrete = VarType.Int32;
            VarType.TrySolveGenericTypes(solvingTypes, 
                VarType.ArrayOf(VarType.Generic(0)), 
                VarType.ArrayOf(concrete));
            Assert.AreEqual(concrete, solvingTypes[0]);
        }
        
        [Test]
        public void SolveGenericTypes_ArrayOfFunOfT_SolvedTypeIsT()
        {
            //Solving  Array of int SomeFun<T>(T)

            var solvingTypes =   new VarType[1];
            var concrete = VarType.Text;
            
            VarType.TrySolveGenericTypes(solvingTypes,
                VarType.ArrayOf(VarType.Fun(VarType.Int32, VarType.Generic(0))),
                VarType.ArrayOf(VarType.Fun(VarType.Int32, concrete))); 
               
            Assert.AreEqual(concrete, solvingTypes[0]);
        }
        
        [Test]
        public void SolveGenericTypes_ConcreteRealToConcreteInt_ReturnsFalse()
        {
            //Solving  Array of int SomeFun<T>(T)
            var result = VarType.TrySolveGenericTypes(new VarType[1],
                genericType: VarType.Int32, concreteType: VarType.Real);
            Assert.IsFalse(result);
        }
        
        [Test]
        public void SolveGenericTypes_ComplexGenericFunWith2GenericsType_AllTypesAreSolved()
        {
            //Solving  Array of   T0[] SomeFun<T0,T1>(T1, T1[])

            var solvingTypes =   new VarType[2];
            var concrete1 = VarType.Text;
            var concrete2 = VarType.ArrayOf(VarType.Int32);
            VarType.TrySolveGenericTypes(solvingTypes,
                VarType.Fun(VarType.ArrayOf(VarType.Generic(0)), VarType.Generic(1),VarType.ArrayOf(VarType.Generic(1))),
                VarType.Fun(VarType.ArrayOf(concrete1), concrete2,VarType.ArrayOf(concrete2))
                );

            Assert.Multiple(() =>
            {
                Assert.AreEqual(concrete1, solvingTypes[0]);
                Assert.AreEqual(concrete2, solvingTypes[1]);
            });
        }

        [Test]
        public void SubstituteConcreteTypes_NonGenericTyp_returnsSelf()
        {
            var someSolving = new[] {VarType.Int32, VarType.ArrayOf(VarType.Text)};
            var concreteTypes = new[]
            {
                VarType.Int32,
                VarType.Real,
                VarType.ArrayOf(VarType.Int32),
                VarType.ArrayOf(VarType.ArrayOf(VarType.Text)),
                VarType.Fun(VarType.ArrayOf(VarType.Int32), VarType.ArrayOf(VarType.Text)),
            };
            foreach (var concreteType in concreteTypes)
            {
                var result = VarType.SubstituteConcreteTypes(concreteType, someSolving);
                Assert.AreEqual(result,concreteType);    
            }
            
        }
        
        [Test]
        public void SubstituteConcreteTypes_PrimitiveGenericType_RetutnsConcrete()
        {
            var someSolving = new[] {VarType.Int32, VarType.ArrayOf(VarType.Text)};
            var genericType = VarType.Generic(0);
            var expected = someSolving[0];
            
            var result = VarType.SubstituteConcreteTypes(genericType, someSolving);
            Assert.AreEqual(expected, result);    
            
            
        }
        [Test]
        public void SubstituteConcreteTypes_ComplexGenericType_RetutnsConcrete()
        {
            var someSolving = new[] {VarType.Int32, VarType.ArrayOf(VarType.Text)};
            // array of (T0[] fun<T0,T1>(double, T1, T0))
            var genericType = VarType.ArrayOf(
                VarType.Fun(
                        VarType.ArrayOf(VarType.Generic(0)), 
                        VarType.Real, VarType.Generic(1),VarType.Generic(0))); ;
            var expected = VarType.ArrayOf(
                VarType.Fun(
                    VarType.ArrayOf(someSolving[0]), 
                    VarType.Real, someSolving[1],someSolving[0]));
            
            var result = VarType.SubstituteConcreteTypes(genericType, someSolving);
            Assert.AreEqual(expected, result);    
        }
        [Test]
        public void SearchMaxGenericTypeId_ComplexGenericTypeWithThreeArgs_FindAll()
        {
            // array of (T0[] fun<T0,T1,T2>(double, T1, T0,T2))
            var genericType = VarType.ArrayOf(
                VarType.Fun(
                    VarType.ArrayOf(VarType.Generic(0)), 
                    VarType.Real, VarType.Generic(1),VarType.Generic(0), VarType.Generic(2)));
            var maxCount = genericType.SearchMaxGenericTypeId();
            Assert.AreEqual(2, maxCount);
        }
    }
}