# LeetCode 0520 — Detect Capital
#
# Three valid forms: ALL CAPS, all lowercase, or Title Case (first char
# capital, rest lowercase). Pre-classify the first character, then verify
# the rest follow the implied rule.

fun isUpper(c):
    return c >= /'A' and c <= /'Z'

fun isLower(c):
    return c >= /'a' and c <= /'z'

fun detectCapitalUse(word):
    n = word.count()
    if n <= 1: return true
    firstUpper = isUpper(word[0])
    secondUpper = isUpper(word[1])
    # First lowercase → all must be lowercase.
    # Else: tail must consistently be all-upper or all-lower, matching word[1].
    if not firstUpper:
        if secondUpper: return false
    i = 2
    while i < n:
        if isUpper(word[i]) != secondUpper: return false
        i += 1
    if not firstUpper and secondUpper: return false
    return true

@Test('USA', true)
@Test('FlaG', false)
@Test('Google', true)
@Test('leetcode', true)
@Test('A', true)
@Test('a', true)
fun testDetect(word, expected):
    assertEqual(detectCapitalUse(word), expected)
