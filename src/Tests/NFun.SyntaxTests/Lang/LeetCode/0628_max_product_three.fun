# LeetCode 0628 — Maximum Product of Three Numbers
#
# Sort and pick the larger of:
#   - the three largest
#   - the two smallest (most negative — their product is positive) × the largest

fun maximumProduct(nums):
    s = nums.sort()
    n = s.count()
    last3 = s[n - 1] * s[n - 2] * s[n - 3]
    twoSmallTop = s[0] * s[1] * s[n - 1]
    return max(last3, twoSmallTop)

@Test
fun testPositives():
    assertEqual(maximumProduct([1, 2, 3]), 6)

@Test
fun testFour():
    assertEqual(maximumProduct([1, 2, 3, 4]), 24)

@Test
fun testNegativesWin():
    assertEqual(maximumProduct([-100, -98, -1, 2, 3, 4]), 39200)

@Test
fun testWithZeros():
    assertEqual(maximumProduct([-1, 0, 1, 2, 3]), 6)
