# LeetCode 0643 — Maximum Average Subarray I
#
# Length-k window — sliding sum, divide once at the end. Return the average
# as a real.

fun findMaxAverage(nums, k):
    windowSum = 0
    i = 0
    while i < k:
        windowSum += nums[i]
        i += 1
    best = windowSum
    while i < nums.count():
        windowSum += nums[i] - nums[i - k]
        if windowSum > best: best = windowSum
        i += 1
    return (best * 1.0) / (k * 1.0)

@Test
fun testCanonical():
    assertEqual(findMaxAverage([1, 12, -5, -6, 50, 3], 4), 12.75)

@Test
fun testAllSame():
    assertEqual(findMaxAverage([5], 1), 5.0)
