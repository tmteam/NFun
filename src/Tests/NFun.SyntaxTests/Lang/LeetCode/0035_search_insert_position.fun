# LeetCode 0035 — Search Insert Position
#
# Given a sorted array `nums` and a target value, return the index if the
# target is found. If not, return the index where it would be inserted in
# order. Must be O(log n) — standard binary search.

fun searchInsert(nums, target):
    lo = 0
    hi = nums.count()
    while lo < hi:
        mid = (lo + hi) // 2
        if nums[mid] == target: return mid
        elif nums[mid] < target: lo = mid + 1
        else: hi = mid
    return lo

@Test(5, 2)
@Test(2, 1)
@Test(7, 4)
@Test(0, 0)
fun testSearchInsert(target, expected):
    assertEqual(searchInsert([1, 3, 5, 6], target), expected)

@Test
fun testEmptyArray():
    assertEqual(searchInsert([], 5), 0)

@Test
fun testSingleElement():
    assertEqual(searchInsert([1], 0), 0)
    assertEqual(searchInsert([1], 1), 0)
    assertEqual(searchInsert([1], 2), 1)
