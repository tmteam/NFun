using NFun.Types;
using NUnit.Framework;

namespace NFun.UnitTests
{
    [TestFixture]
    public class FunnyTypeTest
    {
        [Test]
        public void Empty_BaseTypeEqualsEmpty()
        {
            var typeA = FunnyType.Empty;
            Assert.AreEqual(BaseFunnyType.Empty, typeA.BaseType);
        }
        #region Equals
        
        [Test]
        public void TwoEqualPrimitiveTypes_Equals_ReturnsTrue()
        {
            var typeA = FunnyType.PrimitiveOf(BaseFunnyType.Int32);
            var typeB = FunnyType.PrimitiveOf(BaseFunnyType.Int32);
            Assert.IsTrue(typeA== typeB);
        }
        
        [Test]
        public void TwoNotEqualPrimitiveTypes_Equals_ReturnsFalse()
        {
            var typeA = FunnyType.PrimitiveOf(BaseFunnyType.Real);
            var typeB = FunnyType.PrimitiveOf(BaseFunnyType.Int32);
            Assert.IsFalse(typeA== typeB);
        }
        
        [Test]
        public void TwoEqualArrayTypes_Equals_ReturnsTrue()
        {
            var typeA = FunnyType.ArrayOf(FunnyType.Int32);
            var typeB = FunnyType.ArrayOf(FunnyType.Int32);
            Assert.IsTrue(typeA== typeB);
        }
        
        [Test]
        public void TwoNotEqualArrayTypes_Equals_ReturnsFalse()
        {
            var typeA = FunnyType.ArrayOf(FunnyType.Int32);
            var typeB = FunnyType.ArrayOf(FunnyType.Real);
            Assert.IsFalse(typeA== typeB);
        }
        
        [Test]
        public void TwoEqualFunTypes_Equals_ReturnsTrue()
        {
            var typeA = FunnyType.Fun(FunnyType.Bool, FunnyType.Int32);
            var typeB = FunnyType.Fun(FunnyType.Bool, FunnyType.Int32);
            Assert.IsTrue(typeA== typeB);
        }
        
        [Test]
        public void TwoFunTypesWithDifferentInputs_Equals_ReturnsFalse()
        {
            var typeA = FunnyType.Fun(FunnyType.Bool, FunnyType.Int32, FunnyType.Int32);
            var typeB = FunnyType.Fun(FunnyType.Bool, FunnyType.Text, FunnyType.Int32);
            Assert.IsFalse(typeA== typeB);
        }
        
        
        [Test]
        public void TwoFunTypesWithDifferentOutputs_Equals_ReturnsFalse()
        {
            var typeA = FunnyType.Fun(FunnyType.Int32, FunnyType.Text);
            var typeB = FunnyType.Fun(FunnyType.Bool, FunnyType.Text);
            Assert.IsFalse(typeA== typeB);
        }
        
        
        [Test]
        public void TwoComplexHiOrderEqualFunTypes_Equals_ReturnsTrue()
        {
            var typeA = FunnyType.Fun(
                returnType: FunnyType.ArrayOf(FunnyType.Fun(FunnyType.Int32, FunnyType.Text)),
                inputTypes: 
                        new []{
                            FunnyType.ArrayOf(FunnyType.Any),
                            FunnyType.Fun(
                                returnType: FunnyType.ArrayOf(FunnyType.Real), 
                                inputTypes: FunnyType.Fun(
                                                returnType: FunnyType.Fun(
                                                                returnType: FunnyType.ArrayOf(FunnyType.Bool),
                                                                inputTypes: FunnyType.Bool),
                                                inputTypes: FunnyType.Text))
                            });
            var typeB = FunnyType.Fun(
                returnType: FunnyType.ArrayOf(FunnyType.Fun(FunnyType.Int32, FunnyType.Text)),
                inputTypes: 
                new []{
                    FunnyType.ArrayOf(FunnyType.Any),
                    FunnyType.Fun(
                        returnType: FunnyType.ArrayOf(FunnyType.Real), 
                        inputTypes: FunnyType.Fun(
                            returnType: FunnyType.Fun(
                                returnType: FunnyType.ArrayOf(FunnyType.Bool),
                                inputTypes: FunnyType.Bool),
                            inputTypes: FunnyType.Text))
                });
            Assert.IsTrue(typeA== typeB);
        }
        [Test]
        public void TwoEqualGenerics_Equals_ReturnsTrue()
        {
            var typeA = FunnyType.Generic(1);
            var typeB = FunnyType.Generic(1);
            Assert.AreEqual(typeA, typeB);
        }
        [Test]
        public void TwoDifferentGenerics_Equals_ReturnsFalse()
        {
            var typeA = FunnyType.Generic(1);
            var typeB = FunnyType.Generic(2);
            Assert.AreNotEqual(typeA, typeB);
        }
        [Test]
        public void ArrayAndPrimitiveTypes_Equals_ReturnsFalse()
        {
            var typeA = FunnyType.ArrayOf(FunnyType.Int32);
            var typeB = FunnyType.Int32;
            Assert.IsFalse(typeA== typeB);
        }

        
        [Test]
        public void TwoEqualArrayOfArrayTypes_Equals_ReturnsTrue()
        {
            var typeA = FunnyType.ArrayOf(FunnyType.ArrayOf(FunnyType.Int32));
            var typeB = FunnyType.ArrayOf(FunnyType.ArrayOf(FunnyType.Int32));
            Assert.IsTrue(typeA== typeB);
        }
        
