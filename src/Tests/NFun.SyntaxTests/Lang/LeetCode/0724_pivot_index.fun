# LeetCode 0724 — Find Pivot Index
#
# Smallest index where sum(left) == sum(right). Walk through with a running
# left-prefix; right = total - left - current.

fun pivotIndex(nums):
    total = nums.sum()
    left = 0
    i = 0
    while i < nums.count():
        if left == total - left - nums[i]: return i
        left += nums[i]
        i += 1
    return -1

@Test
fun testCanonical():
    assertEqual(pivotIndex([1, 7, 3, 6, 5, 6]), 3)

@Test
fun testNoPivot():
    assertEqual(pivotIndex([1, 2, 3]), -1)

@Test
fun testZeroPivot():
    assertEqual(pivotIndex([2, 1, -1]), 0)
