# LeetCode 0189 — Rotate Array
#
# Rotate right by k. Leetcode wants in-place; we return a fresh array using
# concat of the last k and first (n-k) elements.

fun rotate(nums, k):
    n = nums.count()
    if n == 0: return []
    r = k % n
    return concat(nums.skip(n - r), nums.take(n - r))

@Test
fun testCanonical():
    assertEqual(rotate([1, 2, 3, 4, 5, 6, 7], 3), [5, 6, 7, 1, 2, 3, 4])

@Test
fun testKEqualsLen():
    assertEqual(rotate([1, 2, 3], 3), [1, 2, 3])

@Test
fun testKZero():
    assertEqual(rotate([1, 2, 3], 0), [1, 2, 3])

@Test
fun testKLarger():
    assertEqual(rotate([-1, -100, 3, 99], 2), [3, 99, -1, -100])