        [Test]
        public void TwoNotEqualArrayOfArrayTypes_Equals_ReturnsTrue()
        {
            var typeA = FunnyType.ArrayOf(FunnyType.ArrayOf(FunnyType.Int32));
            var typeB = FunnyType.ArrayOf(FunnyType.ArrayOf(FunnyType.Real));
            Assert.IsFalse(typeA== typeB);
        }
        #endregion
        
        #region CanBeConverted
        [TestCase(BaseFunnyType.UInt8, BaseFunnyType.Any, true)]
        [TestCase(BaseFunnyType.UInt8, BaseFunnyType.Real, true)]
        [TestCase(BaseFunnyType.UInt8, BaseFunnyType.Bool, false)]
        [TestCase(BaseFunnyType.UInt8, BaseFunnyType.UInt64, true)]
        [TestCase(BaseFunnyType.UInt8, BaseFunnyType.UInt32, true)]
        [TestCase(BaseFunnyType.UInt8, BaseFunnyType.UInt16, true)]
        [TestCase(BaseFunnyType.UInt8, BaseFunnyType.UInt8, true)]
        [TestCase(BaseFunnyType.UInt8, BaseFunnyType.Int64, true)]
        [TestCase(BaseFunnyType.UInt8, BaseFunnyType.Int32, true)]
        [TestCase(BaseFunnyType.UInt8, BaseFunnyType.Int16, true)]
        
        [TestCase(BaseFunnyType.UInt16, BaseFunnyType.Any, true)]
        [TestCase(BaseFunnyType.UInt16, BaseFunnyType.Real, true)]
        [TestCase(BaseFunnyType.UInt16, BaseFunnyType.Bool, false)]
        [TestCase(BaseFunnyType.UInt16, BaseFunnyType.UInt64, true)]
        [TestCase(BaseFunnyType.UInt16, BaseFunnyType.UInt32, true)]
        [TestCase(BaseFunnyType.UInt16, BaseFunnyType.UInt16, true)]
        [TestCase(BaseFunnyType.UInt16, BaseFunnyType.UInt8, false)]
        [TestCase(BaseFunnyType.UInt16, BaseFunnyType.Int64, true)]
        [TestCase(BaseFunnyType.UInt16, BaseFunnyType.Int32, true)]
        [TestCase(BaseFunnyType.UInt16, BaseFunnyType.Int16, false)]
        
        
        [TestCase(BaseFunnyType.UInt32, BaseFunnyType.Any, true)]
        [TestCase(BaseFunnyType.UInt32, BaseFunnyType.Real, true)]
        [TestCase(BaseFunnyType.UInt32, BaseFunnyType.Bool, false)]
        [TestCase(BaseFunnyType.UInt32, BaseFunnyType.UInt64, true)]
        [TestCase(BaseFunnyType.UInt32, BaseFunnyType.UInt32, true)]
        [TestCase(BaseFunnyType.UInt32, BaseFunnyType.UInt16, false)]
        [TestCase(BaseFunnyType.UInt32, BaseFunnyType.UInt8, false)]
        [TestCase(BaseFunnyType.UInt32, BaseFunnyType.Int64, true)]
        [TestCase(BaseFunnyType.UInt32, BaseFunnyType.Int32, false)]
        [TestCase(BaseFunnyType.UInt32, BaseFunnyType.Int16, false)]
        
