# LeetCode 1979 — Find Greatest Common Divisor of Array
#
# GCD of min(nums) and max(nums).

fun gcd(a, b):
    if b == 0: return a
    return gcd(b, a % b)

fun findGCD(nums):
    return gcd(nums.max(), nums.min())

@Test
fun testCanonical():
    assertEqual(findGCD([2, 5, 6, 9, 10]), 2)

@Test
fun testCoprime():
    assertEqual(findGCD([7, 5, 6, 8, 3]), 1)

@Test
fun testSingleton():
    assertEqual(findGCD([3]), 3)
