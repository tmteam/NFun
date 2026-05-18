# LeetCode 0162 — Find Peak Element
#
# Any element strictly greater than both neighbours is a peak. With virtual
# `-∞` at the boundaries, binary search converges to a peak in O(log n):
# if the right neighbour is bigger, a peak lies on the right; else on the
# left (including current).

fun findPeakElement(nums):
    lo = 0
    hi = nums.count() - 1
    while lo < hi:
        mid = (lo + hi) // 2
        if nums[mid] < nums[mid + 1]:
            lo = mid + 1
        else:
            hi = mid
    return lo

@Test
fun testCanonical():
    # [1, 2, 3, 1] — peak is index 2 (value 3)
    assertEqual(findPeakElement([1, 2, 3, 1]), 2)

@Test
fun testMonotonicIncreasing():
    assertEqual(findPeakElement([1, 2, 3, 4, 5]), 4)

@Test
fun testMonotonicDecreasing():
    assertEqual(findPeakElement([5, 4, 3, 2, 1]), 0)

@Test
fun testSingle():
    assertEqual(findPeakElement([42]), 0)
