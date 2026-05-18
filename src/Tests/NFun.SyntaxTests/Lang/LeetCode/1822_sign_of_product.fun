# LeetCode 1822 — Sign of the Product of an Array
#
# Return 1, -1 or 0 without computing the (potentially huge) product —
# flip a sign for each negative; short-circuit on zero.

fun arraySign(nums):
    sign = 1
    for x in nums:
        if x == 0: return 0
        if x < 0: sign = -sign
    return sign

@Test
fun testPositive():
    assertEqual(arraySign([-1, -2, -3, -4, 3, 2, 1]), 1)

@Test
fun testZero():
    assertEqual(arraySign([1, 5, 0, 2, -3]), 0)

@Test
fun testNegative():
    assertEqual(arraySign([-1, 1, -1, 1, -1]), -1)
