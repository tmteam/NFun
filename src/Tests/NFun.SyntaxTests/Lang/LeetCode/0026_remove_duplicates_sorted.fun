# LeetCode 0026 — Remove Duplicates from Sorted Array
#
# Canonical leetcode wants an in-place rewrite returning the new length;
# without mutable arrays we return the deduplicated array instead — same
# information, cleaner.

fun removeDuplicates(nums):
    if nums.count() == 0: return []
    out = [nums[0]]
    i = 1
    while i < nums.count():
        if nums[i] != nums[i - 1]:
            out = concat(out, [nums[i]])
        i += 1
    return out

@Test
fun testCanonical():
    assertEqual(removeDuplicates([1, 1, 2]), [1, 2])

@Test
fun testWithGaps():
    assertEqual(removeDuplicates([0, 0, 1, 1, 1, 2, 2, 3, 3, 4]), [0, 1, 2, 3, 4])

@Test
fun testNoDuplicates():
    assertEqual(removeDuplicates([1, 2, 3]), [1, 2, 3])

@Test
fun testEmpty():
    assertEqual(removeDuplicates([]), [])
