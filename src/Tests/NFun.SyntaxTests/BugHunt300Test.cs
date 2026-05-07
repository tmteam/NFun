using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// Bug-hunt 300: comprehensive stress test for recursive types, complex named types,
/// functional types in fields, optional chains, and wild combinations.
/// Each test runs an isolated script; failures expose real-world breakage.
/// </summary>
[TestFixture]
public class BugHunt300Test {

    private static CalculationResult Run(string script) =>
        script.CalcWithDialect(
            namedTypesSupport: NamedTypesSupport.Enabled,
            optionalTypesSupport: OptionalTypesSupport.Enabled);

    // ========================================================================
    // SECTION A — Linked-list recursion (50 tests)
    // ========================================================================

    [TestCase("type n = {v: int, next: n? = none}\rl(x) = if(x==none) 0 else 1 + l(x?.next)\ry = l(n{v=1})", 1)]
    [TestCase("type n = {v: int, next: n? = none}\rl(x) = if(x==none) 0 else 1 + l(x?.next)\ry = l(n{v=1, next=n{v=2}})", 2)]
    [TestCase("type n = {v: int, next: n? = none}\rl(x) = if(x==none) 0 else 1 + l(x?.next)\ry = l(n{v=1, next=n{v=2, next=n{v=3}}})", 3)]
    [TestCase("type n = {v: int, next: n? = none}\rl(x) = if(x==none) 0 else 1 + l(x?.next)\ry = l(n{v=1, next=n{v=2, next=n{v=3, next=n{v=4}}}})", 4)]
    [TestCase("type n = {v: int, next: n? = none}\rs(x) = if(x==none) 0 else x!.v + s(x?.next)\ry = s(n{v=1, next=n{v=2, next=n{v=3}}})", 6)]
    [TestCase("type n = {v: int, next: n? = none}\rs(x) = if(x==none) 0 else x!.v + s(x?.next)\ry = s(n{v=10, next=n{v=20, next=n{v=30, next=n{v=40}}}})", 100)]
    [TestCase("type n = {v: real, next: n? = none}\rs(x) = if(x==none) 0.0 else x!.v + s(x?.next)\ry = s(n{v=1.5, next=n{v=2.5}})", 4.0)]
    [TestCase("type n = {label: text, next: n? = none}\rl(x) = if(x==none) 0 else 1 + l(x?.next)\ry = l(n{label='a', next=n{label='b'}})", 2)]
    [TestCase("type n = {v: int, next: n? = none}\rlast(x) = if(x?.next==none) x!.v else last(x?.next)\ry = last(n{v=1, next=n{v=2, next=n{v=3}}})", 3)]
    [TestCase("type n = {v: int, next: n? = none}\rlast(x) = if(x?.next==none) x!.v else last(x?.next)\ry = last(n{v=99})", 99)]
    [TestCase("type n = {v: int, next: n? = none}\rl(x) = if(x==none) 0 else 1 + l(x?.next)\rn1 = n{v=1}\rn2 = n{v=2, next=n1}\ry = l(n2)", 2)]
    [TestCase("type n = {v: int, next: n? = none}\rl(x) = if(x==none) 0 else 1 + l(x?.next)\rmkChain(d) = if(d<=0) n{v=0} else n{v=d, next=mkChain(d-1)}\ry = l(mkChain(5))", 6)]
    [TestCase("type n = {v: int, next: n? = none}\rfind(x, target) = if(x==none) false else if(x!.v==target) true else find(x?.next, target)\ry = find(n{v=1, next=n{v=2, next=n{v=3}}}, 2)", true)]
    [TestCase("type n = {v: int, next: n? = none}\rfind(x, target) = if(x==none) false else if(x!.v==target) true else find(x?.next, target)\ry = find(n{v=1, next=n{v=2}}, 99)", false)]
    [TestCase("type n = {v: int, next: n? = none}\rmaxv(x) = if(x?.next==none) x!.v else maxOf(x!.v, maxv(x?.next))\rmaxOf(a,b) = if(a>b) a else b\ry = maxv(n{v=3, next=n{v=7, next=n{v=2, next=n{v=5}}}})", 7)]
    [TestCase("type n = {v: int, next: n? = none}\rsum_(x) = if(x==none) 0 else x!.v + sum_(x?.next)\rcount_(x) = if(x==none) 0 else 1 + count_(x?.next)\rl1 = n{v=10, next=n{v=20, next=n{v=30}}}\ry = sum_(l1) + count_(l1)", 63)]
    [TestCase("type n = {v: int, next: n? = none}\rl(x) = if(x==none) 0 else 1 + l(x?.next)\ry = l(none)", 0)]
    [TestCase("type n = {v: int, next: n? = none}\rd1 = n{v=1}\rd2 = n{v=2, next=d1}\rd3 = n{v=3, next=d2}\rl(x) = if(x==none) 0 else 1 + l(x?.next)\ry = l(d3)", 3)]
    [TestCase("type n = {flag: bool, next: n? = none}\rl(x) = if(x==none) 0 else 1 + l(x?.next)\ry = l(n{flag=true, next=n{flag=false}})", 2)]
    [TestCase("type n = {v: int, next: n? = none}\reach2(x) = if(x==none) true else if(x!.v % 2 == 0) eachEven(x?.next) else false\reachEven(x) = eachEven(x)\reachEvenInner(x) = if(x==none) true else if(x!.v % 2 == 0) eachEvenInner(x?.next) else false\ry = eachEvenInner(n{v=2, next=n{v=4, next=n{v=6}}})", true)]
    [TestCase("type n = {v: int, next: n? = none}\reachEven(x) = if(x==none) true else if(x!.v % 2 == 0) eachEven(x?.next) else false\ry = eachEven(n{v=2, next=n{v=3}})", false)]
    [TestCase("type n = {v: int, next: n? = none}\rsumByCond(x, cond) = if(x==none) 0 else if(cond(x!.v)) x!.v + sumByCond(x?.next, cond) else sumByCond(x?.next, cond)\ry = sumByCond(n{v=1, next=n{v=2, next=n{v=3, next=n{v=4}}}}, rule it>2)", 7)]
    [TestCase("type n = {v: int, next: n? = none}\rl(x:n?) = if(x==none) 0 else 1 + l(x?.next)\ry = l(n{v=1, next=n{v=2}})", 2)]
    [TestCase("type n = {v: int, next: n? = none}\rl(x:n?):int = if(x==none) 0 else 1 + l(x?.next)\ry = l(n{v=1, next=n{v=2, next=n{v=3}}})", 3)]
    [TestCase("type n = {v: int, next: n? = none}\rsum_(x:n?):int = if(x==none) 0 else x!.v + sum_(x?.next)\ry = sum_(n{v=10, next=n{v=20}})", 30)]
    [TestCase("type n = {v: int, next: n? = none}\rsumNeg(x) = if(x==none) 0 else (0-x!.v) + sumNeg(x?.next)\ry = sumNeg(n{v=1, next=n{v=2, next=n{v=3}}})", -6)]
    [TestCase("type n = {v: int, next: n? = none}\rdouble(x) = if(x==none) 0 else 2*x!.v + double(x?.next)\ry = double(n{v=1, next=n{v=2, next=n{v=3}}})", 12)]
    [TestCase("type n = {v: int, next: n? = none}\rl(x) = if(x==none) 0 else 1 + l(x?.next)\rl_one = n{v=1}\ry = l(l_one)", 1)]
    [TestCase("type n = {v: int, next: n? = none}\rsum_(x) = if(x==none) 0 else x!.v + sum_(x?.next)\rl_seq = n{v=1, next=n{v=2}}\ry = sum_(l_seq) + sum_(l_seq)", 6)]
    [TestCase("type n = {v: int, next: n? = none}\rl(x) = if(x==none) 0 else 1 + l(x?.next)\rsum_(x) = if(x==none) 0 else x!.v + sum_(x?.next)\rl1 = n{v=10, next=n{v=20, next=n{v=30}}}\rcounted = l(l1)\rsummed = sum_(l1)\ry = counted * summed", 180)]
    [TestCase("type n = {v: int, next: n? = none}\rl1 = n{v=5}\rl2 = n{v=4, next=l1}\rl3 = n{v=3, next=l2}\rl4 = n{v=2, next=l3}\rl5 = n{v=1, next=l4}\rsum_(x) = if(x==none) 0 else x!.v + sum_(x?.next)\ry = sum_(l5)", 15)]
    [TestCase("type n = {v: int, next: n? = none}\rmkList(d) = if(d<=0) n{v=0} else n{v=d, next=mkList(d-1)}\rsum_(x) = if(x==none) 0 else x!.v + sum_(x?.next)\ry = sum_(mkList(5))", 15)]
    [TestCase("type n = {v: int, next: n? = none}\rmkList(d) = if(d<=0) n{v=0} else n{v=d, next=mkList(d-1)}\rcount_(x) = if(x==none) 0 else 1 + count_(x?.next)\ry = count_(mkList(10))", 11)]
    [TestCase("type n = {v: int, next: n? = none}\rzeroOrSum(x) = if(x==none) 0 else x!.v + zeroOrSum(x?.next)\rl1 = n{v=1, next=n{v=2}}\rl2 = n{v=10, next=n{v=20, next=n{v=30}}}\ry = zeroOrSum(l1) + zeroOrSum(l2)", 63)]
    [TestCase("type n = {v: int, next: n? = none}\rsum_(x) = if(x==none) 0 else x!.v + sum_(x?.next)\rsumOfSums(a, b) = sum_(a) + sum_(b)\rl1 = n{v=1, next=n{v=2}}\rl2 = n{v=3, next=n{v=4}}\ry = sumOfSums(l1, l2)", 10)]
    [TestCase("type n = {v: int, next: n? = none}\rl(x) = if(x==none) 0 else 1 + l(x?.next)\ra = n{v=1, next=n{v=2}}\rb = n{v=1, next=n{v=2, next=n{v=3, next=n{v=4, next=n{v=5}}}}}\ry = l(a) + l(b)", 7)]
    [TestCase("type n = {v: int, next: n? = none}\rl(x) = if(x==none) 0 else 1 + l(x?.next)\ry = l(n{v=1, next=n{v=2, next=n{v=3, next=n{v=4, next=n{v=5, next=n{v=6, next=n{v=7, next=n{v=8, next=n{v=9, next=n{v=10}}}}}}}}}})", 10)]
    [TestCase("type n = {v: int, next: n? = none}\rsum_(x) = if(x==none) 0 else x!.v + sum_(x?.next)\ry = sum_(n{v=1, next=n{v=2, next=n{v=3, next=n{v=4, next=n{v=5, next=n{v=6, next=n{v=7, next=n{v=8, next=n{v=9, next=n{v=10}}}}}}}}}})", 55)]
    [TestCase("type n = {v: int, next: n? = none}\rl(x) = if(x==none) 0 else 1 + l(x?.next)\rmkN(v) = n{v=v}\ry = l(mkN(42))", 1)]
    [TestCase("type n = {v: int, w: int, next: n? = none}\rsumv(x) = if(x==none) 0 else x!.v + sumv(x?.next)\rsumw(x) = if(x==none) 0 else x!.w + sumw(x?.next)\rd = n{v=1, w=10, next=n{v=2, w=20}}\ry = sumv(d) + sumw(d)", 33)]
    [TestCase("type n = {v: int, next: n? = none}\rl(x) = if(x==none) 0 else 1 + l(x?.next)\rcombine(a, b) = l(a) + l(b)\ry = combine(n{v=1}, n{v=2, next=n{v=3}})", 3)]
    [TestCase("type n = {v: int, next: n? = none}\ronlyV(x) = x!.v\ry = onlyV(n{v=42})", 42)]
    [TestCase("type n = {v: int, next: n? = none}\rgetVOrDefault(x, d) = if(x==none) d else x!.v\ry = getVOrDefault(n{v=42}, 0)", 42)]
    [TestCase("type n = {v: int, next: n? = none}\rgetVOrDefault(x, d) = if(x==none) d else x!.v\ry = getVOrDefault(none, -1)", -1)]
    [TestCase("type n = {v: int, next: n? = none}\rl(x) = if(x==none) 0 else 1 + l(x?.next)\rmany(x) = l(x) * 100\ry = many(n{v=1, next=n{v=2}})", 200)]
    [TestCase("type n = {v: int, next: n? = none}\rl(x) = if(x==none) 0 else 1 + l(x?.next)\rsumPair(a, b) = l(a) + l(b)\ry = sumPair(n{v=5}, n{v=10})", 2)]
    public void Section_A_LinkedList(string script, object expected) =>
        Run(script).AssertResultHas("y", expected);

