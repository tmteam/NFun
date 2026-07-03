# LeetCode 0217 — Contains Duplicate
#
# Return true iff any value appears at least twice. Without a hash set, sort
# and check adjacent equality — O(n log n).

fun containsDuplicate(nums):
    sorted = nums.sort()
    i = 1
    while i < sorted.count():
        if sorted[i] == sorted[i - 1]: return true
        i += 1
    return false

@Test
fun testHasDup():
    assertEqual(containsDuplicate([1, 2, 3, 1]), true)

@Test
fun testUnique():
    assertEqual(containsDuplicate([1, 2, 3, 4]), false)

@Test
fun testEmpty():
    assertEqual(containsDuplicate([]), false)

@Test
fun testManyDups():
    assertEqual(containsDuplicate([1, 1, 1, 3, 3, 4, 3, 2, 4, 2]), true)
