# LeetCode 0788 — Rotated Digits
#
# Good numbers contain at least one of {2, 5, 6, 9} and no {3, 4, 7}.

fun isGood(n):
    x = n
    hasChange = false
    while x > 0:
        d = x % 10
        if d == 3 or d == 4 or d == 7: return false
        if d == 2 or d == 5 or d == 6 or d == 9: hasChange = true
        x = x // 10
    return hasChange

fun rotatedDigits(n):
    count = 0
    i = 1
    while i <= n:
        if isGood(i): count += 1
        i += 1
    return count

@Test(10, 4)
@Test(20, 9)
@Test(100, 40)
@Test(1, 0)
fun testRotatedDigits(n, expected):
    assertEqual(rotatedDigits(n), expected)