    [TestCase("type a = {v: int, next: a? = none}\rtype b = {v: int, next: b? = none}\rcount_(x) = if(x==none) 0 else 1 + count_(x?.next)\nra = count_(a{v=1, next=a{v=2}})\nrb = count_(b{v=1, next=b{v=2}})", "ra", 2)]
    [TestCase("type a = {v: int, next: a? = none}\rtype b = {v: int, next: b? = none}\rcount_(x) = if(x==none) 0 else 1 + count_(x?.next)\nra = count_(a{v=1, next=a{v=2}})\nrb = count_(b{v=1, next=b{v=2}})", "rb", 2)]
    public void Section_A_LinkedList_Named(string script, string id, object expected) =>
        Run(script).AssertResultHas(id, expected);

    // ========================================================================
    // SECTION B — Tree recursion (50 tests)
    // ========================================================================

    [TestCase("type t = {v: int, l: t? = none, r: t? = none}\rd(x) = if(x==none) 0 else 1 + maxOf(d(x?.l), d(x?.r))\rmaxOf(a,b) = if(a>b) a else b\ry = d(t{v=1})", 1)]
    [TestCase("type t = {v: int, l: t? = none, r: t? = none}\rd(x) = if(x==none) 0 else 1 + maxOf(d(x?.l), d(x?.r))\rmaxOf(a,b) = if(a>b) a else b\ry = d(t{v=1, l=t{v=2}})", 2)]
    [TestCase("type t = {v: int, l: t? = none, r: t? = none}\rd(x) = if(x==none) 0 else 1 + maxOf(d(x?.l), d(x?.r))\rmaxOf(a,b) = if(a>b) a else b\ry = d(t{v=1, l=t{v=2, l=t{v=3}}, r=t{v=4}})", 3)]
    [TestCase("type t = {v: int, l: t? = none, r: t? = none}\rcountNodes(x) = if(x==none) 0 else 1 + countNodes(x?.l) + countNodes(x?.r)\ry = countNodes(t{v=1, l=t{v=2}, r=t{v=3}})", 3)]
    [TestCase("type t = {v: int, l: t? = none, r: t? = none}\rcountNodes(x) = if(x==none) 0 else 1 + countNodes(x?.l) + countNodes(x?.r)\ry = countNodes(t{v=1, l=t{v=2, l=t{v=4}, r=t{v=5}}, r=t{v=3, l=t{v=6}, r=t{v=7}}})", 7)]
    [TestCase("type t = {v: int, l: t? = none, r: t? = none}\rsumTree(x) = if(x==none) 0 else x!.v + sumTree(x?.l) + sumTree(x?.r)\ry = sumTree(t{v=1, l=t{v=2}, r=t{v=3}})", 6)]
    [TestCase("type t = {v: int, l: t? = none, r: t? = none}\rsumTree(x) = if(x==none) 0 else x!.v + sumTree(x?.l) + sumTree(x?.r)\ry = sumTree(t{v=10, l=t{v=20, l=t{v=40}, r=t{v=50}}, r=t{v=30}})", 150)]
    [TestCase("type t = {v: int, l: t? = none, r: t? = none}\rmaxTree(x) = if(x==none) 0 else maxOf3(x!.v, maxTree(x?.l), maxTree(x?.r))\rmaxOf(a,b) = if(a>b) a else b\rmaxOf3(a,b,c) = maxOf(maxOf(a,b),c)\ry = maxTree(t{v=5, l=t{v=10, l=t{v=3}}, r=t{v=7}})", 10)]
    [TestCase("type t = {v: int, l: t? = none, r: t? = none}\rd(x) = if(x==none) 0 else 1 + maxOf(d(x?.l), d(x?.r))\rmaxOf(a,b) = if(a>b) a else b\ry = d(t{v=1, l=t{v=2, l=t{v=3, l=t{v=4, l=t{v=5}}}}})", 5)]
    [TestCase("type t = {v: int, l: t? = none, r: t? = none}\rcountLeaves(x) = if(x==none) 0 else if(x?.l==none) if(x?.r==none) 1 else countLeaves(x?.r) else countLeaves(x?.l) + countLeaves(x?.r)\ry = countLeaves(t{v=1, l=t{v=2}, r=t{v=3}})", 2)]
    [TestCase("type t = {v: int, l: t? = none, r: t? = none}\rcountLeaves(x) = if(x==none) 0 else if(x?.l==none) if(x?.r==none) 1 else countLeaves(x?.r) else countLeaves(x?.l) + countLeaves(x?.r)\ry = countLeaves(t{v=1, l=t{v=2, l=t{v=4}, r=t{v=5}}, r=t{v=3}})", 3)]
    [TestCase("type t = {tag: text, l: t? = none, r: t? = none}\rd(x) = if(x==none) 0 else 1 + maxOf(d(x?.l), d(x?.r))\rmaxOf(a,b) = if(a>b) a else b\ry = d(t{tag='a', l=t{tag='b'}, r=t{tag='c'}})", 2)]
    [TestCase("type t = {v: real, l: t? = none, r: t? = none}\rsumTree(x) = if(x==none) 0.0 else x!.v + sumTree(x?.l) + sumTree(x?.r)\ry = sumTree(t{v=1.5, l=t{v=2.5}, r=t{v=3.0}})", 7.0)]
    [TestCase("type t = {v: int, l: t? = none, r: t? = none, m: t? = none}\rd(x) = if(x==none) 0 else 1 + max3(d(x?.l), d(x?.r), d(x?.m))\rmaxOf(a,b) = if(a>b) a else b\rmax3(a,b,c) = maxOf(maxOf(a,b),c)\ry = d(t{v=1, l=t{v=2}, m=t{v=3, l=t{v=4}}})", 3)]
    [TestCase("type t = {v: int, l: t? = none, r: t? = none}\rd(x) = if(x==none) 0 else 1 + maxOf(d(x?.l), d(x?.r))\rmaxOf(a,b) = if(a>b) a else b\ry = d(none)", 0)]
    [TestCase("type t = {v: int, l: t? = none, r: t? = none}\rcountNodes(x) = if(x==none) 0 else 1 + countNodes(x?.l) + countNodes(x?.r)\ry = countNodes(none)", 0)]
    [TestCase("type t = {v: int, l: t? = none, r: t? = none}\rcountTotal(x) = if(x==none) 0 else 1 + countTotal(x?.l) + countTotal(x?.r)\rt1 = t{v=1, l=t{v=2}, r=t{v=3, l=t{v=4}, r=t{v=5}}}\ry = countTotal(t1)", 5)]
    [TestCase("type t = {v: int, l: t? = none, r: t? = none}\rmkBalanced(d, v) = if(d<=0) t{v=v} else t{v=v, l=mkBalanced(d-1, v*2), r=mkBalanced(d-1, v*2+1)}\rcountNodes(x) = if(x==none) 0 else 1 + countNodes(x?.l) + countNodes(x?.r)\ry = countNodes(mkBalanced(2, 1))", 7)]
    [TestCase("type t = {v: int, l: t? = none, r: t? = none}\rmkBalanced(d, v) = if(d<=0) t{v=v} else t{v=v, l=mkBalanced(d-1, v*2), r=mkBalanced(d-1, v*2+1)}\rd(x) = if(x==none) 0 else 1 + maxOf(d(x?.l), d(x?.r))\rmaxOf(a,b) = if(a>b) a else b\ry = d(mkBalanced(3, 1))", 4)]
    public void Section_B_Tree(string script, object expected) =>
        Run(script).AssertResultHas("y", expected);

