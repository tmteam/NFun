# LeetCode 0231 — Power of Two
#
# Return true if n is a power of two. Bit trick: a positive power of two has
# exactly one bit set, so n & (n-1) clears it and yields 0.

fun isPowerOfTwo(n):
    if n <= 0: return false
    return (n & (n - 1)) == 0

@Test(1, true)
@Test(2, true)
@Test(3, false)
@Test(4, true)
@Test(16, true)
@Test(0, false)
@Test(-2, false)
@Test(1024, true)
@Test(1023, false)
fun testIsPowerOfTwo(n, expected):
    assertEqual(isPowerOfTwo(n), expected)
