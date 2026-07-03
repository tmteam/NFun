# LeetCode 0152 — Maximum Product Subarray
#
# Track running max and min — a negative number flips which is which.

fun maxProduct(nums):
    best = nums[0]
    curMax = nums[0]
    curMin = nums[0]
    i = 1
    while i < nums.count():
        x = nums[i]
        candA = curMax * x
        candB = curMin * x
        curMax = max(x, max(candA, candB))
        curMin = min(x, min(candA, candB))
        if curMax > best: best = curMax
        i += 1
    return best

@Test
fun testCanonical():
    assertEqual(maxProduct([2, 3, -2, 4]), 6)

@Test
fun testZeroBreaks():
    assertEqual(maxProduct([-2, 0, -1]), 0)

@Test
fun testSingle():
    assertEqual(maxProduct([-2]), -2)

@Test
fun testAllNegativeEven():
    assertEqual(maxProduct([-2, -3, -4]), 12)
