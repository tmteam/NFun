# LeetCode 1652 — Defuse the Bomb
#
# For k > 0, code[i] = sum of next k elements (circular). For k < 0, sum of
# previous |k|. k == 0 → all zeros.

fun decrypt(code, k):
    n = code.count()
    if k == 0: return code.map(rule 0)
    out = []
    i = 0
    while i < n:
        s = 0
        if k > 0:
            j = 1
            while j <= k:
                s += code[(i + j) % n]
                j += 1
        else:
            j = 1
            while j <= -k:
                s += code[((i - j) % n + n) % n]
                j += 1
        out = concat(out, [s])
        i += 1
    return out

@Test
fun testKPos():
    assertEqual(decrypt([5, 7, 1, 4], 3), [12, 10, 16, 13])

@Test
fun testKZero():
    assertEqual(decrypt([1, 2, 3, 4], 0), [0, 0, 0, 0])

@Test
fun testKNeg():
    assertEqual(decrypt([2, 4, 9, 3], -2), [12, 5, 6, 13])
