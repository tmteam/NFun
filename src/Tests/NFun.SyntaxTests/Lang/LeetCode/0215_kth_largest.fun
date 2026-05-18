# LeetCode 0215 — Kth Largest Element in an Array
#
# Canonical leetcode wants a heap or quickselect; sort is O(n log n) and
# crisp here.

fun findKthLargest(nums, k):
    s = nums.sort()
    return s[s.count() - k]

@Test
fun testCanonical():
    assertEqual(findKthLargest([3, 2, 1, 5, 6, 4], 2), 5)

@Test
fun testWithDuplicates():
    assertEqual(findKthLargest([3, 2, 3, 1, 2, 4, 5, 5, 6], 4), 4)

@Test
fun testSingle():
    assertEqual(findKthLargest([1], 1), 1)
