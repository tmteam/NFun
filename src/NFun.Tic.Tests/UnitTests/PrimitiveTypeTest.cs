using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.UnitTests
{
    public class PrimitiveTypeTest
    {
        [TestCase(PrimitiveTypeName.Any)]
        [TestCase(PrimitiveTypeName.Bool)]
        [TestCase(PrimitiveTypeName.Char)]
        [TestCase(PrimitiveTypeName.I16)]
        [TestCase(PrimitiveTypeName.I24)]
        [TestCase(PrimitiveTypeName.I32)]
        [TestCase(PrimitiveTypeName.I48)]
        [TestCase(PrimitiveTypeName.I64)]
        [TestCase(PrimitiveTypeName.I96)]
        [TestCase(PrimitiveTypeName.U8)]
        [TestCase(PrimitiveTypeName.U12)]
        [TestCase(PrimitiveTypeName.U16)]
        [TestCase(PrimitiveTypeName.U24)]
        [TestCase(PrimitiveTypeName.U32)]
        [TestCase(PrimitiveTypeName.U48)]
        [TestCase(PrimitiveTypeName.U64)]
        public void GetLastCommonAncestorToSelfReturnsSelf(PrimitiveTypeName type)
        {
            var result =  new StatePrimitive(type).GetLastCommonPrimitiveAncestor(new StatePrimitive(type)).Name;
            Assert.AreEqual(type,result);
        }

        [TestCase(PrimitiveTypeName.Any)]
        [TestCase(PrimitiveTypeName.Bool)]
        [TestCase(PrimitiveTypeName.Char)]
        [TestCase(PrimitiveTypeName.I16)]
        [TestCase(PrimitiveTypeName.I24)]
        [TestCase(PrimitiveTypeName.I32)]
        [TestCase(PrimitiveTypeName.I48)]
        [TestCase(PrimitiveTypeName.I64)]
        [TestCase(PrimitiveTypeName.I96)]
        [TestCase(PrimitiveTypeName.U8)]
        [TestCase(PrimitiveTypeName.U12)]
        [TestCase(PrimitiveTypeName.U16)]
        [TestCase(PrimitiveTypeName.U24)]
        [TestCase(PrimitiveTypeName.U32)]
        [TestCase(PrimitiveTypeName.U48)]
        [TestCase(PrimitiveTypeName.U64)]
        public void GetLastCommonAncestorToAnyReturnsAny(PrimitiveTypeName type)
        {
            var result =  new StatePrimitive(type).GetLastCommonPrimitiveAncestor(StatePrimitive.Any);
            Assert.AreEqual(StatePrimitive.Any,result);
        }
        
        [TestCase(PrimitiveTypeName.Char, PrimitiveTypeName.Bool, PrimitiveTypeName.Any)]
        [TestCase(PrimitiveTypeName.Char, PrimitiveTypeName.Real, PrimitiveTypeName.Any)]
        [TestCase(PrimitiveTypeName.Char, PrimitiveTypeName.I96, PrimitiveTypeName.Any)]
        [TestCase(PrimitiveTypeName.Char, PrimitiveTypeName.I64, PrimitiveTypeName.Any)]
        [TestCase(PrimitiveTypeName.Char, PrimitiveTypeName.I48, PrimitiveTypeName.Any)]
        [TestCase(PrimitiveTypeName.Char, PrimitiveTypeName.I32, PrimitiveTypeName.Any)]
        [TestCase(PrimitiveTypeName.Char, PrimitiveTypeName.I24, PrimitiveTypeName.Any)]
        [TestCase(PrimitiveTypeName.Char, PrimitiveTypeName.I16, PrimitiveTypeName.Any)]
        [TestCase(PrimitiveTypeName.Char, PrimitiveTypeName.U64, PrimitiveTypeName.Any)]
        [TestCase(PrimitiveTypeName.Char, PrimitiveTypeName.U48, PrimitiveTypeName.Any)]
        [TestCase(PrimitiveTypeName.Char, PrimitiveTypeName.U32, PrimitiveTypeName.Any)]
        [TestCase(PrimitiveTypeName.Char, PrimitiveTypeName.U24, PrimitiveTypeName.Any)]
        [TestCase(PrimitiveTypeName.Char, PrimitiveTypeName.U16, PrimitiveTypeName.Any)]
        [TestCase(PrimitiveTypeName.Char, PrimitiveTypeName.U12, PrimitiveTypeName.Any)]
        [TestCase(PrimitiveTypeName.Char, PrimitiveTypeName.U8, PrimitiveTypeName.Any)]
        
        [TestCase(PrimitiveTypeName.Bool, PrimitiveTypeName.Real, PrimitiveTypeName.Any)]
        [TestCase(PrimitiveTypeName.Bool, PrimitiveTypeName.I96, PrimitiveTypeName.Any)]
        [TestCase(PrimitiveTypeName.Bool, PrimitiveTypeName.I64, PrimitiveTypeName.Any)]
        [TestCase(PrimitiveTypeName.Bool, PrimitiveTypeName.I48, PrimitiveTypeName.Any)]
        [TestCase(PrimitiveTypeName.Bool, PrimitiveTypeName.I32, PrimitiveTypeName.Any)]
        [TestCase(PrimitiveTypeName.Bool, PrimitiveTypeName.I24, PrimitiveTypeName.Any)]
        [TestCase(PrimitiveTypeName.Bool, PrimitiveTypeName.I16, PrimitiveTypeName.Any)]
        [TestCase(PrimitiveTypeName.Bool, PrimitiveTypeName.U64, PrimitiveTypeName.Any)]
        [TestCase(PrimitiveTypeName.Bool, PrimitiveTypeName.U48, PrimitiveTypeName.Any)]
        [TestCase(PrimitiveTypeName.Bool, PrimitiveTypeName.U32, PrimitiveTypeName.Any)]
        [TestCase(PrimitiveTypeName.Bool, PrimitiveTypeName.U24, PrimitiveTypeName.Any)]
        [TestCase(PrimitiveTypeName.Bool, PrimitiveTypeName.U16, PrimitiveTypeName.Any)]
        [TestCase(PrimitiveTypeName.Bool, PrimitiveTypeName.U12, PrimitiveTypeName.Any)]
        [TestCase(PrimitiveTypeName.Bool, PrimitiveTypeName.U8, PrimitiveTypeName.Any)]
        
        [TestCase(PrimitiveTypeName.Real, PrimitiveTypeName.I96, PrimitiveTypeName.Real)]
        [TestCase(PrimitiveTypeName.Real, PrimitiveTypeName.I64, PrimitiveTypeName.Real)]
        [TestCase(PrimitiveTypeName.Real, PrimitiveTypeName.I48, PrimitiveTypeName.Real)]
        [TestCase(PrimitiveTypeName.Real, PrimitiveTypeName.I32, PrimitiveTypeName.Real)]
        [TestCase(PrimitiveTypeName.Real, PrimitiveTypeName.I24, PrimitiveTypeName.Real)]
        [TestCase(PrimitiveTypeName.Real, PrimitiveTypeName.I16, PrimitiveTypeName.Real)]
        [TestCase(PrimitiveTypeName.Real, PrimitiveTypeName.U64, PrimitiveTypeName.Real)]
        [TestCase(PrimitiveTypeName.Real, PrimitiveTypeName.U48, PrimitiveTypeName.Real)]
        [TestCase(PrimitiveTypeName.Real, PrimitiveTypeName.U32, PrimitiveTypeName.Real)]
        [TestCase(PrimitiveTypeName.Real, PrimitiveTypeName.U24, PrimitiveTypeName.Real)]
        [TestCase(PrimitiveTypeName.Real, PrimitiveTypeName.U16, PrimitiveTypeName.Real)]
        [TestCase(PrimitiveTypeName.Real, PrimitiveTypeName.U12, PrimitiveTypeName.Real)]
        [TestCase(PrimitiveTypeName.Real, PrimitiveTypeName.U8,  PrimitiveTypeName.Real)]

        [TestCase(PrimitiveTypeName.I96, PrimitiveTypeName.I64, PrimitiveTypeName.I96)]
        [TestCase(PrimitiveTypeName.I96, PrimitiveTypeName.I48, PrimitiveTypeName.I96)]
        [TestCase(PrimitiveTypeName.I96, PrimitiveTypeName.I32, PrimitiveTypeName.I96)]
        [TestCase(PrimitiveTypeName.I96, PrimitiveTypeName.I24, PrimitiveTypeName.I96)]
        [TestCase(PrimitiveTypeName.I96, PrimitiveTypeName.I16, PrimitiveTypeName.I96)]
        [TestCase(PrimitiveTypeName.I96, PrimitiveTypeName.U64, PrimitiveTypeName.I96)]
        [TestCase(PrimitiveTypeName.I96, PrimitiveTypeName.U48, PrimitiveTypeName.I96)]
        [TestCase(PrimitiveTypeName.I96, PrimitiveTypeName.U32, PrimitiveTypeName.I96)]
        [TestCase(PrimitiveTypeName.I96, PrimitiveTypeName.U24, PrimitiveTypeName.I96)]
        [TestCase(PrimitiveTypeName.I96, PrimitiveTypeName.U16, PrimitiveTypeName.I96)]
        [TestCase(PrimitiveTypeName.I96, PrimitiveTypeName.U12, PrimitiveTypeName.I96)]
        [TestCase(PrimitiveTypeName.I96, PrimitiveTypeName.U8,  PrimitiveTypeName.I96)]

        [TestCase(PrimitiveTypeName.I64, PrimitiveTypeName.I48, PrimitiveTypeName.I64)]
        [TestCase(PrimitiveTypeName.I64, PrimitiveTypeName.I32, PrimitiveTypeName.I64)]
        [TestCase(PrimitiveTypeName.I64, PrimitiveTypeName.I24, PrimitiveTypeName.I64)]
        [TestCase(PrimitiveTypeName.I64, PrimitiveTypeName.I16, PrimitiveTypeName.I64)]
        [TestCase(PrimitiveTypeName.I64, PrimitiveTypeName.U64, PrimitiveTypeName.I96)]
        [TestCase(PrimitiveTypeName.I64, PrimitiveTypeName.U48, PrimitiveTypeName.I64)]
        [TestCase(PrimitiveTypeName.I64, PrimitiveTypeName.U32, PrimitiveTypeName.I64)]
        [TestCase(PrimitiveTypeName.I64, PrimitiveTypeName.U24, PrimitiveTypeName.I64)]
        [TestCase(PrimitiveTypeName.I64, PrimitiveTypeName.U16, PrimitiveTypeName.I64)]
        [TestCase(PrimitiveTypeName.I64, PrimitiveTypeName.U12, PrimitiveTypeName.I64)]
        [TestCase(PrimitiveTypeName.I64, PrimitiveTypeName.U8,  PrimitiveTypeName.I64)]
   
        [TestCase(PrimitiveTypeName.I48, PrimitiveTypeName.I32, PrimitiveTypeName.I48)]
        [TestCase(PrimitiveTypeName.I48, PrimitiveTypeName.I24, PrimitiveTypeName.I48)]
        [TestCase(PrimitiveTypeName.I48, PrimitiveTypeName.I16, PrimitiveTypeName.I48)]
        [TestCase(PrimitiveTypeName.I48, PrimitiveTypeName.U64, PrimitiveTypeName.I96)]
        [TestCase(PrimitiveTypeName.I48, PrimitiveTypeName.U48, PrimitiveTypeName.I64)]
        [TestCase(PrimitiveTypeName.I48, PrimitiveTypeName.U32, PrimitiveTypeName.I48)]
        [TestCase(PrimitiveTypeName.I48, PrimitiveTypeName.U24, PrimitiveTypeName.I48)]
        [TestCase(PrimitiveTypeName.I48, PrimitiveTypeName.U16, PrimitiveTypeName.I48)]
        [TestCase(PrimitiveTypeName.I48, PrimitiveTypeName.U12, PrimitiveTypeName.I48)]
        [TestCase(PrimitiveTypeName.I48, PrimitiveTypeName.U8,  PrimitiveTypeName.I48)]
        
        [TestCase(PrimitiveTypeName.I32, PrimitiveTypeName.I24, PrimitiveTypeName.I32)]
        [TestCase(PrimitiveTypeName.I32, PrimitiveTypeName.I16, PrimitiveTypeName.I32)]
        [TestCase(PrimitiveTypeName.I32, PrimitiveTypeName.U64, PrimitiveTypeName.I96)]
        [TestCase(PrimitiveTypeName.I32, PrimitiveTypeName.U48, PrimitiveTypeName.I64)]
        [TestCase(PrimitiveTypeName.I32, PrimitiveTypeName.U32, PrimitiveTypeName.I48)]
        [TestCase(PrimitiveTypeName.I32, PrimitiveTypeName.U24, PrimitiveTypeName.I32)]
        [TestCase(PrimitiveTypeName.I32, PrimitiveTypeName.U16, PrimitiveTypeName.I32)]
        [TestCase(PrimitiveTypeName.I32, PrimitiveTypeName.U12, PrimitiveTypeName.I32)]
        [TestCase(PrimitiveTypeName.I32, PrimitiveTypeName.U8,  PrimitiveTypeName.I32)]

        [TestCase(PrimitiveTypeName.I24, PrimitiveTypeName.I16, PrimitiveTypeName.I24)]
        [TestCase(PrimitiveTypeName.I24, PrimitiveTypeName.U64, PrimitiveTypeName.I96)]
        [TestCase(PrimitiveTypeName.I24, PrimitiveTypeName.U48, PrimitiveTypeName.I64)]
        [TestCase(PrimitiveTypeName.I24, PrimitiveTypeName.U32, PrimitiveTypeName.I48)]
        [TestCase(PrimitiveTypeName.I24, PrimitiveTypeName.U24, PrimitiveTypeName.I32)]
        [TestCase(PrimitiveTypeName.I24, PrimitiveTypeName.U16, PrimitiveTypeName.I24)]
        [TestCase(PrimitiveTypeName.I24, PrimitiveTypeName.U12, PrimitiveTypeName.I24)]
        [TestCase(PrimitiveTypeName.I24, PrimitiveTypeName.U8,  PrimitiveTypeName.I24)]

        [TestCase(PrimitiveTypeName.I16, PrimitiveTypeName.U64, PrimitiveTypeName.I96)]
        [TestCase(PrimitiveTypeName.I16, PrimitiveTypeName.U48, PrimitiveTypeName.I64)]
        [TestCase(PrimitiveTypeName.I16, PrimitiveTypeName.U32, PrimitiveTypeName.I48)]
        [TestCase(PrimitiveTypeName.I16, PrimitiveTypeName.U24, PrimitiveTypeName.I32)]
        [TestCase(PrimitiveTypeName.I16, PrimitiveTypeName.U16, PrimitiveTypeName.I24)]
        [TestCase(PrimitiveTypeName.I16, PrimitiveTypeName.U12, PrimitiveTypeName.I16)]
        [TestCase(PrimitiveTypeName.I16, PrimitiveTypeName.U8,  PrimitiveTypeName.I16)]

        [TestCase(PrimitiveTypeName.U64, PrimitiveTypeName.U48, PrimitiveTypeName.U64)]
        [TestCase(PrimitiveTypeName.U64, PrimitiveTypeName.U32, PrimitiveTypeName.U64)]
        [TestCase(PrimitiveTypeName.U64, PrimitiveTypeName.U24, PrimitiveTypeName.U64)]
        [TestCase(PrimitiveTypeName.U64, PrimitiveTypeName.U16, PrimitiveTypeName.U64)]
        [TestCase(PrimitiveTypeName.U64, PrimitiveTypeName.U12, PrimitiveTypeName.U64)]
        [TestCase(PrimitiveTypeName.U64, PrimitiveTypeName.U8,  PrimitiveTypeName.U64)]

        [TestCase(PrimitiveTypeName.U48, PrimitiveTypeName.U32, PrimitiveTypeName.U48)]
        [TestCase(PrimitiveTypeName.U48, PrimitiveTypeName.U24, PrimitiveTypeName.U48)]
        [TestCase(PrimitiveTypeName.U48, PrimitiveTypeName.U16, PrimitiveTypeName.U48)]
        [TestCase(PrimitiveTypeName.U48, PrimitiveTypeName.U12, PrimitiveTypeName.U48)]
        [TestCase(PrimitiveTypeName.U48, PrimitiveTypeName.U8,  PrimitiveTypeName.U48)]

        [TestCase(PrimitiveTypeName.U32, PrimitiveTypeName.U24, PrimitiveTypeName.U32)]
        [TestCase(PrimitiveTypeName.U32, PrimitiveTypeName.U16, PrimitiveTypeName.U32)]
        [TestCase(PrimitiveTypeName.U32, PrimitiveTypeName.U12, PrimitiveTypeName.U32)]
        [TestCase(PrimitiveTypeName.U32, PrimitiveTypeName.U8,  PrimitiveTypeName.U32)]
 
        [TestCase(PrimitiveTypeName.U24, PrimitiveTypeName.U16, PrimitiveTypeName.U24)]
        [TestCase(PrimitiveTypeName.U24, PrimitiveTypeName.U12, PrimitiveTypeName.U24)]
        [TestCase(PrimitiveTypeName.U24, PrimitiveTypeName.U8,  PrimitiveTypeName.U24)]
        
        [TestCase(PrimitiveTypeName.U16, PrimitiveTypeName.U12, PrimitiveTypeName.U16)]
        [TestCase(PrimitiveTypeName.U16, PrimitiveTypeName.U8,  PrimitiveTypeName.U16)]

        [TestCase(PrimitiveTypeName.U12, PrimitiveTypeName.U8,  PrimitiveTypeName.U12)]
        public void GetLastCommonAncestor(PrimitiveTypeName a, PrimitiveTypeName b, PrimitiveTypeName expected)
        {
            var result =  new StatePrimitive(a).GetLastCommonPrimitiveAncestor(new StatePrimitive(b)).Name;
            Assert.AreEqual( expected, result);
            var revresult = new StatePrimitive(b).GetLastCommonPrimitiveAncestor(new StatePrimitive(a)).Name;
            Assert.AreEqual(expected, revresult);
        }

        [TestCase(PrimitiveTypeName.Any)]
        [TestCase(PrimitiveTypeName.Bool)]
        [TestCase(PrimitiveTypeName.Char)]
        [TestCase(PrimitiveTypeName.I16)]
        [TestCase(PrimitiveTypeName.I24)]
        [TestCase(PrimitiveTypeName.I32)]
        [TestCase(PrimitiveTypeName.I48)]
        [TestCase(PrimitiveTypeName.I64)]
        [TestCase(PrimitiveTypeName.I96)]
        [TestCase(PrimitiveTypeName.U8)]
        [TestCase(PrimitiveTypeName.U12)]
        [TestCase(PrimitiveTypeName.U16)]
        [TestCase(PrimitiveTypeName.U24)]
        [TestCase(PrimitiveTypeName.U32)]
        [TestCase(PrimitiveTypeName.U48)]
        [TestCase(PrimitiveTypeName.U64)]
        public void GetFirstCommonDescendantToSelfReturnsSelf(PrimitiveTypeName type)
        {
            var result =  new StatePrimitive(type).GetFirstCommonDescendantOrNull(new StatePrimitive(type)).Name;
            Assert.AreEqual(type,result);
        }

        [TestCase(PrimitiveTypeName.Any)]
        [TestCase(PrimitiveTypeName.Bool)]
        [TestCase(PrimitiveTypeName.Char)]
        [TestCase(PrimitiveTypeName.I16)]
        [TestCase(PrimitiveTypeName.I24)]
        [TestCase(PrimitiveTypeName.I32)]
        [TestCase(PrimitiveTypeName.I48)]
        [TestCase(PrimitiveTypeName.I64)]
        [TestCase(PrimitiveTypeName.I96)]
        [TestCase(PrimitiveTypeName.U8)]
        [TestCase(PrimitiveTypeName.U12)]
        [TestCase(PrimitiveTypeName.U16)]
        [TestCase(PrimitiveTypeName.U24)]
        [TestCase(PrimitiveTypeName.U32)]
        [TestCase(PrimitiveTypeName.U48)]
        [TestCase(PrimitiveTypeName.U64)]
        public void GetFirstCommonDescendantToAnyReturnsSelf(PrimitiveTypeName type)
        {
            var result =  new StatePrimitive(type).GetFirstCommonDescendantOrNull(StatePrimitive.Any).Name;
            Assert.AreEqual(type,result);
            var reversed =  StatePrimitive.Any.GetFirstCommonDescendantOrNull(new StatePrimitive(type)).Name;
            Assert.AreEqual(type,reversed);

        }

        
        [TestCase(PrimitiveTypeName.Real, PrimitiveTypeName.Char)]
        [TestCase(PrimitiveTypeName.Bool, PrimitiveTypeName.Char)]
        [TestCase(PrimitiveTypeName.I96, PrimitiveTypeName.Char)]
        [TestCase(PrimitiveTypeName.I64, PrimitiveTypeName.Char)]
        [TestCase(PrimitiveTypeName.I48, PrimitiveTypeName.Char)]
        [TestCase(PrimitiveTypeName.I32, PrimitiveTypeName.Char)]
        [TestCase(PrimitiveTypeName.I24, PrimitiveTypeName.Char)]
        [TestCase(PrimitiveTypeName.I16, PrimitiveTypeName.Char)]
        [TestCase(PrimitiveTypeName.U64, PrimitiveTypeName.Char)]
        [TestCase(PrimitiveTypeName.U48, PrimitiveTypeName.Char)]
        [TestCase(PrimitiveTypeName.U32, PrimitiveTypeName.Char)]
        [TestCase(PrimitiveTypeName.U24, PrimitiveTypeName.Char)]
        [TestCase(PrimitiveTypeName.U16, PrimitiveTypeName.Char)]
        [TestCase(PrimitiveTypeName.U12, PrimitiveTypeName.Char)]
        [TestCase(PrimitiveTypeName.U8,  PrimitiveTypeName.Char)]

        [TestCase(PrimitiveTypeName.Real,PrimitiveTypeName.Bool)]
        [TestCase(PrimitiveTypeName.Char,PrimitiveTypeName.Bool)]
        [TestCase(PrimitiveTypeName.I96, PrimitiveTypeName.Bool)]
        [TestCase(PrimitiveTypeName.I64, PrimitiveTypeName.Bool)]
        [TestCase(PrimitiveTypeName.I48, PrimitiveTypeName.Bool)]
        [TestCase(PrimitiveTypeName.I32, PrimitiveTypeName.Bool)]
        [TestCase(PrimitiveTypeName.I24, PrimitiveTypeName.Bool)]
        [TestCase(PrimitiveTypeName.I16, PrimitiveTypeName.Bool)]
        [TestCase(PrimitiveTypeName.U64, PrimitiveTypeName.Bool)]
        [TestCase(PrimitiveTypeName.U48, PrimitiveTypeName.Bool)]
        [TestCase(PrimitiveTypeName.U32, PrimitiveTypeName.Bool)]
        [TestCase(PrimitiveTypeName.U24, PrimitiveTypeName.Bool)]
        [TestCase(PrimitiveTypeName.U16, PrimitiveTypeName.Bool)]
        [TestCase(PrimitiveTypeName.U12, PrimitiveTypeName.Bool)]
        [TestCase(PrimitiveTypeName.U8,  PrimitiveTypeName.Bool)]
        public void GetFirstCommonDescendant_returnsNull(PrimitiveTypeName a, PrimitiveTypeName b)
        {
            var result = new StatePrimitive(a).GetFirstCommonDescendantOrNull(new StatePrimitive(b));
            Assert.IsNull(result);
            var revresult = new StatePrimitive(b).GetFirstCommonDescendantOrNull(new StatePrimitive(a));
            Assert.IsNull(revresult);
        }


        [TestCase(PrimitiveTypeName.Real, PrimitiveTypeName.I96, PrimitiveTypeName.I96)]
        [TestCase(PrimitiveTypeName.Real, PrimitiveTypeName.I64, PrimitiveTypeName.I64)]
        [TestCase(PrimitiveTypeName.Real, PrimitiveTypeName.I48, PrimitiveTypeName.I48)]
        [TestCase(PrimitiveTypeName.Real, PrimitiveTypeName.I32, PrimitiveTypeName.I32)]
        [TestCase(PrimitiveTypeName.Real, PrimitiveTypeName.I24, PrimitiveTypeName.I24)]
        [TestCase(PrimitiveTypeName.Real, PrimitiveTypeName.I16, PrimitiveTypeName.I16)]
        [TestCase(PrimitiveTypeName.Real, PrimitiveTypeName.U64, PrimitiveTypeName.U64)]
        [TestCase(PrimitiveTypeName.Real, PrimitiveTypeName.U48, PrimitiveTypeName.U48)]
        [TestCase(PrimitiveTypeName.Real, PrimitiveTypeName.U32, PrimitiveTypeName.U32)]
        [TestCase(PrimitiveTypeName.Real, PrimitiveTypeName.U24, PrimitiveTypeName.U24)]
        [TestCase(PrimitiveTypeName.Real, PrimitiveTypeName.U16, PrimitiveTypeName.U16)]
        [TestCase(PrimitiveTypeName.Real, PrimitiveTypeName.U12, PrimitiveTypeName.U12)]
        [TestCase(PrimitiveTypeName.Real, PrimitiveTypeName.U8,  PrimitiveTypeName.U8 )]

        [TestCase(PrimitiveTypeName.I96, PrimitiveTypeName.I64, PrimitiveTypeName.I64)]
        [TestCase(PrimitiveTypeName.I96, PrimitiveTypeName.I48, PrimitiveTypeName.I48)]
        [TestCase(PrimitiveTypeName.I96, PrimitiveTypeName.I32, PrimitiveTypeName.I32)]
        [TestCase(PrimitiveTypeName.I96, PrimitiveTypeName.I24, PrimitiveTypeName.I24)]
        [TestCase(PrimitiveTypeName.I96, PrimitiveTypeName.I16, PrimitiveTypeName.I16)]
        [TestCase(PrimitiveTypeName.I96, PrimitiveTypeName.U64, PrimitiveTypeName.U64)]
        [TestCase(PrimitiveTypeName.I96, PrimitiveTypeName.U48, PrimitiveTypeName.U48)]
        [TestCase(PrimitiveTypeName.I96, PrimitiveTypeName.U32, PrimitiveTypeName.U32)]
        [TestCase(PrimitiveTypeName.I96, PrimitiveTypeName.U24, PrimitiveTypeName.U24)]
        [TestCase(PrimitiveTypeName.I96, PrimitiveTypeName.U16, PrimitiveTypeName.U16)]
        [TestCase(PrimitiveTypeName.I96, PrimitiveTypeName.U12, PrimitiveTypeName.U12)]
        [TestCase(PrimitiveTypeName.I96, PrimitiveTypeName.U8,  PrimitiveTypeName.U8)]

        [TestCase(PrimitiveTypeName.I64, PrimitiveTypeName.I48, PrimitiveTypeName.I48)]
        [TestCase(PrimitiveTypeName.I64, PrimitiveTypeName.I32, PrimitiveTypeName.I32)]
        [TestCase(PrimitiveTypeName.I64, PrimitiveTypeName.I24, PrimitiveTypeName.I24)]
        [TestCase(PrimitiveTypeName.I64, PrimitiveTypeName.I16, PrimitiveTypeName.I16)]
        [TestCase(PrimitiveTypeName.I64, PrimitiveTypeName.U64, PrimitiveTypeName.U48)]
        [TestCase(PrimitiveTypeName.I64, PrimitiveTypeName.U48, PrimitiveTypeName.U48)]
        [TestCase(PrimitiveTypeName.I64, PrimitiveTypeName.U32, PrimitiveTypeName.U32)]
        [TestCase(PrimitiveTypeName.I64, PrimitiveTypeName.U24, PrimitiveTypeName.U24)]
        [TestCase(PrimitiveTypeName.I64, PrimitiveTypeName.U16, PrimitiveTypeName.U16)]
        [TestCase(PrimitiveTypeName.I64, PrimitiveTypeName.U12, PrimitiveTypeName.U12)]
        [TestCase(PrimitiveTypeName.I64, PrimitiveTypeName.U8 , PrimitiveTypeName.U8 )]
   
        [TestCase(PrimitiveTypeName.I48, PrimitiveTypeName.I32, PrimitiveTypeName.I32)]
        [TestCase(PrimitiveTypeName.I48, PrimitiveTypeName.I24, PrimitiveTypeName.I24)]
        [TestCase(PrimitiveTypeName.I48, PrimitiveTypeName.I16, PrimitiveTypeName.I16)]
        [TestCase(PrimitiveTypeName.I48, PrimitiveTypeName.U64, PrimitiveTypeName.U32)]
        [TestCase(PrimitiveTypeName.I48, PrimitiveTypeName.U48, PrimitiveTypeName.U32)]
        [TestCase(PrimitiveTypeName.I48, PrimitiveTypeName.U32, PrimitiveTypeName.U32)]
        [TestCase(PrimitiveTypeName.I48, PrimitiveTypeName.U24, PrimitiveTypeName.U24)]
        [TestCase(PrimitiveTypeName.I48, PrimitiveTypeName.U16, PrimitiveTypeName.U16)]
        [TestCase(PrimitiveTypeName.I48, PrimitiveTypeName.U12, PrimitiveTypeName.U12)]
        [TestCase(PrimitiveTypeName.I48, PrimitiveTypeName.U8 , PrimitiveTypeName.U8)]
        
        [TestCase(PrimitiveTypeName.I32, PrimitiveTypeName.I24, PrimitiveTypeName.I24)]
        [TestCase(PrimitiveTypeName.I32, PrimitiveTypeName.I16, PrimitiveTypeName.I16)]
        [TestCase(PrimitiveTypeName.I32, PrimitiveTypeName.U64, PrimitiveTypeName.U24)]
        [TestCase(PrimitiveTypeName.I32, PrimitiveTypeName.U48, PrimitiveTypeName.U24)]
        [TestCase(PrimitiveTypeName.I32, PrimitiveTypeName.U32, PrimitiveTypeName.U24)]
        [TestCase(PrimitiveTypeName.I32, PrimitiveTypeName.U24, PrimitiveTypeName.U24)]
        [TestCase(PrimitiveTypeName.I32, PrimitiveTypeName.U16, PrimitiveTypeName.U16)]
        [TestCase(PrimitiveTypeName.I32, PrimitiveTypeName.U12, PrimitiveTypeName.U12)]
        [TestCase(PrimitiveTypeName.I32, PrimitiveTypeName.U8 , PrimitiveTypeName.U8 )]

        [TestCase(PrimitiveTypeName.I24, PrimitiveTypeName.I16, PrimitiveTypeName.I16)]
        [TestCase(PrimitiveTypeName.I24, PrimitiveTypeName.U64, PrimitiveTypeName.U16)]
        [TestCase(PrimitiveTypeName.I24, PrimitiveTypeName.U48, PrimitiveTypeName.U16)]
        [TestCase(PrimitiveTypeName.I24, PrimitiveTypeName.U32, PrimitiveTypeName.U16)]
        [TestCase(PrimitiveTypeName.I24, PrimitiveTypeName.U24, PrimitiveTypeName.U16)]
        [TestCase(PrimitiveTypeName.I24, PrimitiveTypeName.U16, PrimitiveTypeName.U16)]
        [TestCase(PrimitiveTypeName.I24, PrimitiveTypeName.U12, PrimitiveTypeName.U12)]
        [TestCase(PrimitiveTypeName.I24, PrimitiveTypeName.U8 , PrimitiveTypeName.U8 )]

        [TestCase(PrimitiveTypeName.I16, PrimitiveTypeName.U64, PrimitiveTypeName.U12)]
        [TestCase(PrimitiveTypeName.I16, PrimitiveTypeName.U48, PrimitiveTypeName.U12)]
        [TestCase(PrimitiveTypeName.I16, PrimitiveTypeName.U32, PrimitiveTypeName.U12)]
        [TestCase(PrimitiveTypeName.I16, PrimitiveTypeName.U24, PrimitiveTypeName.U12)]
        [TestCase(PrimitiveTypeName.I16, PrimitiveTypeName.U16, PrimitiveTypeName.U12)]
        [TestCase(PrimitiveTypeName.I16, PrimitiveTypeName.U12, PrimitiveTypeName.U12)]
        [TestCase(PrimitiveTypeName.I16, PrimitiveTypeName.U8,  PrimitiveTypeName.U8)]

        [TestCase(PrimitiveTypeName.U64, PrimitiveTypeName.U48, PrimitiveTypeName.U48)]
        [TestCase(PrimitiveTypeName.U64, PrimitiveTypeName.U32, PrimitiveTypeName.U32)]
        [TestCase(PrimitiveTypeName.U64, PrimitiveTypeName.U24, PrimitiveTypeName.U24)]
        [TestCase(PrimitiveTypeName.U64, PrimitiveTypeName.U16, PrimitiveTypeName.U16)]
        [TestCase(PrimitiveTypeName.U64, PrimitiveTypeName.U12, PrimitiveTypeName.U12)]
        [TestCase(PrimitiveTypeName.U64, PrimitiveTypeName.U8 , PrimitiveTypeName.U8 )]

        [TestCase(PrimitiveTypeName.U48, PrimitiveTypeName.U32, PrimitiveTypeName.U32)]
        [TestCase(PrimitiveTypeName.U48, PrimitiveTypeName.U24, PrimitiveTypeName.U24)]
        [TestCase(PrimitiveTypeName.U48, PrimitiveTypeName.U16, PrimitiveTypeName.U16)]
        [TestCase(PrimitiveTypeName.U48, PrimitiveTypeName.U12, PrimitiveTypeName.U12)]
        [TestCase(PrimitiveTypeName.U48, PrimitiveTypeName.U8 , PrimitiveTypeName.U8 )]

        [TestCase(PrimitiveTypeName.U32, PrimitiveTypeName.U24, PrimitiveTypeName.U24)]
        [TestCase(PrimitiveTypeName.U32, PrimitiveTypeName.U16, PrimitiveTypeName.U16)]
        [TestCase(PrimitiveTypeName.U32, PrimitiveTypeName.U12, PrimitiveTypeName.U12)]
        [TestCase(PrimitiveTypeName.U32, PrimitiveTypeName.U8 , PrimitiveTypeName.U8 )]
 
        [TestCase(PrimitiveTypeName.U24, PrimitiveTypeName.U16, PrimitiveTypeName.U16)]
        [TestCase(PrimitiveTypeName.U24, PrimitiveTypeName.U12, PrimitiveTypeName.U12)]
        [TestCase(PrimitiveTypeName.U24, PrimitiveTypeName.U8,  PrimitiveTypeName.U8)]
        
        [TestCase(PrimitiveTypeName.U16, PrimitiveTypeName.U12, PrimitiveTypeName.U12)]
        [TestCase(PrimitiveTypeName.U16, PrimitiveTypeName.U8,  PrimitiveTypeName.U8)]

        [TestCase(PrimitiveTypeName.U12, PrimitiveTypeName.U8,  PrimitiveTypeName.U8)]
        public void GetFirstCommonDescendant(PrimitiveTypeName a, PrimitiveTypeName b, PrimitiveTypeName expected)
        {
            var result = new StatePrimitive(a).GetFirstCommonDescendantOrNull(new StatePrimitive(b))?.Name;
            Assert.AreEqual(expected, result);
            var revresult = new StatePrimitive(b).GetFirstCommonDescendantOrNull(new StatePrimitive(a))?.Name;
            Assert.AreEqual(expected, revresult);
        }

        [TestCase(PrimitiveTypeName.Real, PrimitiveTypeName.Real)]
        [TestCase(PrimitiveTypeName.I32, PrimitiveTypeName.Real)]
        [TestCase(PrimitiveTypeName.U24, PrimitiveTypeName.Real)]
        [TestCase(PrimitiveTypeName.I64, PrimitiveTypeName.Real)]
        [TestCase(PrimitiveTypeName.U64, PrimitiveTypeName.Real)]
        [TestCase(PrimitiveTypeName.U8, PrimitiveTypeName.Real)]
        [TestCase(PrimitiveTypeName.Real, PrimitiveTypeName.Any)]
        [TestCase(PrimitiveTypeName.I32, PrimitiveTypeName.I96)]
        [TestCase(PrimitiveTypeName.U24, PrimitiveTypeName.I96)]
        [TestCase(PrimitiveTypeName.I64, PrimitiveTypeName.I96)]
        [TestCase(PrimitiveTypeName.U64, PrimitiveTypeName.I96)]
        [TestCase(PrimitiveTypeName.U8, PrimitiveTypeName.I96)]
        [TestCase(PrimitiveTypeName.U24, PrimitiveTypeName.U64)]
        [TestCase(PrimitiveTypeName.U64, PrimitiveTypeName.U64)]
        [TestCase(PrimitiveTypeName.U8, PrimitiveTypeName.U64)]
        [TestCase(PrimitiveTypeName.Char, PrimitiveTypeName.Any)]

        public void CanBeImplicitlyConverted_returnsTrue(PrimitiveTypeName from, PrimitiveTypeName to)
        {
            Assert.IsTrue(new StatePrimitive(from).CanBeImplicitlyConvertedTo(new StatePrimitive(to)));
        }
        [TestCase(PrimitiveTypeName.Real, PrimitiveTypeName.U48)]
        [TestCase(PrimitiveTypeName.I32, PrimitiveTypeName.U64)]
        [TestCase(PrimitiveTypeName.U24, PrimitiveTypeName.I16)]
        [TestCase(PrimitiveTypeName.I64, PrimitiveTypeName.I32)]
        [TestCase(PrimitiveTypeName.Real, PrimitiveTypeName.I64)]
        [TestCase(PrimitiveTypeName.I96, PrimitiveTypeName.U48)]
        [TestCase(PrimitiveTypeName.Any, PrimitiveTypeName.Real)]
        [TestCase(PrimitiveTypeName.I32, PrimitiveTypeName.U12)]
        [TestCase(PrimitiveTypeName.U24, PrimitiveTypeName.U12)]
        [TestCase(PrimitiveTypeName.I64, PrimitiveTypeName.I24)]
        [TestCase(PrimitiveTypeName.U64, PrimitiveTypeName.I16)]
        [TestCase(PrimitiveTypeName.Char, PrimitiveTypeName.Bool)]
        public void CanBeImplicitlyConverted_returnsFalse(PrimitiveTypeName from, PrimitiveTypeName to)
        {
            Assert.IsFalse(new StatePrimitive(from).CanBeImplicitlyConvertedTo(new StatePrimitive(to)));
        }
    }
}