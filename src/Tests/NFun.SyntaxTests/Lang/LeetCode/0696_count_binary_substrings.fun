# LeetCode 0696 — Count Binary Substrings
#
# Substrings with equal counts of 0s and 1s where all 0s and 1s are grouped
# consecutively. Track lengths of consecutive runs; each adjacent pair of
# runs contributes min(prev, current) substrings.

fun countBinarySubstrings(s):
    prev = 0
    cur = 1
    total = 0
    i = 1
    while i < s.count():
        if s[i] == s[i - 1]:
            cur += 1
        else:
            total += min(prev, cur)
            prev = cur
            cur = 1
        i += 1
    total += min(prev, cur)
    return total

@Test('00110011', 6)
@Test('10101', 4)
@Test('0', 0)
@Test('00110', 3)
fun testCountBinary(s, expected):
    assertEqual(countBinarySubstrings(s), expected)
