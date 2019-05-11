using NFun.HindleyMilner.Tyso;
using NUnit.Framework;

namespace TysoTake2.TypeSolvingNodes.Tests
{
    public class NSUnitTests
    {
        private FSolver _f;

        [SetUp]
        public void Init()
        {
            _f = new FSolver();
        }
        [Test]
        public void SingleNodeCase()
        {
            _f.SetStrict(0, FType.Int32);
            var result = _f.Solve();
            
            Assert.AreEqual(0, result.GenericsCount);
            Assert.AreEqual(FType.Int32, result.GetNodeType(0));
        }
        [Test]
        public void SingleVarNode_GenericsCountEquals1()
        {
            _f.SetVar(0, "x");
            var result = _f.Solve();
            Assert.AreEqual(1,result.GenericsCount);
            Assert.AreEqual(FType.Generic(0), result.GetNodeType(0));
            Assert.AreEqual(FType.Generic(0), result.GetVarType("x"));
        }
        [Test]
        public void SingleArrayOfGenerics_GenericsCountEquals1()
        {
            _f.SetStrict(0, FType.ArrayOf(FType.Generic(0)));
            var result = _f.Solve();
            Assert.AreEqual(1,result.GenericsCount);
            Assert.AreEqual(FType.ArrayOf(FType.Generic(0)), result.GetNodeType(0));
        }
        [Test]
        public void SingleArray()
        {
            _f.SetStrict(0, FType.ArrayOf(FType.Int32));
            Assert.AreEqual(FType.ArrayOf(FType.Int32), _f.Solve().GetNodeType(0));
        }
        
        [Test]
        public void SingleGenericArrayTurnsConcrete_AllGenericsAreSolved()
        {
            _f.SetStrict(0, FType.ArrayOf(FType.Generic(0)));
            Assert.IsTrue(_f.SetStrict(0, FType.ArrayOf(FType.Int32)));
            var solving = _f.Solve();
            Assert.AreEqual(0, solving.GenericsCount);
            Assert.AreEqual(FType.ArrayOf(FType.Int32), solving.GetNodeType(0));
        }
        [Test]
        public void GenericTurnsConcrete_AllGenericsAreSolved()
        {
            _f.SetStrict(0, FType.Generic(0));
            Assert.IsTrue(_f.SetStrict(0, FType.Int32));
            var solving = _f.Solve();
            Assert.AreEqual(0, solving.GenericsCount);
            Assert.AreEqual(FType.Int32, solving.GetNodeType(0));
        }
        [Test]
        public void GenericFuncTurnsConcrete_AllGenericsAreSolved()
        {
            _f.SetStrict(0,  FType.GenericFun(3));
            var strictType = FType.Fun(FType.Int32, FType.Bool,FType.Bool, FType.Any);
            Assert.IsTrue(_f.SetStrict(0, strictType));
            var solving = _f.Solve();
            Assert.AreEqual(0, solving.GenericsCount);
            Assert.AreEqual(strictType, solving.GetNodeType(0));
        }
        
        [Test]
        public void UniteTwoGenericNodes_SingleGenericAppears()
        {
            _f.Unite(1,0);
            var result = _f.Solve();
            
            Assert.AreEqual(1, result.GenericsCount);
            Assert.AreEqual(FType.Generic(0), result.GetNodeType(0));
            Assert.AreEqual(FType.Generic(0), result.GetNodeType(0));
        }
        
        
        [Test]
        public void FunReturnsItsArg_SetOutType_AllGenericsAreSolved()
        {
            _f.SetCall(new CallDef(FType.Generic(0), new[] {0, 1}));
            Assert.IsTrue(_f.SetStrict(1, FType.Int32));
            var result = _f.Solve();
            Assert.AreEqual(0, result.GenericsCount);
            Assert.AreEqual(FType.Int32, result.GetNodeType(0));
        }
        [Test]
        public void TwoNodesEqualsToEachOther_TypeIsSame()
        {
            _f.SetStrict(0, FType.Bool);
            _f.Unite(1, 0);  
            _f.Unite(2, 1);  
            Assert.AreEqual(FType.Bool, _f.Solve().GetNodeType(2));
        }
        [Test]
        public void TwoNodesEqualsToEachOther_TypeSetToTopNode_TypeIsSame()
        {
            _f.Unite(1, 0);  
            _f.Unite(2, 1);  
            _f.SetStrict(2, FType.Bool);
            Assert.AreEqual(FType.Bool, _f.Solve().GetNodeType(0));
            Assert.AreEqual(FType.Bool, _f.Solve().GetNodeType(1));
            Assert.AreEqual(FType.Bool, _f.Solve().GetNodeType(2));
        }
        [Test]
        public void TwoArrayNodesEqualsToEachOther_TypeIsSame()
        {
            _f.SetStrict(0, FType.ArrayOf(FType.Int32));
            _f.Unite(1, 0);  
            _f.Unite(2, 1);  
            Assert.AreEqual(FType.ArrayOf(FType.Int32), _f.Solve().GetNodeType(2));
        }
    }
}