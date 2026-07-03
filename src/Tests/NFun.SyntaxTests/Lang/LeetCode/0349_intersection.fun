# LeetCode 0349 — Intersection of Two Arrays
#
# Distinct elements present in both. Sort + two-pointer keeps it O(n log n)
# without sets.

fun intersection(nums1, nums2):
    a = nums1.sort()
    b = nums2.sort()
    out = []
    i = 0
    j = 0
    while i < a.count() and j < b.count():
        if a[i] < b[j]:
            i += 1
        elif a[i] > b[j]:
            j += 1
        else:
            if out.count() == 0 or out[out.count() - 1] != a[i]:
                out = concat(out, [a[i]])
            i += 1
            j += 1
    return out

@Test
fun testCanonical():
    assertEqual(intersection([1, 2, 2, 1], [2, 2]), [2])

@Test
fun testTwoCommon():
    assertEqual(intersection([4, 9, 5], [9, 4, 9, 8, 4]), [4, 9])

@Test
fun testNoCommon():
    assertEqual(intersection([1, 2, 3], [4, 5, 6]), [])

@Test
fun testEmpty():
    assertEqual(intersection([], [1, 2]), [])
