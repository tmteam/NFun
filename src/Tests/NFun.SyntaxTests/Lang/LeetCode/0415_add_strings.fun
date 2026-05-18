# LeetCode 0415 — Add Strings
#
# Add two non-negative integers given as decimal strings. Schoolbook
# right-to-left with carry.

fun digitOf(c):
    digits = '0123456789'
    i = 0
    while i < 10:
        if digits[i] == c: return i
        i += 1
    return -1

fun digitChar(d):
    return '0123456789'[d]

fun addStrings(num1, num2):
    i = num1.count() - 1
    j = num2.count() - 1
    carry = 0
    out = ''
    while i >= 0 or j >= 0 or carry > 0:
        a = if i >= 0: digitOf(num1[i]) else: 0
        b = if j >= 0: digitOf(num2[j]) else: 0
        s = a + b + carry
        out = concat('{digitChar(s % 10)}', out)
        carry = s // 10
        i -= 1
        j -= 1
    return out

@Test('11', '123', '134')
@Test('456', '77', '533')
@Test('0', '0', '0')
@Test('9999', '1', '10000')
fun testAddStrings(a, b, expected):
    assertEqual(addStrings(a, b), expected)
