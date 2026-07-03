# LeetCode 0989 — Add to Array-Form of Integer
#
# `num` is given as a digit array; add the integer `k` to it. Walk
# right-to-left mixing num's digit and k's residual; trail with any final
# carry digits.

fun addToArrayForm(num, k):
    out = []
    i = num.count() - 1
    carry = k
    while i >= 0 or carry > 0:
        d = if i >= 0: num[i] else: 0
        total = d + carry
        out = concat([total % 10], out)
        carry = total // 10
        i -= 1
    return out

@Test
fun testCanonical():
    assertEqual(addToArrayForm([1, 2, 0, 0], 34), [1, 2, 3, 4])

@Test
fun testCarryGrow():
    assertEqual(addToArrayForm([2, 7, 4], 181), [4, 5, 5])

@Test
fun testLargeK():
    assertEqual(addToArrayForm([1, 0, 0], 1000), [1, 1, 0, 0])