        [TestCase(BaseFunnyType.UInt64, BaseFunnyType.Any, true)]
        [TestCase(BaseFunnyType.UInt64, BaseFunnyType.Real, true)]
        [TestCase(BaseFunnyType.UInt64, BaseFunnyType.Bool, false)]
        [TestCase(BaseFunnyType.UInt64, BaseFunnyType.UInt64, true)]
        [TestCase(BaseFunnyType.UInt64, BaseFunnyType.UInt32, false)]
        [TestCase(BaseFunnyType.UInt64, BaseFunnyType.UInt16, false)]
        [TestCase(BaseFunnyType.UInt64, BaseFunnyType.UInt8, false)]
        [TestCase(BaseFunnyType.UInt64, BaseFunnyType.Int64, false)]
        [TestCase(BaseFunnyType.UInt64, BaseFunnyType.Int32, false)]
        [TestCase(BaseFunnyType.UInt64, BaseFunnyType.Int16, false)]
        
        [TestCase(BaseFunnyType.Int16, BaseFunnyType.Any, true)]
        [TestCase(BaseFunnyType.Int16, BaseFunnyType.Real, true)]
        [TestCase(BaseFunnyType.Int16, BaseFunnyType.Bool, false)]
        [TestCase(BaseFunnyType.Int16, BaseFunnyType.UInt64, false)]
        [TestCase(BaseFunnyType.Int16, BaseFunnyType.UInt32, false)]
        [TestCase(BaseFunnyType.Int16, BaseFunnyType.UInt16, false)]
        [TestCase(BaseFunnyType.Int16, BaseFunnyType.UInt8, false)]
        [TestCase(BaseFunnyType.Int16, BaseFunnyType.Int64, true)]
        [TestCase(BaseFunnyType.Int16, BaseFunnyType.Int32, true)]
        [TestCase(BaseFunnyType.Int16, BaseFunnyType.Int16, true)]
        
        [TestCase(BaseFunnyType.Int32, BaseFunnyType.Any, true)]
        [TestCase(BaseFunnyType.Int32, BaseFunnyType.Real, true)]

        [TestCase(BaseFunnyType.Int32, BaseFunnyType.Bool, false)]
        [TestCase(BaseFunnyType.Int32, BaseFunnyType.UInt64, false)]
        [TestCase(BaseFunnyType.Int32, BaseFunnyType.UInt32, false)]
        [TestCase(BaseFunnyType.Int32, BaseFunnyType.UInt16, false)]
        [TestCase(BaseFunnyType.Int32, BaseFunnyType.UInt8, false)]
        [TestCase(BaseFunnyType.Int32, BaseFunnyType.Int64, true)]
        [TestCase(BaseFunnyType.Int32, BaseFunnyType.Int32, true)]
        [TestCase(BaseFunnyType.Int32, BaseFunnyType.Int16, false)]

