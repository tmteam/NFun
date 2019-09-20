using NFun.TypeInference.Solving;
using NUnit.Framework;

namespace NFun.HmTests.UnitTests
{
    public class NSUnitTests
    {
        private TiSolver _ti;

        [SetUp]
        public void Init()
        {
            _ti = new TiSolver();
        }
        [Test]
        public void SingleNodeCase()
        {
            _ti.SetStrict(0, TiType.Int32);
            var result = _ti.Solve();
            
            Assert.AreEqual(0, result.GenericsCount);
            Assert.AreEqual(TiType.Int32, result.GetNodeType(0));
        }
        [Test]
        public void SingleVarNode_GenericsCountEquals1()
        {
            _ti.SetVar(0, "x");
            var result = _ti.Solve();
            Assert.AreEqual(1,result.GenericsCount);
            Assert.AreEqual(TiType.Generic(0), result.GetNodeType(0));
            Assert.AreEqual(TiType.Generic(0), result.GetVarType("x"));
        }
        [Test]
        public void SingleArrayOfGenerics_GenericsCountEquals1()
        {
            _ti.SetStrict(0, TiType.ArrayOf(TiType.Generic(0)));
            var result = _ti.Solve();
            Assert.AreEqual(1,result.GenericsCount);
            Assert.AreEqual(TiType.ArrayOf(TiType.Generic(0)), result.GetNodeType(0));
        }
        [Test]
        public void SingleArray()
        {
            _ti.SetStrict(0, TiType.ArrayOf(TiType.Int32));
            Assert.AreEqual(TiType.ArrayOf(TiType.Int32), _ti.Solve().GetNodeType(0));
        }
        
        [Test]
        public void SingleGenericArrayTurnsConcrete_AllGenericsAreSolved()
        {
            _ti.SetStrict(0, TiType.ArrayOf(TiType.Generic(0)));
            Assert.IsTrue(_ti.SetStrict(0, TiType.ArrayOf(TiType.Int32)));
            var solving = _ti.Solve();
            Assert.AreEqual(0, solving.GenericsCount);
            Assert.AreEqual(TiType.ArrayOf(TiType.Int32), solving.GetNodeType(0));
        }
        [Test]
        public void GenericTurnsConcrete_AllGenericsAreSolved()
        {
            _ti.SetStrict(0, TiType.Generic(0));
            Assert.IsTrue(_ti.SetStrict(0, TiType.Int32));
            var solving = _ti.Solve();
            Assert.AreEqual(0, solving.GenericsCount);
            Assert.AreEqual(TiType.Int32, solving.GetNodeType(0));
        }
        [Test]
        public void GenericFuncTurnsConcrete_AllGenericsAreSolved()
        {
            _ti.SetStrict(0,  TiType.GenericFun(3));
            var strictType = TiType.Fun(TiType.Int32, TiType.Bool,TiType.Bool, TiType.Any);
            Assert.IsTrue(_ti.SetStrict(0, strictType));
            var solving = _ti.Solve();
            Assert.AreEqual(0, solving.GenericsCount);
            Assert.AreEqual(strictType, solving.GetNodeType(0));
        }
        
        [Test]
        public void UniteTwoGenericNodes_SingleGenericAppears()
        {
            _ti.Unite(1,0);
            var result = _ti.Solve();
            
            Assert.AreEqual(1, result.GenericsCount);
            Assert.AreEqual(TiType.Generic(0), result.GetNodeType(0));
            Assert.AreEqual(TiType.Generic(0), result.GetNodeType(0));
        }
        
        
        [Test]
        public void FunReturnsItsArg_SetOutType_AllGenericsAreSolved()
        {
            _ti.SetCall(new CallDefenition(TiType.Generic(0), new[] {0, 1}));
            Assert.IsTrue(_ti.SetStrict(1, TiType.Int32));
            var result = _ti.Solve();
            Assert.AreEqual(0, result.GenericsCount);
            Assert.AreEqual(TiType.Int32, result.GetNodeType(0));
        }
        [Test]
        public void TwoNodesEqualsToEachOther_TypeIsSame()
        {
            _ti.SetStrict(0, TiType.Bool);
            _ti.Unite(1, 0);  
            _ti.Unite(2, 1);  
            Assert.AreEqual(TiType.Bool, _ti.Solve().GetNodeType(2));
        }
        [Test]
        public void TwoNodesEqualsToEachOther_TypeSetToTopNode_TypeIsSame()
        {
            _ti.Unite(1, 0);  
            _ti.Unite(2, 1);  
            _ti.SetStrict(2, TiType.Bool);
            Assert.AreEqual(TiType.Bool, _ti.Solve().GetNodeType(0));
            Assert.AreEqual(TiType.Bool, _ti.Solve().GetNodeType(1));
            Assert.AreEqual(TiType.Bool, _ti.Solve().GetNodeType(2));
        }
        [Test]
        public void TwoArrayNodesEqualsToEachOther_TypeIsSame()
        {
            _ti.SetStrict(0, TiType.ArrayOf(TiType.Int32));
            _ti.Unite(1, 0);  
            _ti.Unite(2, 1);  
            Assert.AreEqual(TiType.ArrayOf(TiType.Int32), _ti.Solve().GetNodeType(2));
        }
        [Test]
        public void SetGenericEqualToSelf_BothNodesEqualGeneric()
        {
            var generic = new SolvingNode();
            var refToGeneric = new SolvingNode();
            refToGeneric.SetEqualTo(generic);

            Assert.IsTrue(generic.SetEqualTo(refToGeneric));

            Assert.IsTrue(generic.MakeType(10).IsPrimitiveGeneric);
            Assert.IsTrue(refToGeneric.MakeType(10).IsPrimitiveGeneric);
        }
        
        [Test]
        public void SetReferenceGenericEqualToTheGeneric_BothNodesEqualGeneric()
        {
            var generic = new SolvingNode();
            var refToGeneric = new SolvingNode();
            refToGeneric.SetEqualTo(generic);

            Assert.IsTrue(refToGeneric.SetEqualTo(generic));

            Assert.IsTrue(generic.MakeType(10).IsPrimitiveGeneric);
            Assert.IsTrue(refToGeneric.MakeType(10).IsPrimitiveGeneric);
        }
    }
}