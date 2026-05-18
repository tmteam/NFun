# LeetCode 1672 — Richest Customer Wealth
#
# Each row is an account list — return the maximum row sum.

fun maximumWealth(accounts):
    best = 0
    for row in accounts:
        total = row.sum()
        if total > best: best = total
    return best

@Test
fun testCanonical():
    assertEqual(maximumWealth([[1, 2, 3], [3, 2, 1]]), 6)

@Test
fun testSecondCase():
    assertEqual(maximumWealth([[1, 5], [7, 3], [3, 5]]), 10)

@Test
fun testThird():
    assertEqual(maximumWealth([[2, 8, 7], [7, 1, 3], [1, 9, 5]]), 17)
