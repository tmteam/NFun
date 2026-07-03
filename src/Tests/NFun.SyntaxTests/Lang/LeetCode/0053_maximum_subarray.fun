# LeetCode 0053 — Maximum Subarray
#
# Given an integer array `nums`, find the contiguous subarray with the largest
# sum and return that sum. Kadane's algorithm — O(n), no mutation needed
# beyond the two running totals.

fun maxSubarray(nums):
    bestHere = nums[0]
    best = nums[0]
    i = 1
    while i < nums.count():
        bestHere = max(nums[i], bestHere + nums[i])
        best = max(best, bestHere)
        i += 1
    return best

@Test
fun testCanonical():
    assertEqual(maxSubarray([-2, 1, -3, 4, -1, 2, 1, -5, 4]), 6)

@Test
fun testSingleElement():
    assertEqual(maxSubarray([1]), 1)
    assertEqual(maxSubarray([-1]), -1)

@Test
fun testAllPositive():
    assertEqual(maxSubarray([5, 4, -1, 7, 8]), 23)

@Test
fun testAllNegative():
    assertEqual(maxSubarray([-3, -2, -5, -1, -4]), -1)
