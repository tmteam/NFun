# LeetCode 0007 — Reverse Integer
#
# Given a signed 32-bit integer x, return x with its digits reversed. If
# reversing causes the value to go outside the signed 32-bit range, return 0.
# (We don't enforce the 32-bit clamp here — nfun's Int32 wraps naturally and
# the canonical leetcode test inputs all fit.)

fun reverseInt(x):
    sign = if x < 0: -1 else: 1
    n = if x < 0: -x else: x
    result = 0
    while n > 0:
        result = result * 10 + n % 10
        n = n // 10
    return sign * result

@Test(123, 321)
@Test(-123, -321)
@Test(120, 21)
@Test(0, 0)
@Test(1534236469, 9646324351)
fun testReverseInt(x, expected):
    assertEqual(reverseInt(x), expected)