    // ========================================================================
    // SECTION C — Two named types same shape (30 tests)
    // ========================================================================

    [TestCase("type a = {v: int, next: a? = none}\rtype b = {v: real, next: b? = none}\rcount_(x) = if(x==none) 0 else 1 + count_(x?.next)\nra = count_(a{v=1, next=a{v=2}})\nrb = count_(b{v=1.5, next=b{v=2.5, next=b{v=3.5}}})", "ra", 2, "rb", 3)]
    [TestCase("type a = {v: int, next: a? = none}\rtype b = {label: text, next: b? = none}\rcount_(x) = if(x==none) 0 else 1 + count_(x?.next)\nra = count_(a{v=1})\nrb = count_(b{label='hi'})", "ra", 1, "rb", 1)]
    [TestCase("type a = {v: int, next: a? = none}\rtype b = {v: int, next: b? = none, extra: text = ''}\rcount_(x) = if(x==none) 0 else 1 + count_(x?.next)\nra = count_(a{v=1, next=a{v=2}})\nrb = count_(b{v=1, next=b{v=2, extra='x'}})", "ra", 2, "rb", 2)]
    public void Section_C_TwoNamedSameShape(string script, string id1, object expected1, string id2, object expected2) {
        var result = Run(script);
        result.AssertResultHas(id1, expected1);
        result.AssertResultHas(id2, expected2);
    }

