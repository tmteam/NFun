# LeetCode 0961 — N-Repeated Element in Size 2N Array
#
# Exactly one element appears N times; the rest once. Pigeonhole: within any
# window of length 4, at least two slots must be the repeated element. Just
# return the first value seen more than once.

fun repeatedNTimes(nums):
    n = nums.count()
    i = 0
    while i < n:
        j = i + 1
        while j < n and j <= i + 3:
            if nums[i] == nums[j]: return nums[i]
            j += 1
        i += 1
    return -1

@Test
fun testCanonical():
    assertEqual(repeatedNTimes([1, 2, 3, 3]), 3)

@Test
fun testCloseRepeat():
    assertEqual(repeatedNTimes([2, 1, 2, 5, 3, 2]), 2)

@Test
fun testFarRepeat():
    assertEqual(repeatedNTimes([5, 1, 5, 2, 5, 3, 5, 4]), 5)
