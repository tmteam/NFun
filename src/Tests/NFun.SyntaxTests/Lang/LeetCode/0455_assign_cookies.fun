# LeetCode 0455 — Assign Cookies
#
# Sort both children's greed factors and cookie sizes ascending, then sweep:
# for each cookie, satisfy the smallest unsatisfied child that fits.

fun findContentChildren(g, s):
    gs = g.sort()
    ss = s.sort()
    i = 0
    j = 0
    while i < gs.count() and j < ss.count():
        if ss[j] >= gs[i]: i += 1
        j += 1
    return i

@Test
fun testCanonical():
    assertEqual(findContentChildren([1, 2, 3], [1, 1]), 1)

@Test
fun testAllFit():
    assertEqual(findContentChildren([1, 2], [1, 2, 3]), 2)

@Test
fun testNoFit():
    assertEqual(findContentChildren([10, 9, 8], [1, 2, 3]), 0)
