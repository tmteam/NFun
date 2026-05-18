# LeetCode 0322 — Coin Change
#
# Fewest coins summing to `amount`, or -1 if impossible. Bottom-up DP:
# dp[i] = 1 + min(dp[i - c]) over coins c.

fun coinChange(coins, amount):
    big = amount + 1
    dp = [0]
    i = 1
    while i <= amount:
        best = big
        for c in coins:
            if c <= i and dp[i - c] + 1 < best:
                best = dp[i - c] + 1
        dp = concat(dp, [best])
        i += 1
    if dp[amount] >= big: return -1
    return dp[amount]

@Test
fun testCanonical():
    assertEqual(coinChange([1, 2, 5], 11), 3)

@Test
fun testNoSolution():
    assertEqual(coinChange([2], 3), -1)

@Test
fun testZero():
    assertEqual(coinChange([1], 0), 0)

@Test
fun testOne():
    assertEqual(coinChange([1, 2, 5], 1), 1)