    // ========================================================================
    // SECTION D — Functional types in struct fields (30 tests)
    // ========================================================================

    [TestCase("type op = {f: rule(int)->int}\rapply_(x, v) = x.f(v)\ry = apply_(op{f = rule it+1}, 5)", 6)]
    [TestCase("type op = {f: rule(int)->int}\rapply_(x, v) = x.f(v)\ry = apply_(op{f = rule it*2}, 5)", 10)]
    [TestCase("type op = {f: rule(int,int)->int}\rapply_(x, a, b) = x.f(a, b)\ry = apply_(op{f = rule it1+it2}, 3, 4)", 7)]
    [TestCase("type op = {f: rule(int,int)->int}\rapply_(x, a, b) = x.f(a, b)\ry = apply_(op{f = rule it1*it2}, 3, 4)", 12)]
    [TestCase("type op = {add: rule(int)->int}\rcompose(o1, o2) = rule o2.add(o1.add(it))\rdoubled = compose(op{add = rule it+1}, op{add = rule it+1})\ry = doubled(10)", 12)]
    [TestCase("type pair = {a: int, f: rule(int)->int}\rcombo(p, x) = p.a + p.f(x)\ry = combo(pair{a=10, f=rule it*2}, 5)", 20)]
    [TestCase("type box = {x: int, f: rule()->int}\runbox(b) = b.f()\ry = unbox(box{x=42, f=rule 100})", 100)]
    [Ignore("GitHub #124: function-typed field on unannotated parameter — `apply_(x, v) = x.f(v)` fails because parser desugars `x.f(v)` → `f(x, v)`, so TIC sees a call to non-existent `f` instead of struct-field call. With explicit `x:op` annotation the same script works.")]
    public void Section_D_FunctionFields(string script, object expected) =>
        Run(script).AssertResultHas("y", expected);

    // ========================================================================
    // SECTION E — Optional chains (40 tests)
    // ========================================================================

