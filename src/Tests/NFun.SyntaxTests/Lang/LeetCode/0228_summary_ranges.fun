# LeetCode 0228 — Summary Ranges
#
# Given a sorted unique int array, return the smallest list of ranges that
# cover every element. Single-pointer walk; whenever the next value isn't
# previous+1, flush the current range.

fun summaryRanges(nums):
    out = []
    n = nums.count()
    if n == 0: return out
    start = nums[0]
    prev = nums[0]
    i = 1
    while i < n:
        if nums[i] == prev + 1:
            prev = nums[i]
        else:
            chunk = if start == prev: '{start}' else: '{start}->{prev}'
            out = concat(out, [chunk])
            start = nums[i]
            prev = nums[i]
        i += 1
    last = if start == prev: '{start}' else: '{start}->{prev}'
    return concat(out, [last])

@Test
fun testCanonical():
    assertEqual(summaryRanges([0, 1, 2, 4, 5, 7]), ['0->2', '4->5', '7'])

@Test
fun testSingleton():
    assertEqual(summaryRanges([0]), ['0'])

@Test
fun testEmpty():
    assertEqual(summaryRanges([]), [])

@Test
fun testFullRange():
    assertEqual(summaryRanges([1, 2, 3, 4]), ['1->4'])
