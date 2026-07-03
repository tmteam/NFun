# LeetCode 1002 — Find Common Characters
#
# For each character, return min occurrence across all words.

fun countChar(s:text, c:char):
    n = 0
    for x in s:
        if x == c: n += 1
    return n

fun findCommonChars(words):
    if words.count() == 0: return []
    result = ''
    first = words[0]
    for c in first:
        # min count across all words
        m = countChar(first, c)
        for w in words:
            cnt = countChar(w, c)
            if cnt < m: m = cnt
        # how many of c are already in result?
        already = countChar(result, c)
        if already < m: result = concat(result, '{c}')
    # split into list of single-char strings
    out = []
    for c in result:
        out = concat(out, ['{c}'])
    return out

@Test
fun testCanonical():
    assertEqual(findCommonChars(['bella', 'label', 'roller']), ['e', 'l', 'l'])

@Test
fun testNoCommon():
    assertEqual(findCommonChars(['cool', 'lock', 'cook']), ['c', 'o'])