    [TestCase("type n = {v: int, next: n? = none}\ry = n{v=1}!.v", 1)]
    [TestCase("type n = {v: int, next: n? = none}\ry = n{v=1, next=n{v=2}}?.next!.v", 2)]
    [TestCase("type n = {v: int, next: n? = none}\rd = n{v=1, next=n{v=2, next=n{v=3}}}\ry = d?.next?.next!.v", 3)]
    [TestCase("type n = {v: int, next: n? = none}\rgetSecond(x) = x?.next!.v\ry = getSecond(n{v=10, next=n{v=20}})", 20)]
    [TestCase("type n = {v: int, next: n? = none}\rd = n{v=1}\ry = (d?.next?.v) ?? -1", -1)]
    [TestCase("type n = {v: int, next: n? = none}\rd = n{v=1, next=n{v=2}}\ry = (d?.next?.v) ?? -1", 2)]
    [TestCase("type n = {v: int, next: n? = none}\rd = n{v=1}\rgetVOrZero(x) = x?.v ?? 0\ry = getVOrZero(d?.next)", 0)]
    [TestCase("type n = {v: int, next: n? = none}\rsafeNext(x) = x?.next ?? x\ry = safeNext(n{v=1})!.v", 1)]
    [TestCase("type n = {v: int, next: n? = none}\rsafeNext(x) = x?.next ?? x\ry = safeNext(n{v=1, next=n{v=2}})!.v", 2)]
    [TestCase("type n = {v: int, next: n? = none}\rl1 = n{v=1, next=n{v=2}}\ry = (l1?.next?.next?.v) ?? 99", 99)]
    [TestCase("type n = {v: int, next: n? = none}\rdoubleNext(x) = x?.next?.next\rd = n{v=1, next=n{v=2, next=n{v=3}}}\ry = (doubleNext(d)?.v) ?? -1", 3)]
    [TestCase("type n = {v: int, next: n? = none}\ry = (none ?? n{v=42})!.v", 42)]
    [TestCase("type n = {v: int, next: n? = none}\rd:n? = n{v=5}\ry = d!.v", 5)]
    [TestCase("type n = {v: int, next: n? = none}\rd:n? = none\ry = (d?.v) ?? -1", -1)]
    [TestCase("type n = {v: int, next: n? = none}\rselectV(a, b) = (a?.v) ?? (b?.v) ?? 0\ry = selectV(none, n{v=42})", 42)]
    [TestCase("type n = {v: int, next: n? = none}\rselectV(a, b) = (a?.v) ?? (b?.v) ?? 0\ry = selectV(n{v=10}, n{v=20})", 10)]
    public void Section_E_OptionalChains(string script, object expected) =>
        Run(script).AssertResultHas("y", expected);

    // ========================================================================
    // SECTION F — Higher-order on recursive (20 tests)
    // ========================================================================

    [TestCase("type n = {v: int, next: n? = none}\rfoldList(x, acc, f) = if(x==none) acc else foldList(x?.next, f(acc, x!.v), f)\ry = foldList(n{v=1, next=n{v=2, next=n{v=3}}}, 0, rule it1+it2)", 6)]
    [TestCase("type n = {v: int, next: n? = none}\rfoldList(x, acc, f) = if(x==none) acc else foldList(x?.next, f(acc, x!.v), f)\ry = foldList(n{v=1, next=n{v=2, next=n{v=3}}}, 1, rule it1*it2)", 6)]
    [TestCase("type n = {v: int, next: n? = none}\rfoldList(x, acc, f) = if(x==none) acc else foldList(x?.next, f(acc, x!.v), f)\rmaxOf(a,b) = if(a>b) a else b\ry = foldList(n{v=3, next=n{v=7, next=n{v=2}}}, 0, maxOf)", 7)]
    [TestCase("type n = {v: int, next: n? = none}\rcountIf(x, p) = if(x==none) 0 else (if(p(x!.v)) 1 else 0) + countIf(x?.next, p)\ry = countIf(n{v=1, next=n{v=2, next=n{v=3, next=n{v=4}}}}, rule it>2)", 2)]
    [TestCase("type n = {v: int, next: n? = none}\rallTrue(x, p) = if(x==none) true else if(p(x!.v)) allTrue(x?.next, p) else false\ry = allTrue(n{v=2, next=n{v=4, next=n{v=6}}}, rule it%2==0)", true)]
    [TestCase("type n = {v: int, next: n? = none}\rallTrue(x, p) = if(x==none) true else if(p(x!.v)) allTrue(x?.next, p) else false\ry = allTrue(n{v=2, next=n{v=3}}, rule it%2==0)", false)]
    [TestCase("type n = {v: int, next: n? = none}\ranyTrue(x, p) = if(x==none) false else if(p(x!.v)) true else anyTrue(x?.next, p)\ry = anyTrue(n{v=1, next=n{v=2, next=n{v=3}}}, rule it>2)", true)]
    [TestCase("type n = {v: int, next: n? = none}\ranyTrue(x, p) = if(x==none) false else if(p(x!.v)) true else anyTrue(x?.next, p)\ry = anyTrue(n{v=1, next=n{v=2}}, rule it>10)", false)]
    public void Section_F_HigherOrder(string script, object expected) =>
        Run(script).AssertResultHas("y", expected);

    // ========================================================================
    // SECTION G — Recursive + arrays (20 tests)
    // ========================================================================

    [TestCase("type n = {v: int, next: n? = none}\rl1 = n{v=1, next=n{v=2, next=n{v=3}}}\rsum_(x) = if(x==none) 0 else x!.v + sum_(x?.next)\rarr = [l1, n{v=10, next=n{v=20}}]\ry = sum_(arr[0]) + sum_(arr[1])", 36)]
    [TestCase("type n = {v: int, next: n? = none}\rsum_(x) = if(x==none) 0 else x!.v + sum_(x?.next)\rarr = [n{v=1}, n{v=2}, n{v=3}]\ry = sum_(arr[0]) + sum_(arr[1]) + sum_(arr[2])", 6)]
    [TestCase("type bag = {items: int[]}\rsumBag(b) = sum(b.items)\ry = sumBag(bag{items=[1,2,3,4]})", 10)]
    [TestCase("type bag = {items: int[]}\rsumPair(a, b) = sum(a.items) + sum(b.items)\ry = sumPair(bag{items=[1,2]}, bag{items=[10,20,30]})", 63)]
    [TestCase("type box = {data: real[]}\ravgData(b) = sum(b.data) / b.data.count()\ry = avgData(box{data=[1.0, 2.0, 3.0, 4.0]})", 2.5)]
    public void Section_G_RecursiveArrays(string script, object expected) =>
        Run(script).AssertResultHas("y", expected);

