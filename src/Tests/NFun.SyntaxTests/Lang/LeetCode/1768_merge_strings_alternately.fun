# LeetCode 1768 — Merge Strings Alternately

fun mergeAlternately(word1, word2):
    out = ''
    i = 0
    while i < word1.count() or i < word2.count():
        if i < word1.count(): out = concat(out, '{word1[i]}')
        if i < word2.count(): out = concat(out, '{word2[i]}')
        i += 1
    return out

@Test('abc', 'pqr', 'apbqcr')
@Test('ab', 'pqrs', 'apbqrs')
@Test('abcd', 'pq', 'apbqcd')
@Test('', 'xyz', 'xyz')
fun testMergeAlternately(a, b, expected):
    assertEqual(mergeAlternately(a, b), expected)
