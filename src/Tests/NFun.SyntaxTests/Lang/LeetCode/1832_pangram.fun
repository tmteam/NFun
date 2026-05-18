# LeetCode 1832 — Check if Sentence Is Pangram

fun isPangram(sentence):
    alphabet = 'abcdefghijklmnopqrstuvwxyz'
    i = 0
    while i < 26:
        c = alphabet[i]
        seen = false
        for ch in sentence:
            if ch == c: seen = true
        if not seen: return false
        i += 1
    return true

@Test('thequickbrownfoxjumpsoverthelazydog', true)
@Test('leetcode', false)
fun testIsPangram(s, expected):
    assertEqual(isPangram(s), expected)