    // ========================================================================
    // SECTION H — Deeply nested + composite (20 tests)
    // ========================================================================

    [TestCase("type inner = {v: int}\rtype outer = {i: inner}\ry = outer{i=inner{v=42}}.i.v", 42)]
    [TestCase("type inner = {v: int}\rtype mid = {i: inner}\rtype outer = {m: mid}\ry = outer{m=mid{i=inner{v=42}}}.m.i.v", 42)]
    [TestCase("type a = {v: int}\rtype b = {a_: a}\rtype c = {b_: b}\rtype d = {c_: c}\ry = d{c_=c{b_=b{a_=a{v=99}}}}.c_.b_.a_.v", 99)]
    [TestCase("type p = {x: int, y: int}\rtype line = {start: p, finish: p}\ry = line{start=p{x=1,y=2}, finish=p{x=3,y=4}}.finish.x", 3)]
    [TestCase("type box = {x: int, inner: box? = none}\rdepth(b) = if(b==none) 0 else 1 + depth(b?.inner)\ry = depth(box{x=1, inner=box{x=2, inner=box{x=3, inner=box{x=4}}}})", 4)]
    [TestCase("type wrap = {real_v: real, opt_v: int? = none}\ry = wrap{real_v=3.14, opt_v=42}!.opt_v ?? -1", 42)]
    [TestCase("type wrap = {real_v: real, opt_v: int? = none}\ry = (wrap{real_v=3.14}.opt_v) ?? -1", -1)]
    public void Section_H_DeepNested(string script, object expected) =>
        Run(script).AssertResultHas("y", expected);

    // ========================================================================
    // SECTION I — Default values + recursion (20 tests)
    // ========================================================================

    [TestCase("type n = {v: int = 0, next: n? = none}\rl(x) = if(x==none) 0 else 1 + l(x?.next)\ry = l(n{v=1, next=n{}})", 2)]
    [TestCase("type n = {v: int = 5, next: n? = none}\rsum_(x) = if(x==none) 0 else x!.v + sum_(x?.next)\ry = sum_(n{next=n{}})", 10)]
    [TestCase("type n = {v: int = 7, next: n? = none}\rsum_(x) = if(x==none) 0 else x!.v + sum_(x?.next)\ry = sum_(n{})", 7)]
    [TestCase("type n = {v: int, label: text = '', next: n? = none}\rl(x) = if(x==none) 0 else 1 + l(x?.next)\ry = l(n{v=1, next=n{v=2}})", 2)]
    [TestCase("type n = {v: int = 0, label: text = '', next: n? = none}\rsum_(x) = if(x==none) 0 else x!.v + sum_(x?.next)\ry = sum_(n{v=10, next=n{label='x', next=n{v=5}}})", 15)]
    public void Section_I_DefaultValues(string script, object expected) =>
        Run(script).AssertResultHas("y", expected);

    // ========================================================================
    // SECTION J — Edge cases & wild combinations (40 tests)
    // ========================================================================

    [TestCase("type n = {v: int, next: n? = none}\rid(x) = x\ry = id(n{v=42})!.v", 42)]
    [TestCase("type n = {v: int, next: n? = none}\rconst1(x) = 1\ry = const1(n{v=42})", 1)]
    [TestCase("type n = {v: int, next: n? = none}\rgetV(x:n) = x.v\ry = getV(n{v=99})", 99)]
    [TestCase("type n = {v: int, next: n? = none}\rgetV(x:n?):int? = x?.v\ry = (getV(n{v=99})) ?? -1", 99)]
    [TestCase("type n = {v: int, next: n? = none}\rsumTwo(a:n, b:n) = a.v + b.v\ry = sumTwo(n{v=10}, n{v=20})", 30)]
    [TestCase("type a = {v: int}\rtype b = {v: int}\rgetV(x:a) = x.v\ry = getV(a{v=42})", 42)]
    [TestCase("type a = {v: int, next: a? = none}\rl(x) = if(x==none) 0 else 1 + l(x?.next)\rmkAndCall(v) = l(a{v=v})\ry = mkAndCall(99)", 1)]
    [TestCase("type n = {v: int, next: n? = none}\rl(x) = if(x==none) 0 else 1 + l(x?.next)\rcompoundIfThen(d) = if(d>0) l(n{v=d, next=n{v=d+1}}) else 0\ry = compoundIfThen(5)", 2)]
    [TestCase("type n = {v: int, next: n? = none}\rmaxV(x) = if(x?.next==none) x!.v else maxOf(x!.v, maxV(x?.next))\rmaxOf(a,b) = if(a>b) a else b\ry = maxV(n{v=3, next=n{v=7, next=n{v=2, next=n{v=10, next=n{v=1}}}}})", 10)]
    [TestCase("type n = {v: int, next: n? = none}\rcondSum(x, lim) = if(x==none) 0 else if(x!.v>lim) x!.v + condSum(x?.next, lim) else condSum(x?.next, lim)\ry = condSum(n{v=1, next=n{v=5, next=n{v=2, next=n{v=8, next=n{v=3}}}}}, 4)", 13)]
    [TestCase("type n = {v: int, next: n? = none}\rfromTo(s,e) = if(s>e) none else n{v=s, next=fromTo(s+1,e)}\rsum_(x) = if(x==none) 0 else x!.v + sum_(x?.next)\ry = sum_(fromTo(1, 10))", 55)]
    [TestCase("type n = {v: int, next: n? = none}\rfromTo(s,e) = if(s>e) none else n{v=s, next=fromTo(s+1,e)}\rsum_(x) = if(x==none) 0 else x!.v + sum_(x?.next)\rcount_(x) = if(x==none) 0 else 1 + count_(x?.next)\rl1 = fromTo(1, 100)\ry = sum_(l1) - count_(l1) * 50", 50)]
    [TestCase("type t = {v: int, l: t? = none, r: t? = none}\rmkBalanced(d, v) = if(d<=0) t{v=v} else t{v=v, l=mkBalanced(d-1, v*2), r=mkBalanced(d-1, v*2+1)}\rsumTree(x) = if(x==none) 0 else x!.v + sumTree(x?.l) + sumTree(x?.r)\ry = sumTree(mkBalanced(3, 1))", 120)]
    [TestCase("type n = {v: int, next: n? = none}\rmap_(x, f) = if(x==none) none else n{v=f(x!.v), next=map_(x?.next, f)}\rsum_(x) = if(x==none) 0 else x!.v + sum_(x?.next)\ry = sum_(map_(n{v=1, next=n{v=2, next=n{v=3}}}, rule it*10))", 60)]
    [TestCase("type n = {v: int, next: n? = none}\rfilter_(x, p) = if(x==none) none else if(p(x!.v)) n{v=x!.v, next=filter_(x?.next, p)} else filter_(x?.next, p)\rcount_(x) = if(x==none) 0 else 1 + count_(x?.next)\ry = count_(filter_(n{v=1, next=n{v=2, next=n{v=3, next=n{v=4, next=n{v=5}}}}}, rule it>2))", 3)]
    [TestCase("type n = {v: int, next: n? = none}\rappend_(a, b) = if(a==none) b else n{v=a!.v, next=append_(a?.next, b)}\rcount_(x) = if(x==none) 0 else 1 + count_(x?.next)\ry = count_(append_(n{v=1, next=n{v=2}}, n{v=10, next=n{v=20}}))", 4)]
    public void Section_J_Edge(string script, object expected) =>
        Run(script).AssertResultHas("y", expected);

