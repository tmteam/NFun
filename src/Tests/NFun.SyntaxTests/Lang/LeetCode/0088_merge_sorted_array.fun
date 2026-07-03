# LeetCode 0088 — Merge Sorted Array
#
# Merge nums1 (first m elements) and nums2 (first n elements) into a single
# sorted output. The leetcode version writes back into nums1; we return a
# fresh array instead.

fun mergeSorted(nums1, m, nums2, n):
    out = []
    i = 0
    j = 0
    while i < m and j < n:
        if nums1[i] <= nums2[j]:
            out = concat(out, [nums1[i]])
            i += 1
        else:
            out = concat(out, [nums2[j]])
            j += 1
    while i < m:
        out = concat(out, [nums1[i]])
        i += 1
    while j < n:
        out = concat(out, [nums2[j]])
        j += 1
    return out

@Test
fun testCanonical():
    assertEqual(mergeSorted([1, 2, 3, 0, 0, 0], 3, [2, 5, 6], 3), [1, 2, 2, 3, 5, 6])

@Test
fun testOneEmpty():
    assertEqual(mergeSorted([1, 2, 3], 3, [], 0), [1, 2, 3])
    assertEqual(mergeSorted([0, 0, 0], 0, [1, 2, 3], 3), [1, 2, 3])

@Test
fun testInterleaved():
    assertEqual(mergeSorted([1, 3, 5, 0, 0, 0], 3, [2, 4, 6], 3), [1, 2, 3, 4, 5, 6])
