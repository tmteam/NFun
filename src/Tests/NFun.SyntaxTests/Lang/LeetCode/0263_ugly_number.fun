# LeetCode 0263 — Ugly Number
#
# An ugly number has 2, 3 and 5 as its only prime factors. Repeatedly divide
# out those factors; if we land on 1, n is ugly.

fun isUgly(n):
    if n <= 0: return false
    x = n
    while x % 2 == 0:
        x = x // 2
    while x % 3 == 0:
        x = x // 3
    while x % 5 == 0:
        x = x // 5
    return x == 1

@Test(6, true)
@Test(1, true)
@Test(14, false)
@Test(8, true)
@Test(30, true)
@Test(7, false)
@Test(0, false)
@Test(-6, false)
fun testIsUgly(n, expected):
    assertEqual(isUgly(n), expected)