        [TestCase(BaseFunnyType.Int64, BaseFunnyType.Any, true)]
        [TestCase(BaseFunnyType.Int64, BaseFunnyType.Real, true)]
        [TestCase(BaseFunnyType.Int64, BaseFunnyType.Bool, false)]
        [TestCase(BaseFunnyType.Int64, BaseFunnyType.UInt64, false)]
        [TestCase(BaseFunnyType.Int64, BaseFunnyType.UInt32, false)]
        [TestCase(BaseFunnyType.Int64, BaseFunnyType.UInt16, false)]
        [TestCase(BaseFunnyType.Int64, BaseFunnyType.UInt8,  false)]
        [TestCase(BaseFunnyType.Int64, BaseFunnyType.Int64,  true)]
        [TestCase(BaseFunnyType.Int64, BaseFunnyType.Int32,  false)]
        [TestCase(BaseFunnyType.Int64, BaseFunnyType.Int16,  false)]
        
        [TestCase(BaseFunnyType.Real, BaseFunnyType.Any,    true)]
        [TestCase(BaseFunnyType.Real, BaseFunnyType.Real,   true)]
        [TestCase(BaseFunnyType.Real, BaseFunnyType.Bool,   false)]
        [TestCase(BaseFunnyType.Real, BaseFunnyType.UInt64, false)]
        [TestCase(BaseFunnyType.Real, BaseFunnyType.UInt32, false)]
        [TestCase(BaseFunnyType.Real, BaseFunnyType.UInt16, false)]
        [TestCase(BaseFunnyType.Real, BaseFunnyType.UInt8,  false)]
        [TestCase(BaseFunnyType.Real, BaseFunnyType.Int64,  false)]
        [TestCase(BaseFunnyType.Real, BaseFunnyType.Int32,  false)]
        [TestCase(BaseFunnyType.Real, BaseFunnyType.Int16,  false)]
        
        [TestCase(BaseFunnyType.Bool, BaseFunnyType.Any,    true)]
        [TestCase(BaseFunnyType.Bool, BaseFunnyType.Bool,   true)]
        [TestCase(BaseFunnyType.Bool, BaseFunnyType.Real,   false)]
        [TestCase(BaseFunnyType.Bool, BaseFunnyType.UInt64, false)]
        [TestCase(BaseFunnyType.Bool, BaseFunnyType.UInt32, false)]
        [TestCase(BaseFunnyType.Bool, BaseFunnyType.UInt16, false)]
        [TestCase(BaseFunnyType.Bool, BaseFunnyType.UInt8,  false)]
        [TestCase(BaseFunnyType.Bool, BaseFunnyType.Int64,  false)]
        [TestCase(BaseFunnyType.Bool, BaseFunnyType.Int32,  false)]
        [TestCase(BaseFunnyType.Bool, BaseFunnyType.Int16,  false)]
 
        [TestCase(BaseFunnyType.Any, BaseFunnyType.Any,    true)]
        [TestCase(BaseFunnyType.Any, BaseFunnyType.Bool,   false)]
        [TestCase(BaseFunnyType.Any, BaseFunnyType.Real,   false)]
        [TestCase(BaseFunnyType.Any, BaseFunnyType.UInt64, false)]
        [TestCase(BaseFunnyType.Any, BaseFunnyType.UInt32, false)]
        [TestCase(BaseFunnyType.Any, BaseFunnyType.UInt16, false)]
        [TestCase(BaseFunnyType.Any, BaseFunnyType.UInt8,  false)]
        [TestCase(BaseFunnyType.Any, BaseFunnyType.Int64,  false)]
        [TestCase(BaseFunnyType.Any, BaseFunnyType.Int32,  false)]
        [TestCase(BaseFunnyType.Any, BaseFunnyType.Int16,  false)]
        public void PrimitiveTypes_CanBeConverted(BaseFunnyType from, BaseFunnyType to, bool canBeConverted)
        {
            var typeFrom = FunnyType.PrimitiveOf(from);
            var typeTo = FunnyType.PrimitiveOf(to);
            Assert.AreEqual(canBeConverted, typeFrom.CanBeConvertedTo(typeTo));
        }
        
        
        [TestCase( BaseFunnyType.Any,    true)]
        [TestCase( BaseFunnyType.Bool,   false)]
        [TestCase( BaseFunnyType.Real,   false)]
        [TestCase( BaseFunnyType.UInt64, false)]
        [TestCase( BaseFunnyType.UInt32, false)]
        [TestCase( BaseFunnyType.UInt16, false)]
        [TestCase( BaseFunnyType.UInt8,  false)]
        [TestCase( BaseFunnyType.Int64,  false)]
        [TestCase( BaseFunnyType.Int32,  false)]
        [TestCase( BaseFunnyType.Int16,  false)]
        public void TextType_CanBeConvertedTo(BaseFunnyType to, bool canBeConverted)
        {
            var typeFrom = FunnyType.Text;
            var typeTo = FunnyType.PrimitiveOf(to);
            Assert.AreEqual(canBeConverted, typeFrom.CanBeConvertedTo(typeTo));
        }
        
