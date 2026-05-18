# LeetCode 1859 — Sorting the Sentence
#
# Words have a trailing digit indicating their position (1..n). Sort by that.

fun lastDigit(w:text) -> int:
    lastCharText = '{w[w.count() - 1]}'
    pos:int = convert(lastCharText)
    return pos

fun stripDigit(w:text) -> text:
    return w.take(w.count() - 1)

fun sortSentence(s:text):
    words = s.split(' ')
    placed:text[] = repeat('', words.count())
    for w in words:
        idx = lastDigit(w) - 1
        placed = placed.set(idx, stripDigit(w))
    return placed.join(' ')

@Test
fun testCanonical():
    assertEqual(sortSentence('is2 sentence4 This1 a3'), 'This is a sentence')

@Test
fun testMyselfMe():
    assertEqual(sortSentence('Myself2 Me1 I4 and3'), 'Me Myself and I')
