# LeetCode 0153 — Find Minimum in Rotated Sorted Array
#
# Sorted array rotated unknown times. Binary search comparing mid to hi.

fun findMin(nums):
    lo = 0
    hi = nums.count() - 1
    while lo < hi:
        mid = (lo + hi) // 2
        if nums[mid] > nums[hi]:
            lo = mid + 1
        else:
            hi = mid
    return nums[lo]

@Test
fun testCanonical():
    assertEqual(findMin([3, 4, 5, 1, 2]), 1)

@Test
fun testNoRotation():
    assertEqual(findMin([1, 2, 3, 4, 5]), 1)

@Test
fun testFullyRotated():
    assertEqual(findMin([4, 5, 6, 7, 0, 1, 2]), 0)

@Test
fun testSingle():
    assertEqual(findMin([5]), 5)
