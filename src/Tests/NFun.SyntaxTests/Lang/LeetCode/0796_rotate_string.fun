# LeetCode 0796 — Rotate String
#
# Goal is a rotation of s iff |s| == |goal| AND goal occurs as a substring
# of s+s.

fun isSubstring(needle, hay):
    n = hay.count()
    m = needle.count()
    if m == 0: return true
    if m > n: return false
    i = 0
    while i <= n - m:
        ok = true
        j = 0
        while j < m and ok:
            if hay[i + j] != needle[j]: ok = false
            j += 1
        if ok: return true
        i += 1
    return false

fun rotateString(s, goal):
    if s.count() != goal.count(): return false
    return isSubstring(goal, concat(s, s))

@Test('abcde', 'cdeab', true)
@Test('abcde', 'abced', false)
@Test('aaaa', 'aaaa', true)
@Test('ab', 'ba', true)
@Test('a', 'b', false)
fun testRotateString(s, goal, expected):
    assertEqual(rotateString(s, goal), expected)
