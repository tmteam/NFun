# LeetCode 0868 — Binary Gap
#
# Longest distance between two consecutive set bits in n's binary form, or 0
# if fewer than two bits are set.

fun binaryGap(n):
    last = -1
    best = 0
    pos = 0
    x = n
    while x > 0:
        if (x & 1) == 1:
            if last >= 0:
                d = pos - last
                if d > best: best = d
            last = pos
        x = x >> 1
        pos += 1
    return best

@Test(22, 2)
@Test(8, 0)
@Test(5, 2)
@Test(6, 1)
@Test(1, 0)
fun testBinaryGap(n, expected):
    assertEqual(binaryGap(n), expected)
