# LeetCode 2108 — Find First Palindromic String in the Array

fun isPal(s):
    lo = 0
    hi = s.count() - 1
    while lo < hi:
        if s[lo] != s[hi]: return false
        lo += 1
        hi -= 1
    return true

fun firstPalindrome(words):
    for w in words:
        if isPal(w): return w
    return ''

@Test
fun testCanonical():
    assertEqual(firstPalindrome(['abc', 'car', 'ada', 'racecar', 'cool']), 'ada')

@Test
fun testRacecarFirst():
    assertEqual(firstPalindrome(['notapalindrome', 'racecar']), 'racecar')

@Test
fun testNonePresent():
    assertEqual(firstPalindrome(['def', 'ghi']), '')
