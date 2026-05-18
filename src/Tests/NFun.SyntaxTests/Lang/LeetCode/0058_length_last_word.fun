# LeetCode 0058 — Length of Last Word
#
# Skip trailing spaces, then count the run of non-space characters.

fun lengthOfLastWord(s):
    i = s.count() - 1
    while i >= 0 and s[i] == /' ':
        i -= 1
    length = 0
    while i >= 0 and s[i] != /' ':
        length += 1
        i -= 1
    return length

@Test
fun testCanonical():
    assertEqual(lengthOfLastWord('Hello World'), 5)

@Test
fun testTrailingSpace():
    assertEqual(lengthOfLastWord('   fly me   to   the moon  '), 4)

@Test
fun testSingleWord():
    assertEqual(lengthOfLastWord('luffy'), 5)

@Test
fun testOneChar():
    assertEqual(lengthOfLastWord('a'), 1)
