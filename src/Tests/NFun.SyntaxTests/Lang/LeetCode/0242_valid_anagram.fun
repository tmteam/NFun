# LeetCode 0242 — Valid Anagram
#
# `s` and `t` are anagrams iff their character multisets match. Without a
# hash counter, sort both strings and compare — O(n log n).

fun isAnagram(s, t):
    if s.count() != t.count(): return false
    a = s.sort()
    b = t.sort()
    i = 0
    while i < a.count():
        if a[i] != b[i]: return false
        i += 1
    return true

@Test
fun testCanonical():
    assertEqual(isAnagram('anagram', 'nagaram'), true)

@Test
fun testNotAnagram():
    assertEqual(isAnagram('rat', 'car'), false)

@Test
fun testDifferentLengths():
    assertEqual(isAnagram('aa', 'aab'), false)

@Test
fun testBothEmpty():
    assertEqual(isAnagram('', ''), true)
