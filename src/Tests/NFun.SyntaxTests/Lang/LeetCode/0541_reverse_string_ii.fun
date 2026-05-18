# LeetCode 0541 — Reverse String II
#
# Walk in 2k-sized blocks. In each block reverse the first k characters,
# leave the next k unchanged. Tail shorter than k → reverse it all.

fun reverseStrII(s, k):
    n = s.count()
    out = ''
    i = 0
    while i < n:
        firstK = if i + k - 1 < n: s[i:i + k - 1].reverse() else: s[i:n - 1].reverse()
        nextStart = i + k
        nextEnd = if i + 2 * k - 1 < n: i + 2 * k - 1 else: n - 1
        nextChunk = if nextStart <= nextEnd: s[nextStart:nextEnd] else: ''
        out = concat(concat(out, firstK), nextChunk)
        i += 2 * k
    return out

@Test
fun testCanonical():
    assertEqual(reverseStrII('abcdefg', 2), 'bacdfeg')

@Test
fun testKLargerThanLength():
    assertEqual(reverseStrII('abcd', 5), 'dcba')

@Test
fun testKOne():
    assertEqual(reverseStrII('abcde', 1), 'abcde')

@Test
fun testEmpty():
    assertEqual(reverseStrII('', 3), '')
