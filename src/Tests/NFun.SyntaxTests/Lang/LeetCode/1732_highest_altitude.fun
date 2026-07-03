# LeetCode 1732 — Find the Highest Altitude
#
# Running prefix sum, track max (and the initial 0 altitude).

fun largestAltitude(gain):
    cur = 0
    best = 0
    for g in gain:
        cur += g
        if cur > best: best = cur
    return best

@Test
fun testCanonical():
    assertEqual(largestAltitude([-5, 1, 5, 0, -7]), 1)

@Test
fun testAllDownhill():
    assertEqual(largestAltitude([-4, -3, -2, -1, 4, 3, 2]), 0)

@Test
fun testSingleClimb():
    assertEqual(largestAltitude([3]), 3)
