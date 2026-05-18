# LeetCode 0075 — Sort Colors
#
# Dutch national flag. Leetcode wants O(n) in-place; without mutable arrays
# we sort. The result matches what the in-place algorithm produces.

fun sortColors(nums):
    return nums.sort()

@Test
fun testCanonical():
    assertEqual(sortColors([2, 0, 2, 1, 1, 0]), [0, 0, 1, 1, 2, 2])

@Test
fun testTwoElements():
    assertEqual(sortColors([2, 0, 1]), [0, 1, 2])

@Test
fun testAlreadySorted():
    assertEqual(sortColors([0, 0, 1, 1, 2, 2]), [0, 0, 1, 1, 2, 2])
