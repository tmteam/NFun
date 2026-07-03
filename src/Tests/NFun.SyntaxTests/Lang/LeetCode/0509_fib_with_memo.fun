# LeetCode 0509 variant — Climbing Stairs / Fibonacci in O(log n) via fast
# doubling. Identities:
#   F(2k)   = F(k) · (2 F(k+1) − F(k))
#   F(2k+1) = F(k+1)² + F(k)²
# Iterative, processing the binary digits of n from high to low. Standalone
# from the linear `0509_fibonacci_number.fun`.

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
