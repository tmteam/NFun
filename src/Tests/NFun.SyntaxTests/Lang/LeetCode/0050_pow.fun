# LeetCode 0050 — Pow(x, n)
#
# Compute x raised to the integer power n. Fast exponentiation: O(log |n|),
# iterative form so we don't trip the recursion-depth guard for large n.

fun myPow(x, n):
    if n == 0: return 1.0
    acc = if n < 0: 1.0 / x else: x
    p = if n < 0: -n else: n
    result = 1.0
    while p > 0:
        if p % 2 == 1: result = result * acc
        acc = acc * acc
        p = p // 2
    return result

@Test
fun testInteger():
    assertEqual(myPow(2.0, 10), 1024.0)
    assertEqual(myPow(2.0, 0), 1.0)

@Test
fun testNegativeExponent():
    assertEqual(myPow(2.0, -2), 0.25)

@Test
fun testIdentities():
    assertEqual(myPow(1.0, 100), 1.0)
    assertEqual(myPow(7.0, 1), 7.0)
