using System;
using NFun.Runtime;
using NFun.Types;
using NUnit.Framework;

namespace NFun.ExprementalTests
{
    [TestFixture]
    public class VqtTest
    {
        [Test]
        public void vqtByInt_returnsVqtByInt()
        {
            var expr = "y = vqt(42, good(), now())";
            var runtime = BuildVqtRuntime(expr);
            
            var startTime = DateTime.Now.Ticks;
            var res = runtime.Calculate();
            var finishTime = DateTime.Now.Ticks;
            
            var vqt = res.Get("y");
            Assert.IsInstanceOf<IVQT>(vqt.Value);
            var qt = vqt.Value as IVQT;
            Assert.AreEqual(42, qt.V);
            Assert.AreEqual(192, qt.Q);
            Assert.LessOrEqual(startTime, qt.T);
            Assert.GreaterOrEqual(finishTime, qt.T);
        }
        
        [Test]
        public void vqByText_returnsVqByText()
        {
            var expr = "y = vq('42',bad())";
            var runtime = BuildVqtRuntime(expr);
            var res = runtime.Calculate();
            var vqt = res.Get("y");
            
            Assert.IsInstanceOf<IVQT>(vqt.Value);
            var qt = vqt.Value as IVQT;
            Assert.AreEqual("42", qt.V);
            Assert.AreEqual(8, qt.Q);
            Assert.AreEqual(-1, qt.T);
        }
        
        
        [Test]
        public void vqByVqt_returnsUpdatedVqt()
        {
            var expr = "y = vq(['42','12'].setT(now()) , bad())";
            var runtime = BuildVqtRuntime(expr);
            
            var startTime = DateTime.Now.Ticks;
            var res = runtime.Calculate();
            var finishTime = DateTime.Now.Ticks;
            
            var vqt = res.Get("y");
            Assert.IsInstanceOf<IVQT>(vqt.Value);
            var qt = vqt.Value as IVQT;
            Assert.AreEqual(new []{"42","12"}, qt.V);
            Assert.AreEqual(8, qt.Q);
            Assert.LessOrEqual(startTime, qt.T);
            Assert.GreaterOrEqual(finishTime, qt.T);
        }
        [Test]
        public void setQBadThanSetT_InputIsNotVqt_returnsValueBadNow()
        {
            var expt = "y = x.setQ(good()).setT(now())";
            var runtime = BuildVqtRuntime(expt);
            var startTime = DateTime.Now.Ticks;
            var res = runtime.Calculate(Var.New("x", 12.1));
            var finishTime = DateTime.Now.Ticks;
            var vqt = res.Get("y");
            Assert.IsInstanceOf<IVQT>(vqt.Value);
            var qt = vqt.Value as IVQT;
            Assert.AreEqual(12.1, qt.V);
            Assert.AreEqual(192, qt.Q);
            Assert.LessOrEqual(startTime, qt.T);
            Assert.GreaterOrEqual(finishTime, qt.T);
        }
        [Test]
        public void setQBad_InputIsNotVqt_returnsValueBadUndefined()
        {
            var expt = "y = x.setQ(bad())";
            var runtime = BuildVqtRuntime(expt);
            var res = runtime.Calculate(Var.New("x", 12.1));
            var vqt = res.Get("y");
            Assert.IsInstanceOf<IVQT>(vqt.Value);
            var qt = vqt.Value as IVQT;
            Assert.AreEqual(12.1, qt.V);
            Assert.AreEqual(8, qt.Q);
            Assert.AreEqual(-1, qt.T);
        }
        [Test]
        public void setQGood_InputIsNotVqt_returnsValueGoodUndefined()
        {
            var expt = "y = x.setQ(good())";
            var runtime = BuildVqtRuntime(expt);
            var res = runtime.Calculate(Var.New("x", 12.1));
            var vqt = res.Get("y");
            Assert.IsInstanceOf<IVQT>(vqt.Value);
            var qt = vqt.Value as IVQT;
            Assert.AreEqual(12.1, qt.V);
            Assert.AreEqual(192, qt.Q);
            Assert.AreEqual(-1, qt.T);
        }
        [Test]
        public void IsGood_InputIsGood_returnsTrue()
        {
            var expt = "y = x.isGood()";
            var runtime = BuildVqtRuntime(expt);
            var res = runtime.Calculate(Var.New("x", 
                new PrimitiveVQT(36.6)
                {
                    Q = 192,
                    T =  DateTime.Now.Ticks
                }));
            res.AssertReturns(Var.New("y",true));

        }
        [Test]
        public void IsGood_InputIsFalse_returnsFalse()
        {
            var expt = "y = x.isGood()";
            var runtime = BuildVqtRuntime(expt);
            var res = runtime.Calculate(Var.New("x", 
                new PrimitiveVQT(36.6)
                {
                    Q = 8,
                    T =  DateTime.Now.Ticks
                }));
            res.AssertReturns(Var.New("y",false));
        }
        [Test]
        public void IsGood_InputIsNotVqt_returnsTrue()
        {
            var expt = "y = x.isGood()";
            var runtime = BuildVqtRuntime(expt);
            var res = runtime.Calculate(Var.New("x", 36.6));
            res.AssertReturns(Var.New("y",true));
        }
        
        [TestCase(36.0,"y=x==36",true)]
        [TestCase(36.0,"y=x.isGood()",true)]
        [TestCase(36.0,"y=x!=10",true)]
        [TestCase(10.0,"y=x!=10",false)]
        [TestCase(10.0,"y=x+10",20.0)]
        [TestCase(10.0,"y=x+x+10",30.0)]
        [TestCase(10.0,"y=(x+x)==20",true)]
        [TestCase(10.0,"y=x>5",true)]
        [TestCase(10.0,"y=x<5",false)]
        [TestCase(10.0,"y=x<=5",false)]
        [TestCase(10.0,"y=x>=5",true)]
        [TestCase(10.0,"y= x.toText()","10")]
        public void SingleVqtInputEquation_CheckOutputValues(object val, string expr, object result)
        {
            var runtime = BuildVqtRuntime(expr);
            var res = runtime.Calculate(Var.New("x", 
                new PrimitiveVQT(val)
            {
                Q = 192,
                T =  DateTime.Now.Ticks
            }));
            res.AssertReturns(Var.New("y",result));
        }
        

        private static FunRuntime BuildVqtRuntime(string expt)
        {
            var runtime = FunBuilder
                .With(expt)
                .WithFunctions(new BadStamp())
                .WithFunctions(new GoodStamp())
                .WithFunctions(new NowFunction())
                .WithFunctions(new ToUtcFunction())
                .WithFunctions(new IsGoodFunction())
                .WithFunctions(new maxQFunction())
                .WithFunctions(new minQFunction())
                .WithFunctions(new VQFunction())
                .WithFunctions(new VQTFunction())
                .WithFunctions(new SetQFunction())
                .WithFunctions(new SetTFunction())
                .Build();
            return runtime;
        }

      
    }
}