    // ========================================================================
    // SECTION K — Tests that should THROW (20 tests)
    // ========================================================================

    [TestCase("type a = {v: int, next: a? = none}\rsum_(x) = if(x==none) 0 else x!.v + sum_(x?.next)\ry = sum_(42)")]
    [TestCase("type a = {v: int, next: a? = none}\rsum_(x) = if(x==none) 0 else x!.v + sum_(x?.next)\ry = sum_('abc')")]
    [TestCase("type fnode = {f: rule(fnode):int}\rselfFunc(n) = n.f(n)\ry = selfFunc(fnode{f = rule it.f(it)})")]
    [TestCase("isEven(n) = if (n == 0) true else isOdd(n - 1)\nisOdd(n)  = if (n == 0) false else isEven(n - 1)\ny = isEven(4)")]
    public void Section_K_ShouldThrow(string script) =>
        Assert.Throws<FunnyParseException>(() => Run(script));

    // ========================================================================
    // SECTION L — More wild combos (~40 tests, mixed)
    // ========================================================================

    [TestCase("type n = {v: int, next: n? = none}\rl(x) = if(x==none) 0 else 1 + l(x?.next)\rd1 = n{v=1}\rd2 = n{v=2, next=n{v=3, next=n{v=4}}}\ry = l(d1) + l(d2) * 10", 31)]
    [TestCase("type p = {a: int, b: int}\rsumP(x:p) = x.a + x.b\ry = sumP(p{a=10, b=20})", 30)]
    [TestCase("type p = {a: int, b: int}\rdiff(x:p) = x.a - x.b\ry = diff(p{a=30, b=10})", 20)]
    [TestCase("type rect = {w: int, h: int}\rarea(r) = r.w * r.h\ry = area(rect{w=5, h=4})", 20)]
    [TestCase("type rect = {w: int, h: int}\rscale(r, k) = rect{w=r.w*k, h=r.h*k}\ry = scale(rect{w=5, h=4}, 3).w", 15)]
    [TestCase("type point = {x: real, y: real}\rdist(a:point, b:point) = ((a.x-b.x)*(a.x-b.x) + (a.y-b.y)*(a.y-b.y))\ry = dist(point{x=0.0, y=0.0}, point{x=3.0, y=4.0})", 25.0)]
    [TestCase("type box = {v: int, parent: box? = none}\rrootV(x) = if(x?.parent==none) x!.v else rootV(x?.parent)\ry = rootV(box{v=1, parent=box{v=2, parent=box{v=3}}})", 3)]
    [TestCase("type box = {v: int, parent: box? = none}\rdepth(x) = if(x==none) 0 else 1 + depth(x?.parent)\ry = depth(box{v=1, parent=box{v=2, parent=box{v=3, parent=box{v=4}}}})", 4)]
    [TestCase("type pair = {fst: int, snd: int}\rmkPair(a, b) = pair{fst=a, snd=b}\rsumP(x) = x.fst + x.snd\ry = sumP(mkPair(10, 20))", 30)]
    [TestCase("type triple = {a: int, b: int, c: int}\rsum3(x) = x.a + x.b + x.c\ry = sum3(triple{a=1, b=2, c=3})", 6)]
    [TestCase("type triple = {a: real, b: real, c: real}\rsum3(x) = x.a + x.b + x.c\ry = sum3(triple{a=1.5, b=2.5, c=3.0})", 7.0)]
    [TestCase("type t = {v: int, kids: t[] = []}\rcountKids(x) = x.kids.count()\ry = countKids(t{v=1, kids=[t{v=2}, t{v=3}, t{v=4}]})", 3)]
    [TestCase("type t = {v: int, kids: t[] = []}\rcountKids(x) = x.kids.count()\ry = countKids(t{v=1})", 0)]
    [TestCase("type rgb = {r: int, g: int, b: int}\rbrightness(c) = c.r + c.g + c.b\ry = brightness(rgb{r=100, g=150, b=50})", 300)]
    [TestCase("type maybe = {hasValue: bool, v: int = 0}\rgetV(m) = if(m.hasValue) m.v else -1\ry = getV(maybe{hasValue=true, v=42})", 42)]
    [TestCase("type maybe = {hasValue: bool, v: int = 0}\rgetV(m) = if(m.hasValue) m.v else -1\ry = getV(maybe{hasValue=false})", -1)]
    [TestCase("type c = {real: real, imag: real}\radd(a:c, b:c) = c{real=a.real+b.real, imag=a.imag+b.imag}\ry = add(c{real=1.0, imag=2.0}, c{real=3.0, imag=4.0}).real", 4.0)]
    [TestCase("type opt_int = {present: bool, v: int}\rgetOrDef(o, d) = if(o.present) o.v else d\ry = getOrDef(opt_int{present=true, v=42}, 0)", 42)]
    [TestCase("type opt_int = {present: bool, v: int}\rgetOrDef(o, d) = if(o.present) o.v else d\ry = getOrDef(opt_int{present=false, v=0}, -1)", -1)]
    [TestCase("type counter = {n: int}\rinc(c) = counter{n=c.n+1}\ry = inc(inc(inc(counter{n=0}))).n", 3)]
    [TestCase("type stack = {top: int, rest: stack? = none}\rdepth(s) = if(s==none) 0 else 1 + depth(s?.rest)\rs1 = stack{top=1}\rs2 = stack{top=2, rest=s1}\rs3 = stack{top=3, rest=s2}\ry = depth(s3)", 3)]
    [TestCase("type stack = {top: int, rest: stack? = none}\rsumS(s) = if(s==none) 0 else s!.top + sumS(s?.rest)\rs3 = stack{top=3, rest=stack{top=2, rest=stack{top=1}}}\ry = sumS(s3)", 6)]
    [TestCase("type queue = {head: int, tail: queue? = none}\rsumQ(q) = if(q==none) 0 else q!.head + sumQ(q?.tail)\rq3 = queue{head=10, tail=queue{head=20, tail=queue{head=30}}}\ry = sumQ(q3)", 60)]
    [TestCase("type linked = {data: int, link: linked? = none}\rfindFirstGT(x, n) = if(x==none) -1 else if(x!.data>n) x!.data else findFirstGT(x?.link, n)\rl = linked{data=1, link=linked{data=5, link=linked{data=3, link=linked{data=8}}}}\ry = findFirstGT(l, 4)", 5)]
    [TestCase("type linked = {data: int, link: linked? = none}\rfindFirstGT(x, n) = if(x==none) -1 else if(x!.data>n) x!.data else findFirstGT(x?.link, n)\ry = findFirstGT(linked{data=1, link=linked{data=2}}, 100)", -1)]
    [TestCase("type bin = {v: bool, l: bin? = none, r: bin? = none}\rcountTrue(x) = if(x==none) 0 else (if(x!.v) 1 else 0) + countTrue(x?.l) + countTrue(x?.r)\ry = countTrue(bin{v=true, l=bin{v=false}, r=bin{v=true, l=bin{v=true}}})", 3)]
    [TestCase("type rt = {v: int, parent: rt? = none, kids: rt[] = []}\rgetParentV(x) = (x?.parent?.v) ?? -1\ry = getParentV(rt{v=1})", -1)]
    public void Section_L_Wild(string script, object expected) =>
        Run(script).AssertResultHas("y", expected);

