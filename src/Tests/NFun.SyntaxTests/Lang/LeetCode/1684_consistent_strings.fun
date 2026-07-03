# LeetCode 1684 — Count the Number of Consistent Strings
#
# A word is consistent if every character appears in `allowed`.

fun inSet(c, allowed):
    i = 0
    while i < allowed.count():
        if allowed[i] == c: return true
        i += 1
    return false

fun countConsistentStrings(allowed, words):
    count = 0
    for w in words:
        ok = true
        for c in w:
            if not inSet(c, allowed): ok = false
        if ok: count += 1
    return count

@Test
fun testCanonical():
    assertEqual(countConsistentStrings('ab', ['ad', 'bd', 'aaab', 'baa', 'badab']), 2)

@Test
fun testAllAllowed():
    assertEqual(countConsistentStrings('abc', ['a', 'b', 'c', 'ab', 'ac', 'bc', 'abc']), 7)
