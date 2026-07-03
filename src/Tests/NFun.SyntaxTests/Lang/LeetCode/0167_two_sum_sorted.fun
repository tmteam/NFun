# LeetCode 0167 — Two Sum II - Input Array Is Sorted
#
# Two pointers from the ends. Return 1-indexed positions per leetcode spec.

fun twoSumSorted(numbers, target):
    lo = 0
    hi = numbers.count() - 1
    while lo < hi:
        s = numbers[lo] + numbers[hi]
        if s == target: return [lo + 1, hi + 1]
        elif s < target: lo += 1
        else: hi -= 1
    return [-1, -1]

@Test
fun testCanonical():
    assertEqual(twoSumSorted([2, 7, 11, 15], 9), [1, 2])

@Test
fun testNegatives():
    assertEqual(twoSumSorted([-1, 0], -1), [1, 2])

@Test
fun testFarEnds():
    assertEqual(twoSumSorted([1, 3, 4, 5, 7, 10, 11], 9), [3, 4])
