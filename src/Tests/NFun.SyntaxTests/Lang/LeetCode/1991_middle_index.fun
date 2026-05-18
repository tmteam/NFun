# LeetCode 1991 — Find the Middle Index in Array
#
# Same as #0724 (Find Pivot Index) — kept for completeness, included via
# an alternative left-sum walk.

fun findMiddleIndex(nums):
    total = nums.sum()
    left = 0
    i = 0
    while i < nums.count():
        if 2 * left + nums[i] == total: return i
        left += nums[i]
        i += 1
    return -1

@Test
fun testCanonical():
    assertEqual(findMiddleIndex([2, 3, -1, 8, 4]), 3)

@Test
fun testEarly():
    assertEqual(findMiddleIndex([1, -1, 4]), 2)

@Test
fun testNone():
    assertEqual(findMiddleIndex([2, 5]), -1)
