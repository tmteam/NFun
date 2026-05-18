# LeetCode 0405 — Convert a Number to Hexadecimal
#
# Non-negative range only — NFun lacks an unsigned right shift, so the
# negative-input case (which leetcode covers via two's-complement) isn't
# expressible here.

fun digitChar(d):
    if d < 10: return '0123456789'[d]
    return 'abcdef'[d - 10]

fun toHex(num):
    if num == 0: return '0'
    n = num
    out = ''
    while n > 0:
        out = concat('{digitChar(n & 15)}', out)
        n = n >> 4
    return out

@Test(26, '1a')
@Test(0, '0')
@Test(255, 'ff')
@Test(4095, 'fff')
@Test(2147483647, '7fffffff')
fun testToHex(num, expected):
    assertEqual(toHex(num), expected)
