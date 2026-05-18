# LeetCode 1009 — Complement of Base 10 Integer
#
# Same trick as #0476: build a mask of all 1-bits the same width as n, XOR.
# Special case n == 0 → 1.

fun bitwiseComplement(n):
    if n == 0: return 1
    mask = 0
    x = n
    while x > 0:
        mask = (mask << 1) | 1
        x = x >> 1
    return n ^ mask

@Test(5, 2)
@Test(7, 0)
@Test(10, 5)
@Test(0, 1)
@Test(1, 0)
fun testComplement(n, expected):
    assertEqual(bitwiseComplement(n), expected)
