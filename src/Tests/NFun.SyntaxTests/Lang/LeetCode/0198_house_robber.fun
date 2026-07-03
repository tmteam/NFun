# LeetCode 0198 — House Robber
#
# Rob non-adjacent houses to maximise total. Classic DP recurrence:
#   take[i] = skip[i-1] + nums[i]
#   skip[i] = max(take[i-1], skip[i-1])
# Two rolling locals — O(n) time, O(1) space.

fun rob(nums):
    take = 0
    skip = 0
    for x in nums:
        newTake = skip + x
        newSkip = max(take, skip)
        take = newTake
        skip = newSkip
    return max(take, skip)

@Test
fun testCanonical():
    assertEqual(rob([1, 2, 3, 1]), 4)

@Test
fun testSecondCase():
    assertEqual(rob([2, 7, 9, 3, 1]), 12)

@Test
fun testSingle():
    assertEqual(rob([5]), 5)

@Test
fun testTwo():
    assertEqual(rob([1, 100]), 100)
