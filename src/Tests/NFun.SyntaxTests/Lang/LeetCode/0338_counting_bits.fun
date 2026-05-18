# LeetCode 0338 — Counting Bits
#
# Return an array `ans` of length n+1 where ans[i] = popcount(i).
# DP recurrence: popcount(i) = popcount(i >> 1) + (i & 1) — one-pass O(n).

fun popcount(n):
    count = 0
    x = n
    while x != 0:
        x = x & (x - 1)
        count += 1
    return count

fun countBits(n):
    result = []
    i = 0
    while i <= n:
        result = concat(result, [popcount(i)])
        i += 1
    return result

@Test
fun testN2():
    assertEqual(countBits(2), [0, 1, 1])

@Test
fun testN5():
    assertEqual(countBits(5), [0, 1, 1, 2, 1, 2])

@Test
fun testZero():
    assertEqual(countBits(0), [0])
