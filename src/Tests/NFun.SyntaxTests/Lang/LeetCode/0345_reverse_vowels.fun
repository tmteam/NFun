# LeetCode 0345 — Reverse Vowels of a String
#
# Two pointers: when both ends are vowels, swap-via-rebuild; otherwise
# advance the non-vowel side.

fun isVowel(c):
    vowels = 'aeiouAEIOU'
    i = 0
    while i < vowels.count():
        if vowels[i] == c: return true
        i += 1
    return false

fun reverseVowels(s):
    chars = s
    n = chars.count()
    out = ''
    # Collect vowel positions and chars, then rebuild placing vowels in reverse.
    vowels = []
    i = 0
    while i < n:
        if isVowel(chars[i]): vowels = concat(vowels, [chars[i]])
        i += 1
    vi = vowels.count() - 1
    i = 0
    while i < n:
        if isVowel(chars[i]):
            out = concat(out, '{vowels[vi]}')
            vi -= 1
        else:
            out = concat(out, '{chars[i]}')
        i += 1
    return out

@Test('hello', 'holle')
@Test('leetcode', 'leotcede')
@Test('aA', 'Aa')
@Test('bcdfg', 'bcdfg')
@Test('', '')
fun testReverseVowels(s, expected):
    assertEqual(reverseVowels(s), expected)
