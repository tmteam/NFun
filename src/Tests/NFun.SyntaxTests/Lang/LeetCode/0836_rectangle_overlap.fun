# LeetCode 0836 — Rectangle Overlap
#
# Two rectangles overlap iff neither is fully to one side / above the other.

fun isRectangleOverlap(rec1, rec2):
    return rec1[0] < rec2[2] and rec1[2] > rec2[0]
       and rec1[1] < rec2[3] and rec1[3] > rec2[1]

@Test
fun testOverlap():
    assertEqual(isRectangleOverlap([0, 0, 2, 2], [1, 1, 3, 3]), true)

@Test
fun testTouchEdge():
    assertEqual(isRectangleOverlap([0, 0, 1, 1], [1, 0, 2, 1]), false)

@Test
fun testTouchCorner():
    assertEqual(isRectangleOverlap([0, 0, 1, 1], [2, 2, 3, 3]), false)

@Test
fun testInside():
    assertEqual(isRectangleOverlap([-5, -5, 5, 5], [-1, -1, 1, 1]), true)