        [TestCase( BaseFunnyType.Any,    true)]
        [TestCase( BaseFunnyType.Bool,   true)]
        [TestCase( BaseFunnyType.Real,   true)]
        [TestCase( BaseFunnyType.UInt64, true)]
        [TestCase( BaseFunnyType.UInt32, true)]
        [TestCase( BaseFunnyType.UInt16, true)]
        [TestCase( BaseFunnyType.UInt8,  true)]
        [TestCase( BaseFunnyType.Int64,  true)]
        [TestCase( BaseFunnyType.Int32,  true)]
        [TestCase( BaseFunnyType.Int16,  true)]
        public void TextType_CanBeConvertedFrom(BaseFunnyType from, bool canBeConverted)
        {
            var typeTo = FunnyType.Text;
            var typeFrom = FunnyType.PrimitiveOf(from);
            Assert.AreEqual(canBeConverted, typeFrom.CanBeConvertedTo(typeTo));
        }

        
        [TestCase(BaseFunnyType.Int32, BaseFunnyType.Bool, false)]
        [TestCase(BaseFunnyType.Int32, BaseFunnyType.Int32, true)]
        [TestCase(BaseFunnyType.Int32, BaseFunnyType.Int64, true)]
        [TestCase(BaseFunnyType.Int32, BaseFunnyType.Real, true)]
        [TestCase(BaseFunnyType.Int32, BaseFunnyType.Any, true)]

        [TestCase(BaseFunnyType.Real, BaseFunnyType.Bool, false)]
        [TestCase(BaseFunnyType.Real, BaseFunnyType.Int32, false)]
        [TestCase(BaseFunnyType.Real, BaseFunnyType.Int64, false)]
        [TestCase(BaseFunnyType.Real, BaseFunnyType.Real, true)]
        [TestCase(BaseFunnyType.Real, BaseFunnyType.Any, true)]

        [TestCase(BaseFunnyType.Bool, BaseFunnyType.Bool, true)]
        [TestCase(BaseFunnyType.Bool, BaseFunnyType.Int32, false)]
        [TestCase(BaseFunnyType.Bool, BaseFunnyType.Int64, false)]
        [TestCase(BaseFunnyType.Bool, BaseFunnyType.Real, false)]
        [TestCase(BaseFunnyType.Bool, BaseFunnyType.Any, true)]
      
        [TestCase(BaseFunnyType.Any, BaseFunnyType.Bool, false)]
        [TestCase(BaseFunnyType.Any, BaseFunnyType.Int32, false)]
        [TestCase(BaseFunnyType.Any, BaseFunnyType.Int64, false)]
        [TestCase(BaseFunnyType.Any, BaseFunnyType.Real, false)]
        [TestCase(BaseFunnyType.Any, BaseFunnyType.Any, true)]
        public void ArrayTypes_CanBeConverted(BaseFunnyType from, BaseFunnyType to, bool canBeConverted)
        {
            var typeFrom = FunnyType.ArrayOf(FunnyType.PrimitiveOf(from));
            var typeTo = FunnyType.ArrayOf(FunnyType.PrimitiveOf(to));
            Assert.AreEqual(canBeConverted, typeFrom.CanBeConvertedTo(typeTo));
        }
        
