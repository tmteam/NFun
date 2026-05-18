# LeetCode 0728 — Self Dividing Numbers
#
# A self-dividing number is divisible by each of its non-zero digits.

fun isSelfDividing(n):
    x = n
    while x > 0:
        d = x % 10
        if d == 0 or n % d != 0: return false
        x = x // 10
    return true

fun selfDividingNumbers(left, right):
    out = []
    i = left
    while i <= right:
        if isSelfDividing(i): out = concat(out, [i])
        i += 1
    return out

@Test
fun testCanonical():
    assertEqual(selfDividingNumbers(1, 22), [1, 2, 3, 4, 5, 6, 7, 8, 9, 11, 12, 15, 22])

@Test
fun testNarrow():
    assertEqual(selfDividingNumbers(47, 85), [48, 55, 66, 77])
