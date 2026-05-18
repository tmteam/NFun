# LeetCode 0647 — Palindromic Substrings
#
# Count distinct palindromic substrings (positions count separately).
# Expand-around-center for each odd and even center → 2n-1 centers, O(n²).

fun expandCount(s, lo, hi):
    count = 0
    while lo >= 0 and hi < s.count() and s[lo] == s[hi]:
        count += 1
        lo -= 1
        hi += 1
    return count

fun countSubstrings(s):
    total = 0
    i = 0
    while i < s.count():
        total += expandCount(s, i, i)
        total += expandCount(s, i, i + 1)
        i += 1
    return total

@Test('abc', 3)
@Test('aaa', 6)
@Test('aaaa', 10)
@Test('', 0)
@Test('a', 1)
fun testCountSubstrings(s, expected):
    assertEqual(countSubstrings(s), expected)