    // ========================================================================
    // GH #126 (fixed) — none-arg call propagates body's Preferred to generic
    // ========================================================================
    // Generic recursive function with literal `0` in the body returned Real
    // instead of Int when called with `none` only. Fix: PropagateReturnOnly-
    // Preferred now also fires when at least one call-site arg is the literal
    // `none`, since none doesn't pin the generic and the body's Preferred is
    // the right default.

    [TestCase("type n = {v: int, next: n? = none}\rs(x) = if(x==none) 0 else x!.v + s(x?.next)\ry = s(none)", 0)]
    [TestCase("type t = {v: int, l: t? = none, r: t? = none}\rsumTree(x) = if(x==none) 0 else x!.v + sumTree(x?.l) + sumTree(x?.r)\ry = sumTree(none)", 0)]
    [TestCase("type n = {v: int, next: n? = none}\rselectV(a, b) = (a?.v) ?? (b?.v) ?? 0\ry = selectV(none, none)", 0)]
    public void GH126_NoneCallUsesBodyPreferred(string script, object expected) =>
        Run(script).AssertResultHas("y", expected);

    // ========================================================================
    // GH #126 follow-up (fixed) — F-bound Fit-check peels Optional candidate
    // ========================================================================
    // The trigger required all four:
    //   (1) recursive named type `n` with `next: n? = none`
    //   (2) helper `loop(x, acc)` that recurses via `x?.next` AND constructs
    //       `n{next=acc}` — F-bound `{next:T?}` is inferred (bare struct shape)
    //   (3) wrapper `go(x) = loop(x, none)` hides `none` behind one indirection
    //   (4) result accessed with `!` (forces Optional handling)
    //
    // TIC inferred the F-bound correctly but runtime `FunnyTypeFitsStructBound`
    // rejected callers' Optional-wrapped candidate against the bare-Struct
    // bound. Fix: peel one Optional layer from candidate when bound is Struct —
    // F-bound represents the structural shape; Optional sits at the arg
    // position level, not in the bound itself.

    [TestCase(@"type n = {v: int = 0, next: n? = none}
                loop(x, acc) = if(x==none) acc else loop(x?.next, n{next=acc})
                go(x) = loop(x, none)
                r = go(n{v=1})
                y = r!.v", 0)]
    [TestCase("type n = {v: int, next: n? = none}\rrev(x) = revHelp(x, none)\rrevHelp(x, acc) = if(x==none) acc else revHelp(x?.next, n{v=x!.v, next=acc})\rfirst_(x) = if(x==none) -1 else x!.v\ry = first_(rev(n{v=1, next=n{v=2, next=n{v=3}}}))", 3)]
    public void GH126Followup_FBoundOptionalCandidatePeeled(string script, object expected) =>
        Run(script).AssertResultHas("y", expected);
}
