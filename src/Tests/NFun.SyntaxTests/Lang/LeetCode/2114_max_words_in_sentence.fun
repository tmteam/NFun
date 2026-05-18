# LeetCode 2114 — Maximum Number of Words Found in Sentences
#
# Max over sentences of (count of spaces + 1).

fun mostWordsFound(sentences):
    best = 0
    for s in sentences:
        words = 1
        for c in s:
            if c == /' ': words += 1
        if words > best: best = words
    return best

@Test
fun testCanonical():
    assertEqual(mostWordsFound(['alice and bob love leetcode', 'i think so too', 'this is great thanks very much']), 6)

@Test
fun testEmpty():
    assertEqual(mostWordsFound(['please wait', 'continue to fight', 'continue to win']), 3)
