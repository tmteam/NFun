# LeetCode 0066 — Plus One
#
# Given a non-empty array of digits representing a non-negative integer,
# increment by one and return the resulting digit array. Carry propagates
# right-to-left; if it survives to the front, prepend a 1.

fun plusOne(digits):
    n = digits.count()
    out = digits
    carry = 1
    i = n - 1
    while i >= 0 and carry > 0:
        s = out[i] + carry
        out = out.setAt(i, s % 10)
        carry = s // 10
        i -= 1
    if carry > 0: return concat([1], out)
    return out

@Test
fun testNormal():
    assertEqual(plusOne([1, 2, 3]), [1, 2, 4])

@Test
fun testCarryEnd():
    assertEqual(plusOne([1, 2, 9]), [1, 3, 0])

@Test
fun testCarryThrough():
    assertEqual(plusOne([9, 9, 9]), [1, 0, 0, 0])

@Test
fun testSingleNine():
    assertEqual(plusOne([9]), [1, 0])

@Test
fun testZero():
    assertEqual(plusOne([0]), [1])
