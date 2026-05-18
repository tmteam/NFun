# LeetCode 0172 — Factorial Trailing Zeroes
#
# Each trailing zero comes from a factor of 10 = 2·5, and factors of 2
# always outnumber 5s — so just count factors of 5 in n!.

fun trailingZeroes(n):
    count = 0
    x = n
    while x > 0:
        x = x // 5
        count += x
    return count

@Test(0, 0)
@Test(3, 0)
@Test(5, 1)
@Test(10, 2)
@Test(25, 6)
@Test(100, 24)
@Test(1000, 249)
fun testTrailingZeroes(n, expected):
    assertEqual(trailingZeroes(n), expected)
