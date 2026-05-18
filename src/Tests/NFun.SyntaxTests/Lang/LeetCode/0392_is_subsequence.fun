# LeetCode 0392 — Is Subsequence
#
# Does `s` appear as a (non-contiguous) subsequence inside `t`? Two-pointer
# scan: advance both on match, only `t` on mismatch.

fun isSubsequence(s, t):
    i = 0
    j = 0
    while i < s.count() and j < t.count():
        if s[i] == t[j]: i += 1
        j += 1
    return i == s.count()

@Test
fun testCanonical():
    assertEqual(isSubsequence('abc', 'ahbgdc'), true)

@Test
fun testNotSubseq():
    assertEqual(isSubsequence('axc', 'ahbgdc'), false)

@Test
fun testEmptyNeedle():
    assertEqual(isSubsequence('', 'anything'), true)

@Test
fun testEmptyBoth():
    assertEqual(isSubsequence('', ''), true)
