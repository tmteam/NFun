# LeetCode 0812 — Largest Triangle Area
#
# Maximise area over all triples of points. Shoelace formula gives area =
# |x1(y2-y3) + x2(y3-y1) + x3(y1-y2)| / 2.

fun triArea(p1, p2, p3):
    a = p1[0] * (p2[1] - p3[1]) + p2[0] * (p3[1] - p1[1]) + p3[0] * (p1[1] - p2[1])
    if a < 0: a = -a
    return (a * 1.0) / 2.0

fun largestTriangleArea(points):
    n = points.count()
    best = 0.0
    i = 0
    while i < n:
        j = i + 1
        while j < n:
            k = j + 1
            while k < n:
                area = triArea(points[i], points[j], points[k])
                if area > best: best = area
                k += 1
            j += 1
        i += 1
    return best

@Test
fun testCanonical():
    pts = [[0, 0], [0, 1], [1, 0], [0, 2], [2, 0]]
    assertEqual(largestTriangleArea(pts), 2.0)

@Test
fun testThreePoints():
    assertEqual(largestTriangleArea([[1, 0], [0, 0], [0, 1]]), 0.5)
