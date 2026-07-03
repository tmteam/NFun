# LeetCode 0279 — Perfect Squares
#
# Least number of perfect squares summing to n. DP: dp[i] = 1 + min over all
# squares s²≤i of dp[i - s²].

fun numSquares(n):
    dp = [0]
    i = 1
    while i <= n:
        best = i  # worst case: 1+1+...+1
        s = 1
        while s * s <= i:
            cand = dp[i - s * s] + 1
            if cand < best: best = cand
            s += 1
        dp = concat(dp, [best])
        i += 1
    return dp[n]

@Test(1, 1)
@Test(4, 1)
@Test(12, 3)
@Test(13, 2)
@Test(7, 4)
@Test(100, 1)
fun testNumSquares(n, expected):
    assertEqual(numSquares(n), expected)
