# LeetCode 1480 — Running Sum of 1d Array

fun runningSum(nums):
    out = []
    total = 0
    for x in nums:
        total += x
        out = concat(out, [total])
    return out

@Test
fun testCanonical():
    assertEqual(runningSum([1, 2, 3, 4]), [1, 3, 6, 10])

@Test
fun testNegatives():
    assertEqual(runningSum([1, -1, 1, -1]), [1, 0, 1, 0])

@Test
fun testEmpty():
    assertEqual(runningSum([]), [])
