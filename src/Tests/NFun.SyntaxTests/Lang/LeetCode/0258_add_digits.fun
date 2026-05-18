# LeetCode 0258 — Add Digits
#
# Repeatedly sum a non-negative integer's digits until one digit remains —
# the digital root. Closed form: `1 + (n - 1) % 9` for n > 0.

fun addDigits(num):
    if num == 0: return 0
    return 1 + (num - 1) % 9

@Test(0, 0)
@Test(38, 2)
@Test(9, 9)
@Test(123, 6)
@Test(10, 1)
fun testAddDigits(num, expected):
    assertEqual(addDigits(num), expected)
