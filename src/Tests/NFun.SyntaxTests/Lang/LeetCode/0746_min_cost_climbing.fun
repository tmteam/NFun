# LeetCode 0746 — Min Cost Climbing Stairs
#
# From step i either jump to i+1 or i+2 (paying cost[i]). Reach past the top.
# DP recurrence with two rolling locals.

fun minCostClimbing(cost):
    a = 0
    b = 0
    for c in cost:
        nextVal = c + min(a, b)
        a = b
        b = nextVal
    return min(a, b)

@Test
fun testCanonical():
    assertEqual(minCostClimbing([10, 15, 20]), 15)

@Test
fun testLonger():
    assertEqual(minCostClimbing([1, 100, 1, 1, 1, 100, 1, 1, 100, 1]), 6)

@Test
fun testTwo():
    assertEqual(minCostClimbing([10, 15]), 10)
