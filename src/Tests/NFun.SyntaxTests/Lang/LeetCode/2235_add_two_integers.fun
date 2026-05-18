# LeetCode 2235 — Add Two Integers (one of the simplest possible)

fun sumLC(num1, num2):
    return num1 + num2

@Test(12, 5, 17)
@Test(-10, 4, -6)
@Test(0, 0, 0)
fun testSum(a, b, expected):
    assertEqual(sumLC(a, b), expected)