        [Test]
        public void FromPrimitiveToArray_CanBeConvertedReturnsFalse()
        {
            var typeFrom = FunnyType.PrimitiveOf(BaseFunnyType.Int32);
            var typeTo = FunnyType.ArrayOf(FunnyType.PrimitiveOf(BaseFunnyType.Int32));
            Assert.IsFalse(typeFrom.CanBeConvertedTo(typeTo));
        }
        
        [TestCase(BaseFunnyType.Bool, false)]
        [TestCase(BaseFunnyType.Int32, false)]
        [TestCase(BaseFunnyType.Int64, false)]
        [TestCase(BaseFunnyType.Real, false)]
        [TestCase(BaseFunnyType.Any, true)]
        public void FromArrayOfInt32ToPrimitive_CanBeConvertedReturnsFalse(BaseFunnyType to, bool canBeConverted)
        {
            var typeFrom = FunnyType.ArrayOf(FunnyType.PrimitiveOf(BaseFunnyType.Int32));
            var typeTo = FunnyType.PrimitiveOf(to);
            Assert.AreEqual(canBeConverted, typeFrom.CanBeConvertedTo(typeTo));
        }
        
        #endregion
        
        [Test]
        public void SolveGenericTypes_SingleTGenericType_SolvedTypeIsT()
        {
            //Solving  T
            var solvingTypes =   new FunnyType[1];
            var concrete = FunnyType.Int32;
            FunnyType.TrySolveGenericTypes(solvingTypes, FunnyType.Generic(0), concrete);
            Assert.AreEqual(concrete, solvingTypes[0]);
        }
        
        [Test]
        public void SolveGenericTypes_ArrayOfT_SolvedTypeIsT()
        {
            //Solving  T[]
            var solvingTypes =   new FunnyType[1];
            var concrete = FunnyType.Int32;
            FunnyType.TrySolveGenericTypes(solvingTypes, 
                FunnyType.ArrayOf(FunnyType.Generic(0)), 
                FunnyType.ArrayOf(concrete));
            Assert.AreEqual(concrete, solvingTypes[0]);
        }
        
        [Test]
        public void SolveGenericTypes_ArrayOfFunOfT_SolvedTypeIsT()
        {
            //Solving  Array of int SomeFun<T>(T)

            var solvingTypes =   new FunnyType[1];
            var concrete = FunnyType.Text;
            
            FunnyType.TrySolveGenericTypes(solvingTypes,
                FunnyType.ArrayOf(FunnyType.Fun(FunnyType.Int32, FunnyType.Generic(0))),
                FunnyType.ArrayOf(FunnyType.Fun(FunnyType.Int32, concrete))); 
               
            Assert.AreEqual(concrete, solvingTypes[0]);
        }
        
        [Test]
        public void SolveGenericTypes_ConcreteRealToConcreteInt_ReturnsFalse()
        {
            //Solving  Array of int SomeFun<T>(T)
            var result = FunnyType.TrySolveGenericTypes(new FunnyType[1],
                genericType: FunnyType.Int32, concreteType: FunnyType.Real);
            Assert.IsFalse(result);
        }
        
