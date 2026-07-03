# LeetCode 0011 — Container With Most Water
#
# Two pointers from the ends inward — at each step the limiting wall is the
# shorter one, so move it inward (any move of the taller wall can only
# shrink the result).

fun maxArea(heights):
    lo = 0
    hi = heights.count() - 1
    best = 0
    while lo < hi:
        h = min(heights[lo], heights[hi])
        area = h * (hi - lo)
        if area > best: best = area
        if heights[lo] < heights[hi]:
            lo += 1
        else:
            hi -= 1
    return best

@Test
fun testCanonical():
    assertEqual(maxArea([1, 8, 6, 2, 5, 4, 8, 3, 7]), 49)

@Test
fun testTwoBars():
    assertEqual(maxArea([1, 1]), 1)

@Test
fun testFlat():
    assertEqual(maxArea([4, 4, 4, 4]), 12)
