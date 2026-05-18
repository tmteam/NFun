# LeetCode 0908 — Smallest Range I
#
# After adjusting every element by some value in [-k, k], minimise
# max(nums) - min(nums). Trivially max(0, range - 2k).

fun smallestRangeI(nums, k):
    hi = nums.max()
    lo = nums.min()
    return max(0, hi - lo - 2 * k)

@Test(1, 0)
@Test(3, 0)
@Test(0, 0)
fun testSimple(k, expected):
    assertEqual(smallestRangeI([1], k), expected)

@Test
fun testCanonical():
    assertEqual(smallestRangeI([1, 3, 6], 3), 0)

@Test
fun testNotEnough():
    assertEqual(smallestRangeI([0, 10], 2), 6)
