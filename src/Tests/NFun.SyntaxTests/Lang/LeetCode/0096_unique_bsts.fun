# LeetCode 0096 — Unique Binary Search Trees
#
# Number of structurally distinct BSTs storing values 1..n. Catalan number
# DP: G(n) = Σ G(i-1) · G(n-i)  for i = 1..n.

fun numTrees(n):
    g = [1]
    i = 1
    while i <= n:
        total = 0
        j = 0
        while j < i:
            total += g[j] * g[i - 1 - j]
            j += 1
        g = concat(g, [total])
        i += 1
    return g[n]

@Test(0, 1)
@Test(1, 1)
@Test(2, 2)
@Test(3, 5)
@Test(4, 14)
@Test(5, 42)
@Test(8, 1430)
fun testNumTrees(n, expected):
    assertEqual(numTrees(n), expected)
