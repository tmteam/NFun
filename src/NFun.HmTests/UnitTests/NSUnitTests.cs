using System;
using NFun.HindleyMilner.Tyso;
using NUnit.Framework;

namespace NFun.HmTests
{
    public class NSUnitTests
    {
        private HmNodeSolver _hmNode;

        [SetUp]
        public void Init()
        {
            _hmNode = new HmNodeSolver();
        }
        [Test]
        public void SingleNodeCase()
        {
            _hmNode.SetStrict(0, FType.Int32);
            var result = _hmNode.Solve();
            
            Assert.AreEqual(0, result.GenericsCount);
            Assert.AreEqual(FType.Int32, result.GetNodeType(0));
        }
        [Test]
        public void SingleVarNode_GenericsCountEquals1()
        {
            _hmNode.SetVar(0, "x");
            var result = _hmNode.Solve();
            Assert.AreEqual(1,result.GenericsCount);
            Assert.AreEqual(FType.Generic(0), result.GetNodeType(0));
            Assert.AreEqual(FType.Generic(0), result.GetVarType("x"));
        }
        [Test]
        public void SingleArrayOfGenerics_GenericsCountEquals1()
        {
            _hmNode.SetStrict(0, FType.ArrayOf(FType.Generic(0)));
            var result = _hmNode.Solve();
            Assert.AreEqual(1,result.GenericsCount);
            Assert.AreEqual(FType.ArrayOf(FType.Generic(0)), result.GetNodeType(0));
        }
        [Test]
        public void SingleArray()
        {
            _hmNode.SetStrict(0, FType.ArrayOf(FType.Int32));
            Assert.AreEqual(FType.ArrayOf(FType.Int32), _hmNode.Solve().GetNodeType(0));
        }
        
        [Test]
        public void SingleGenericArrayTurnsConcrete_AllGenericsAreSolved()
        {
            _hmNode.SetStrict(0, FType.ArrayOf(FType.Generic(0)));
            Assert.IsTrue(_hmNode.SetStrict(0, FType.ArrayOf(FType.Int32)));
            var solving = _hmNode.Solve();
            Assert.AreEqual(0, solving.GenericsCount);
            Assert.AreEqual(FType.ArrayOf(FType.Int32), solving.GetNodeType(0));
        }
        [Test]
        public void GenericTurnsConcrete_AllGenericsAreSolved()
        {
            _hmNode.SetStrict(0, FType.Generic(0));
            Assert.IsTrue(_hmNode.SetStrict(0, FType.Int32));
            var solving = _hmNode.Solve();
            Assert.AreEqual(0, solving.GenericsCount);
            Assert.AreEqual(FType.Int32, solving.GetNodeType(0));
        }
        [Test]
        public void GenericFuncTurnsConcrete_AllGenericsAreSolved()
        {
            _hmNode.SetStrict(0,  FType.GenericFun(3));
            var strictType = FType.Fun(FType.Int32, FType.Bool,FType.Bool, FType.Any);
            Assert.IsTrue(_hmNode.SetStrict(0, strictType));
            var solving = _hmNode.Solve();
            Assert.AreEqual(0, solving.GenericsCount);
            Assert.AreEqual(strictType, solving.GetNodeType(0));
        }
        
        [Test]
        public void UniteTwoGenericNodes_SingleGenericAppears()
        {
            _hmNode.Unite(1,0);
            var result = _hmNode.Solve();
            
            Assert.AreEqual(1, result.GenericsCount);
            Assert.AreEqual(FType.Generic(0), result.GetNodeType(0));
            Assert.AreEqual(FType.Generic(0), result.GetNodeType(0));
        }
        
        
        [Test]
        public void FunReturnsItsArg_SetOutType_AllGenericsAreSolved()
        {
            _hmNode.SetCall(new CallDef(FType.Generic(0), new[] {0, 1}));
            Assert.IsTrue(_hmNode.SetStrict(1, FType.Int32));
            var result = _hmNode.Solve();
            Assert.AreEqual(0, result.GenericsCount);
            Assert.AreEqual(FType.Int32, result.GetNodeType(0));
        }
        [Test]
        public void TwoNodesEqualsToEachOther_TypeIsSame()
        {
            _hmNode.SetStrict(0, FType.Bool);
            _hmNode.Unite(1, 0);  
            _hmNode.Unite(2, 1);  
            Assert.AreEqual(FType.Bool, _hmNode.Solve().GetNodeType(2));
        }
        [Test]
        public void TwoNodesEqualsToEachOther_TypeSetToTopNode_TypeIsSame()
        {
            _hmNode.Unite(1, 0);  
            _hmNode.Unite(2, 1);  
            _hmNode.SetStrict(2, FType.Bool);
            Assert.AreEqual(FType.Bool, _hmNode.Solve().GetNodeType(0));
            Assert.AreEqual(FType.Bool, _hmNode.Solve().GetNodeType(1));
            Assert.AreEqual(FType.Bool, _hmNode.Solve().GetNodeType(2));
        }
        [Test]
        public void TwoArrayNodesEqualsToEachOther_TypeIsSame()
        {
            _hmNode.SetStrict(0, FType.ArrayOf(FType.Int32));
            _hmNode.Unite(1, 0);  
            _hmNode.Unite(2, 1);  
            Assert.AreEqual(FType.ArrayOf(FType.Int32), _hmNode.Solve().GetNodeType(2));
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
        
        [Test]
        public void LcaTo2Generics_MakeTypeThrows()
        {
            var T0 = new SolvingNode();
            var T1 = new SolvingNode();

            var lca = SolvingNode.CreateLca(T0, T1);
            Assert.Throws<InvalidOperationException>(()=>lca.MakeType());
        }
    }
}