# LeetCode 0509 — Fibonacci Number
#
# F(0) = 0, F(1) = 1, F(n) = F(n-1) + F(n-2) for n > 1. Iterative form keeps
# it O(n) without the exponential blow-up of naive recursion (and the spec's
# stack-overflow guard would clip recursion past depth 400 anyway).

fun fib(n):
    if n <= 1: return n
    prev = 0
    curr = 1
    i = 2
    while i <= n:
        next = prev + curr
        prev = curr
        curr = next
        i += 1
    return curr

@Test(0, 0)
@Test(1, 1)
@Test(2, 1)
@Test(3, 2)
@Test(10, 55)
@Test(20, 6765)
@Test(30, 832040)
fun testFib(n, expected):
    assertEqual(fib(n), expected)
