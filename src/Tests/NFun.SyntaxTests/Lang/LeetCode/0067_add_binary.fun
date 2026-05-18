# LeetCode 0067 — Add Binary
#
# Add two binary strings, return the binary sum. Walk from the right with a
# carry; build the result back-to-front.

fun charToDigit(c):
    if c == /'1': return 1
    return 0

fun digitToChar(d):
    if d == 1: return '1'
    return '0'

fun addBinary(a, b):
    i = a.count() - 1
    j = b.count() - 1
    carry = 0
    out = ''
    while i >= 0 or j >= 0 or carry > 0:
        x = if i >= 0: charToDigit(a[i]) else: 0
        y = if j >= 0: charToDigit(b[j]) else: 0
        s = x + y + carry
        out = concat(digitToChar(s % 2), out)
        carry = s // 2
        i -= 1
        j -= 1
    return out

@Test('11', '1', '100')
@Test('1010', '1011', '10101')
@Test('0', '0', '0')
@Test('1', '111', '1000')
fun testAddBinary(a, b, expected):
    assertEqual(addBinary(a, b), expected)
