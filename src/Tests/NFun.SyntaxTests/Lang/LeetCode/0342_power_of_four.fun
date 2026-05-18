# LeetCode 0342 — Power of Four
#
# A positive integer n is a power of 4 iff it's a power of 2 AND its single
# set bit sits at an even position. The mask `0x55555555` has all even-index
# bits — AND yields nonzero only when the set bit aligns.

fun isPowerOfFour(n):
    if n <= 0: return false
    if (n & (n - 1)) != 0: return false
    return (n & 1431655765) != 0

@Test(1, true)
@Test(4, true)
@Test(16, true)
@Test(64, true)
@Test(256, true)
@Test(2, false)
@Test(8, false)
@Test(5, false)
@Test(0, false)
@Test(-4, false)
fun testIsPowerOfFour(n, expected):
    assertEqual(isPowerOfFour(n), expected)
