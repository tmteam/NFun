# LeetCode 0168 — Excel Sheet Column Title
#
# Inverse of #0171. Bijective base-26: subtract 1 before each step so digits
# land in 0..25 (then map to A..Z).

fun digitToLetter(d):
    return 'ABCDEFGHIJKLMNOPQRSTUVWXYZ'[d]

fun convertToTitle(columnNumber):
    n = columnNumber
    out = ''
    while n > 0:
        n -= 1
        out = concat('{digitToLetter(n % 26)}', out)
        n = n // 26
    return out

@Test(1, 'A')
@Test(28, 'AB')
@Test(701, 'ZY')
@Test(26, 'Z')
@Test(27, 'AA')
@Test(2147483647, 'FXSHRXW')
fun testConvertToTitle(num, expected):
    assertEqual(convertToTitle(num), expected)
