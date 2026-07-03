# LeetCode 1431 — Kids With the Greatest Number of Candies
#
# For each kid, can they reach the current max if given `extraCandies` more?

fun kidsWithCandies(candies, extra):
    m = candies.max()
    return candies.map(rule it + extra >= m)

@Test
fun testCanonical():
    assertEqual(kidsWithCandies([2, 3, 5, 1, 3], 3), [true, true, true, false, true])

@Test
fun testTight():
    assertEqual(kidsWithCandies([4, 2, 1, 1, 2], 1), [true, false, false, false, false])

@Test
fun testAllEqual():
    assertEqual(kidsWithCandies([12, 1, 12], 10), [true, false, true])
