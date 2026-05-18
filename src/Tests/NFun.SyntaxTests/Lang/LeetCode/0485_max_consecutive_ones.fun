# LeetCode 0485 — Max Consecutive Ones
#
# Length of the longest contiguous run of 1s.

fun findMaxConsecutiveOnes(nums):
    best = 0
    run = 0
    for x in nums:
        if x == 1:
            run += 1
            if run > best: best = run
        else:
            run = 0
    return best

@Test
fun testCanonical():
    assertEqual(findMaxConsecutiveOnes([1, 1, 0, 1, 1, 1]), 3)

@Test
fun testMixed():
    assertEqual(findMaxConsecutiveOnes([1, 0, 1, 1, 0, 1]), 2)

@Test
fun testAllZero():
    assertEqual(findMaxConsecutiveOnes([0, 0, 0]), 0)

@Test
fun testAllOne():
    assertEqual(findMaxConsecutiveOnes([1, 1, 1, 1]), 4)