        [Test]
        public void SolveGenericTypes_ComplexGenericFunWith2GenericsType_AllTypesAreSolved()
        {
            //Solving  Array of   T0[] SomeFun<T0,T1>(T1, T1[])

            var solvingTypes =   new FunnyType[2];
            var concrete1 = FunnyType.Text;
            var concrete2 = FunnyType.ArrayOf(FunnyType.Int32);
            FunnyType.TrySolveGenericTypes(solvingTypes,
                FunnyType.Fun(FunnyType.ArrayOf(FunnyType.Generic(0)), FunnyType.Generic(1),FunnyType.ArrayOf(FunnyType.Generic(1))),
                FunnyType.Fun(FunnyType.ArrayOf(concrete1), concrete2,FunnyType.ArrayOf(concrete2))
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
            var someSolving = new[] {FunnyType.Int32, FunnyType.ArrayOf(FunnyType.Text)};
            var concreteTypes = new[]
            {
                FunnyType.Int32,
                FunnyType.Real,
                FunnyType.ArrayOf(FunnyType.Int32),
                FunnyType.ArrayOf(FunnyType.ArrayOf(FunnyType.Text)),
                FunnyType.Fun(FunnyType.ArrayOf(FunnyType.Int32), FunnyType.ArrayOf(FunnyType.Text))
            };
            foreach (var concreteType in concreteTypes)
            {
                var result = FunnyType.SubstituteConcreteTypes(concreteType, someSolving);
                Assert.AreEqual(result,concreteType);    
            }
            
        }
        
        [Test]
        public void SubstituteConcreteTypes_PrimitiveGenericType_RetutnsConcrete()
        {
            var someSolving = new[] {FunnyType.Int32, FunnyType.ArrayOf(FunnyType.Text)};
            var genericType = FunnyType.Generic(0);
            var expected = someSolving[0];
            
            var result = FunnyType.SubstituteConcreteTypes(genericType, someSolving);
            Assert.AreEqual(expected, result);    
            
            
        }
        [Test]
        public void SubstituteConcreteTypes_ComplexGenericType_RetutnsConcrete()
        {
            var someSolving = new[] {FunnyType.Int32, FunnyType.ArrayOf(FunnyType.Text)};
            // array of (T0[] fun<T0,T1>(double, T1, T0))
            var genericType = FunnyType.ArrayOf(
                FunnyType.Fun(
                        FunnyType.ArrayOf(FunnyType.Generic(0)), 
                        FunnyType.Real, FunnyType.Generic(1),FunnyType.Generic(0))); ;
            var expected = FunnyType.ArrayOf(
                FunnyType.Fun(
                    FunnyType.ArrayOf(someSolving[0]), 
                    FunnyType.Real, someSolving[1],someSolving[0]));
            
            var result = FunnyType.SubstituteConcreteTypes(genericType, someSolving);
            Assert.AreEqual(expected, result);    
        }
        [Test]
        public void SearchMaxGenericTypeId_ComplexGenericTypeWithThreeArgs_FindAll()
        {
            // array of (T0[] fun<T0,T1,T2>(double, T1, T0,T2))
            var genericType = FunnyType.ArrayOf(
                FunnyType.Fun(
                    FunnyType.ArrayOf(FunnyType.Generic(0)), 
                    FunnyType.Real, FunnyType.Generic(1),FunnyType.Generic(0), FunnyType.Generic(2)));
            var maxCount = genericType.SearchMaxGenericTypeId();
            Assert.AreEqual(2, maxCount);
        }

        [Test]
        public void TwoStructTypesWithNoMembersAreEqual()
        {
            var empty1 = FunnyType.StructOf();
            var empty2 = FunnyType.StructOf();
            Assert.AreEqual(empty1,empty2);
        }
        
        [Test]
        public void TwoStructTypesWithSingleMemberAreEqual()
        {
            var t1 = FunnyType.StructOf("name", FunnyType.Text);
            var t2 = FunnyType.StructOf("name", FunnyType.Text);
            Assert.AreEqual(t1,t2);
        }
        
        
        [Test]
        public void TwoStructTypesWithSingleMemberOfDifferentTypesAreNotEqual()
        {
            var t1 = FunnyType.StructOf("name", FunnyType.Text);
            var t2 = FunnyType.StructOf("name", FunnyType.Int32);
            Assert.AreNotEqual(t1,t2);
        }
        
        [Test]
        public void TwoStructTypesWithSingleMemberOfDifferentNamesAreNotEqual()
        {
            var t1 = FunnyType.StructOf("name", FunnyType.Text);
            var t2 = FunnyType.StructOf("nick", FunnyType.Text);
            Assert.AreNotEqual(t1,t2);
        }
        [Test]
        public void EmptyStructTypeAndSingleMemeberTypesAreNotEqual()
        {
            var t1 = FunnyType.StructOf("name", FunnyType.Text);
            var t2 = FunnyType.StructOf();
            Assert.AreNotEqual(t1,t2);
        }
    }
}