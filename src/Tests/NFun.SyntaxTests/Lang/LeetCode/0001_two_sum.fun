# LeetCode 0001 — Two Sum
#
# Given an array of integers `nums` and an integer `target`, return the indices
# of the two numbers that add up to `target`. Each input has exactly one
# solution; you may not use the same element twice.
#
# nfun-lang note: leetcode's canonical O(n) solution uses a hash map. Without
# mutable maps, fall back to the O(n²) brute force — clear and correct.

fun twoSum(nums, target):
    n = nums.count()
    i = 0
    while i < n:
        j = i + 1
        while j < n:
            if nums[i] + nums[j] == target: return [i, j]
            j += 1
        i += 1
    return [-1, -1]

@Test
fun testCanonical():
    assertEqual(twoSum([2, 7, 11, 15], 9), [0, 1])

@Test
fun testMiddlePair():
    assertEqual(twoSum([3, 2, 4], 6), [1, 2])

@Test
fun testDuplicates():
    assertEqual(twoSum([3, 3], 6), [0, 1])

@Test
fun testNegatives():
    assertEqual(twoSum([-1, -2, -3, -4, -5], -8), [2, 4])
