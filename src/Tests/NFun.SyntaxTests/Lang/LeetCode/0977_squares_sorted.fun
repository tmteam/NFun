# LeetCode 0977 — Squares of a Sorted Array
#
# `nums` is sorted but may contain negatives. The squares' largest values
# come from the ends — two-pointer merge into a result built back-to-front.

fun sortedSquares(nums):
    n = nums.count()
    lo = 0
    hi = n - 1
    out = []
    while lo <= hi:
        a = nums[lo] * nums[lo]
        b = nums[hi] * nums[hi]
        if a > b:
            out = concat([a], out)
            lo += 1
        else:
            out = concat([b], out)
            hi -= 1
    return out

@Test
fun testCanonical():
    assertEqual(sortedSquares([-4, -1, 0, 3, 10]), [0, 1, 9, 16, 100])

@Test
fun testAllPositive():
    assertEqual(sortedSquares([1, 2, 3]), [1, 4, 9])

@Test
fun testAllNegative():
    assertEqual(sortedSquares([-7, -3, -1]), [1, 9, 49])
