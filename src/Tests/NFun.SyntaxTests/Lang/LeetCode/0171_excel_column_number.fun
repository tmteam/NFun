# LeetCode 0171 — Excel Sheet Column Number
#
# Convert a column title (`A`, `B`, …, `Z`, `AA`, `AB`, …) to its 1-based
# column number. Bijective base-26: each char contributes (char - 'A' + 1).
# Chars in NFun don't subtract directly to ints — compare to a precomputed
# ordinal table instead.

fun charOrd(c):
    alphabet = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ'
    i = 0
    while i < 26:
        if alphabet[i] == c: return i + 1
        i += 1
    return 0

fun titleToNumber(columnTitle):
    result = 0
    i = 0
    while i < columnTitle.count():
        result = result * 26 + charOrd(columnTitle[i])
        i += 1
    return result

@Test('A', 1)
@Test('B', 2)
@Test('Z', 26)
@Test('AA', 27)
@Test('AB', 28)
@Test('ZY', 701)
@Test('FXSHRXW', 2147483647)
fun testTitleToNumber(title, expected):
    assertEqual(titleToNumber(title), expected)
