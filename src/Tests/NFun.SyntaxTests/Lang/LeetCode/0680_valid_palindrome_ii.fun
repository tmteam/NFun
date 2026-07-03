# LeetCode 0680 — Valid Palindrome II
#
# At most one deletion allowed. Two pointers; on the first mismatch, try
# skipping either side and check the substring is a palindrome.

fun isRange(s, lo, hi):
    while lo < hi:
        if s[lo] != s[hi]: return false
        lo += 1
        hi -= 1
    return true

fun validPalindrome(s):
    lo = 0
    hi = s.count() - 1
    while lo < hi:
        if s[lo] != s[hi]:
            return isRange(s, lo + 1, hi) or isRange(s, lo, hi - 1)
        lo += 1
        hi -= 1
    return true

@Test('aba', true)
@Test('abca', true)
@Test('abc', false)
@Test('deeee', true)
@Test('eddee', true)
@Test('', true)
fun testValidPalindrome(s, expected):
    assertEqual(validPalindrome(s), expected)
