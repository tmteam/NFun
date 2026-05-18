# LeetCode 0674 — Longest Continuous Increasing Subsequence

fun findLengthOfLCIS(nums):
    if nums.count() == 0: return 0
    best = 1
    run = 1
    i = 1
    while i < nums.count():
        if nums[i] > nums[i - 1]:
            run += 1
            if run > best: best = run
        else:
            run = 1
        i += 1
    return best

@Test
fun testCanonical():
    assertEqual(findLengthOfLCIS([1, 3, 5, 4, 7]), 3)

@Test
fun testAllSame():
    assertEqual(findLengthOfLCIS([2, 2, 2, 2]), 1)

@Test
fun testEmpty():
    assertEqual(findLengthOfLCIS([]), 0)
