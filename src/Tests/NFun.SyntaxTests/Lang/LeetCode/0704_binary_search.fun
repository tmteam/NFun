# LeetCode 0704 — Binary Search
#
# Plain binary search on a sorted array. Return index or -1.

fun binarySearch(nums, target):
    lo = 0
    hi = nums.count() - 1
    while lo <= hi:
        mid = (lo + hi) // 2
        if nums[mid] == target: return mid
        elif nums[mid] < target: lo = mid + 1
        else: hi = mid - 1
    return -1

@Test(9, 4)
@Test(2, -1)
@Test(-1, 0)
@Test(12, 5)
fun testSearch(target, expected):
    assertEqual(binarySearch([-1, 0, 3, 5, 9, 12], target), expected)

@Test
fun testEmpty():
    assertEqual(binarySearch([], 1), -1)

@Test
fun testSingle():
    assertEqual(binarySearch([5], 5), 0)
    assertEqual(binarySearch([5], 1), -1)
