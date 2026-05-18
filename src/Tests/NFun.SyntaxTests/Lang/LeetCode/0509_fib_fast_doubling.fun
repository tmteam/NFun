# LeetCode 0509 variant — Fibonacci via fast doubling (O(log n)).
#
# Identities:
#   F(2k)   = F(k) · (2 F(k+1) − F(k))
#   F(2k+1) = F(k+1)² + F(k)²
# Recursive on n/2, packs (F(n), F(n+1)) into a result struct so a single
# recursive call returns both values.

type fibPair = {a: int, b: int}

fun fibFast(n):
    if n == 0: return fibPair {a = 0, b = 1}
    half = fibFast(n // 2)
    c = half.a * (2 * half.b - half.a)
    d = half.a * half.a + half.b * half.b
    if n % 2 == 0:
        return fibPair {a = c, b = d}
    return fibPair {a = d, b = c + d}

fun fib(n):
    return fibFast(n).a

@Test(0, 0)
@Test(1, 1)
@Test(10, 55)
@Test(20, 6765)
@Test(30, 832040)
fun testFib(n, expected):
    assertEqual(fib(n), expected)
