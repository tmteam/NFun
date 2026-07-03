# LeetCode 0300 — Longest Increasing Subsequence
#
# Patience-sort variant: maintain `tails` where tails[k] is the smallest
# possible tail of any length-(k+1) increasing subsequence seen so far. For
# each x, binary-search for the leftmost tail ≥ x and replace it (or extend
# if x is larger than every tail). LIS length is `tails.count()`. O(n log n).

fun lengthOfLIS(nums):
    tails = []
    for x in nums:
        lo = 0
        hi = tails.count()
        while lo < hi:
            mid = (lo + hi) // 2
            if tails[mid] < x: lo = mid + 1
            else: hi = mid
        if lo == tails.count():
            tails = concat(tails, [x])
        else:
            tails = tails.setAt(lo, x)
    return tails.count()

@Test
fun testCanonical():
    assertEqual(lengthOfLIS([10, 9, 2, 5, 3, 7, 101, 18]), 4)

@Test
fun testMonotone():
    assertEqual(lengthOfLIS([0, 1, 0, 3, 2, 3]), 4)

@Test
fun testAllSame():
    assertEqual(lengthOfLIS([7, 7, 7]), 1)

@Test
fun testSingle():
    assertEqual(lengthOfLIS([42]), 1)
