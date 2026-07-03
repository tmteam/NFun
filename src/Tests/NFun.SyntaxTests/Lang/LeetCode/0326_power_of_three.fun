# LeetCode 0326 — Power of Three
#
# Return true iff n is a power of 3. Repeatedly divide by 3; if we end at 1,
# n was a power of 3.

fun isPowerOfThree(n):
    if n <= 0: return false
    x = n
    while x % 3 == 0:
        x = x // 3
    return x == 1

@Test(1, true)
@Test(3, true)
@Test(9, true)
@Test(27, true)
@Test(45, false)
@Test(0, false)
@Test(-3, false)
@Test(243, true)
fun testIsPowerOfThree(n, expected):
    assertEqual(isPowerOfThree(n), expected)
