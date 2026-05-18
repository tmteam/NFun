# LeetCode 0884 — Uncommon Words from Two Sentences
#
# A word is "uncommon" if it appears exactly once in either sentence and
# not in the other. With no hash map: concat both sentences' words and emit
# every word with total count exactly 1.

fun splitWords(s):
    out = []
    buf = ''
    i = 0
    while i < s.count():
        c = s[i]
        if c == /' ':
            if buf.count() > 0:
                out = concat(out, [buf])
                buf = ''
        else:
            buf = concat(buf, '{c}')
        i += 1
    if buf.count() > 0: out = concat(out, [buf])
    return out

fun uncommonFromSentences(s1, s2):
    words = concat(splitWords(s1), splitWords(s2))
    out = []
    i = 0
    while i < words.count():
        count = 0
        j = 0
        while j < words.count():
            if words[i] == words[j]: count += 1
            j += 1
        if count == 1: out = concat(out, [words[i]])
        i += 1
    return out

@Test
fun testCanonical():
    assertEqual(uncommonFromSentences('this apple is sweet', 'this apple is sour'),
                ['sweet', 'sour'])

@Test
fun testNoneShared():
    assertEqual(uncommonFromSentences('apple apple', 'banana'), ['banana'])
