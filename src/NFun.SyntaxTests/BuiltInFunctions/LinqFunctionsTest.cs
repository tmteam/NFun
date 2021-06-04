using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests.BuiltInFunctions
{
    class LinqFunctionsTest
    {
        [TestCase("y:int = [0,7,1,2,3] . fold(max)", 7)]
        [TestCase("y:int = [0x0,7,1,2,3] . fold(add)", 13)]
        [TestCase("y = [0.0,7.0,1.0,2.0,3.0] . fold(add)", 13.0)]

        [TestCase("mysum(x:int, y:int):int = x+y \r" +
                  "y = [0,7,1,2,3].fold(mysum)", 13)]
        [TestCase(@"rr(x:real):bool = x>10
                     y = filter([11.0,20.0,1.0,2.0],rr)", new[] { 11.0, 20.0 })]
        [TestCase(@"ii(x:int):bool = x>10
                     y = filter([11,20,1,2],ii)", new[] { 11, 20 })]
        [TestCase(@"ii(x:int):int = x*x
                     y = map([1,2,3],ii)", new[] { 1, 4, 9 })]
        [TestCase(@"ii(x:int):real = x/2
                     y = map([1,2,3],ii)", new[] { 0.5, 1.0, 1.5 })]
        [TestCase(@"isodd(x:int):bool = (x.rema(2)) == 0
                     y = map([1,2,3],isodd)", new[] { false, true, false })]
        [TestCase(@"toS1(t:text, x:int):text = t.concat(x.toText())
                     y = fold([1,2,3], ':', toS1)", ":123")]
        [TestCase(@"toS2(t:text, x:int):text = t.concat(x.toText())
                     y = fold([1], '', toS2)", "1")]
        [TestCase(@"toS3(t:text, x:int):text = t.concat(x.toText())
                     y = fold([1][1:1], '', toS3)", "")]
        [TestCase(@"toR(r:real, x:int):real = r+x
                     y = fold([1,2,3], 0.5, toR)", 6.5)]
        [TestCase(@"iSum(r:int, x:int):int = r+x
                     y = fold([1,2,3], iSum)", 6)]
        [TestCase(@"iSum(r:int, x:int):int = r+x
                     y = fold([100], iSum)", 100)]
        [TestCase("y:int = [1,2,3,4,5,6,7].filter({it.rema(2)==0}).sum()", 12)]
        [TestCase("y:int = [1,2,3,4,5,6,7].filter({it.rema(2)==0}).fold({it1+it2+1})", 14)]
        [TestCase("y:int = [1,2,3,4,5,6,7].filter({it.rema(2)==0}).fold({it1+it2+1})", 14)]
        [TestCase("y:int = [1,2,3,4,5,6,7].filter({it.rema(2)==0}).fold(min)", 2)]
        [TestCase("y:int = [1,2,3,4,5,6,7].filter({it.rema(2)==0}).fold(max)", 6)]
        [TestCase("y:int = [1,2,3,4].fold {it1+it2}", 10)]
        [TestCase("y:int = [1,2,3,4,5,6,7].fold(max)", 7)]
        [TestCase("y:int = [1,2,3,4,5,6,7].fold{if(it1>it2) it1 else it2}", 7)]
        [TestCase("y:int = [1,2,3,4,5,6,7].filter({it.rema(2)==0}).fold{if(it1>it2) it1 else it2}", 6)]
        [TestCase("y:int = [1,2,3,4,5,6,7].filter({it.rema(2)==0}).fold{0}", 0)]
        [TestCase("y:int = [1,2,3,4,5,6,7].filter({it.rema(2)==0}).fold{0}", 0)]
        public void HiOrderFunConstantEquatation(string expr, object expected) 
            => expr.AssertReturns("y",expected);

        [TestCase("y:int[] = take([1,2,3,4,5],3)", new[] { 1, 2, 3 })]
        [TestCase("y = take([1.0,2.0,3.0,4.0,5.0],4)", new[] { 1.0, 2.0, 3.0, 4.0 })]
        [TestCase("y = take([1.0,2.0,3.0],20)", new[] { 1.0, 2.0, 3.0 })]
        [TestCase("y = take([1.0,2.0,3.0],0)", new double[0])]
        [TestCase("y = take(skip([1.0,2.0,3.0],1),1)", new[] { 2.0 })]

        [TestCase("y:int[] = skip([1,2,3,4,5],3)", new[] { 4, 5 })]
        [TestCase("y = skip(['1','2','3','4','5'],3)", new[] { "4", "5" })]
        [TestCase("y = skip([1.0,2.0,3.0,4.0,5.0],4)", new[] { 5.0 })]
        [TestCase("y = skip([1.0,2.0,3.0],20)", new double[0])]
        [TestCase("y = skip([1.0,2.0,3.0],0)", new[] { 1.0, 2.0, 3.0 })]

        [TestCase("y = repeat('abc',3)", new[] { "abc", "abc", "abc" })]
        [TestCase("y = repeat('abc',0)", new string[0])]

        [TestCase("mypage(x:int[]):int[] = take(skip(x,1),1) \r y = mypage([1,2,3]) ", new[] { 2 })]

        [TestCase("y:int[] = [1,2,3]. reverse()", new[] { 3, 2, 1 })]
        [TestCase("y:int[] = [1,2,3]. reverse() . reverse()", new[] { 1, 2, 3 })]
        [TestCase("y = []. reverse()", new object[0])]

        [TestCase("y:int = [1,2,3].get(1)", 2)]
        [TestCase("y:int = [1,2,3].get(0)", 1)]

        [TestCase("y = [1.0,2.0].concat([3.0,4.0])", new[] { 1.0, 2.0, 3.0, 4.0 })]
        [TestCase("y = [1.0].concat([2.0]).concat([3.0,4.0])", new[] { 1.0, 2.0, 3.0, 4.0 })]
        [TestCase("y = [].concat([])", new object[0])]

        [TestCase("y:int[] = [1,2,3].set(1,42)", new[] { 1, 42, 3 })]

        [TestCase("y = [1.0] .repeat(3).flat()", new[] { 1.0, 1.0, 1.0 })]
        [TestCase("y = [] .repeat(3).flat()", new object[0])]
        [TestCase("y = ['a','b'] .repeat(3).flat()", new[] { "a", "b", "a", "b", "a", "b" })]
        [TestCase("y = ['a','b'] .repeat(0).flat()", new string[0])]
        [TestCase("y = ['a','b'] .repeat(1).flat()", new[] { "a", "b" })]

        [TestCase("y = [1.0,2.0] . unite([3.0,4.0])", new[] { 1.0, 2.0, 3.0, 4.0 })]
        [TestCase("y = [1.0,2.0,3.0]. unite([3.0,4.0])", new[] { 1.0, 2.0, 3.0, 4.0 })]

        [TestCase("y = []. intersect([])", new object[0])]
        [TestCase("y = [1.0,4.0,2.0,3.0] .intersect([3.0,4.0])", new[] { 4.0, 3.0 })]
        [TestCase("y = [1.0,4.0,2.0,3.0,4.0] .intersect([3.0,4.0])", new[] { 4.0, 3.0 })]
        [TestCase("y = []. unite([])", new object[0])]

        [TestCase("y = [1.0,2.0].unique([3.0,4.0])", new[] { 1.0, 2.0, 3.0, 4.0 })]
        [TestCase("y = [1.0,2.0,3.0].unique([3.0,4.0])", new[] { 1.0, 2.0, 4.0 })]
        [TestCase("y = [3.0,4.0].unique([3.0,4.0])", new double[0])]
        [TestCase("y = [].unique([])", new object[0])]

        [TestCase("y = []. except([])", new object[0])]
        [TestCase("y = [1.0,2.0] . except([3.0,4.0])", new[] { 1.0, 2.0 })]
        [TestCase("y = [1.0,2.0,3.0].except([3.0,4.0])", new[] { 1.0, 2.0 })]

        [TestCase("y = find([1,2,3], 2)", 1)]
        [TestCase("y = find([1,2,3], 4)", -1)]
        [TestCase("y = find([1,2,-4], -4)", 2)]
        [TestCase("y = find([[1,2],[3,4],[5,6]], [3,4])", 1)]
        [TestCase("y = find([[1,2],[3,4],[5,6]], [3,5])", -1)]
        [TestCase("y = find(['la','LALA','pipi'], 'pipi')", 2)]
        [TestCase("y = find(['la','LALA','pipi'], 'pIpi')", -1)]

        [TestCase("y:int[] = [[1],[2,3],[4,5,6]].flat()", new[] { 1, 2, 3, 4, 5, 6 })]
        [TestCase("y:int[] = [[1]][1:1].flat()", new int[0])]
        [TestCase("y:int[] = [[1][1:1]].flat()", new int[0])]
        [TestCase("y = flat([['1'],['2','3'],['4','5','6']])", new[] { "1", "2", "3", "4", "5", "6" })]
        [TestCase("y = flat([['1']][1:1])", new string[0])]
        [TestCase("y = flat([['1'][1:1]])", new string[0])]

        [TestCase("y = [0..100].chunk(10)[0] == [0..9]", true)]
        [TestCase("y = [0..100].chunk(10)[1] == [10..19]", true)]
        [TestCase("y = [0..100].chunk(10)[9] == [90..99]", true)]
        [TestCase("y = [0..100].chunk(10)[10] == [100]", true)]
        [TestCase("y = [0..100].chunk(10)[0] == [0..2]", false)]
        [TestCase("y = [0..100].chunk(10).flat() == [0..100]", true)]
        [TestCase("y = [0..100].chunk(7).flat() == [0..100]", true)]
        [TestCase("y = [0..100].chunk(1).flat() == [0..100]", true)]
        [TestCase("y = [0..1].chunk(7).flat() == [0,1]", true)]
        [TestCase("y = [0..1].chunk(7) == [[0,1]]", true)]
        [TestCase("y = [0..6].chunk(2) == [[0,1],[2,3],[4,5],[6]]", true)]
        [TestCase("y = [3..7].chunk(1) == [[3],[4],[5],[6],[7]]", true)]

      

        [TestCase("y = [true,false,true].map(toText).join(', ')", "True, False, True")]
        [TestCase("y = [1,2,3,4].map(toText).join(', ')", "1, 2, 3, 4")]
        [TestCase("y = ['1','2','3','4'].join(', ')", "1, 2, 3, 4")]
        [TestCase("y = ['1','2','3','4'].join(',')", "1,2,3,4")]
        [TestCase("y = ['01','02','03','04'].join(',')", "01,02,03,04")]

        [TestCase("y = ['1'].join(',')", "1")]

        [TestCase("y = '1 2 3 4 5'.split(' ')", new[] { "1", "2", "3", "4", "5" })]
        [TestCase("y = ' '.split(' ')", new string[0])]
        [TestCase("y = '1 2 3 4 5'.split(' ').join(',')", "1,2,3,4,5")]
        [TestCase("y = [1, 2, 3, 4, 5].map(toText).join(',')", "1,2,3,4,5")]
        [TestCase("y = [1, 2].map(toText)", new[] { "1", "2" })]
        [TestCase("y = '12'.map(toText)", new[] { "1", "2" })]
        [TestCase("y = 'c b a'.split(' ').sort().join(' ')", "a b c")]
        [TestCase("y = 123.toText().reverse()", "321")]
        public void ConstantEquationWithGenericPredefinedFunction(string expr, object expected) => 
            expr.AssertReturns("y",expected);

        [TestCase("y = [1..100].chunk(-1)")]
        [TestCase("y = [1..100].chunk(0)")]
        [TestCase(@"iSum(r:int, x:int):int = r+x
                     y = fold([100][1:1], iSum)")]

        public void FailsOnRuntime(string expr) => expr.AssertObviousFailsOnRuntime();
    }
}
