# LeetCode 0350 — Intersection of Two Arrays II
#
# Multiplicity matters: include each common element as many times as it
# appears in both. Sort + two-pointer.

fun intersectII(nums1, nums2):
    a = nums1.sort()
    b = nums2.sort()
    out = []
    i = 0
    j = 0
    while i < a.count() and j < b.count():
        if a[i] < b[j]: i += 1
        elif a[i] > b[j]: j += 1
        else:
            out = concat(out, [a[i]])
            i += 1
            j += 1
    return out

@Test
fun testCanonical():
    assertEqual(intersectII([1, 2, 2, 1], [2, 2]), [2, 2])

@Test
fun testWithExtras():
    assertEqual(intersectII([4, 9, 5], [9, 4, 9, 8, 4]), [4, 9])

@Test
fun testEmpty():
    assertEqual(intersectII([], [1, 2]), [])
