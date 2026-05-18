# LeetCode 0014 — Longest Common Prefix
#
# Vertical scan — compare ith char across all strings until a misok.

fun longestCommonPrefix(strs):
    if strs.count() == 0: return ''
    out = ''
    i = 0
    while i < strs[0].count():
        c = strs[0][i]
        ok = true
        j = 1
        while j < strs.count() and ok:
            if i >= strs[j].count() or strs[j][i] != c:
                ok = false
            j += 1
        if not ok: return out
        out = concat(out, '{c}')
        i += 1
    return out

@Test
fun testCanonical():
    assertEqual(longestCommonPrefix(['flower', 'flow', 'flight']), 'fl')

@Test
fun testNone():
    assertEqual(longestCommonPrefix(['dog', 'racecar', 'car']), '')

@Test
fun testIdentical():
    assertEqual(longestCommonPrefix(['hello', 'hello']), 'hello')

@Test
fun testEmpty():
    assertEqual(longestCommonPrefix([]), '')

@Test
fun testSingleton():
    assertEqual(longestCommonPrefix(['solo']), 'solo')
