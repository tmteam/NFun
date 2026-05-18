# LeetCode 0125 — Valid Palindrome
#
# Case-insensitive, ignoring non-alphanumeric. Two-pointer scan skipping
# unwanted chars.

fun isAlnum(c):
    if c >= /'0' and c <= /'9': return true
    if c >= /'a' and c <= /'z': return true
    if c >= /'A' and c <= /'Z': return true
    return false

fun toLower(c):
    if c >= /'A' and c <= /'Z':
        # ASCII gap A→a is 32. NFun has no char arithmetic, so compare via
        # the upper alphabet positions.
        upper = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ'
        lower = 'abcdefghijklmnopqrstuvwxyz'
        i = 0
        while i < 26:
            if upper[i] == c: return lower[i]
            i += 1
    return c

fun isPalindrome(s):
    lo = 0
    hi = s.count() - 1
    while lo < hi:
        while lo < hi and not isAlnum(s[lo]): lo += 1
        while lo < hi and not isAlnum(s[hi]): hi -= 1
        if toLower(s[lo]) != toLower(s[hi]): return false
        lo += 1
        hi -= 1
    return true

@Test
fun testCanonical():
    assertEqual(isPalindrome('A man, a plan, a canal: Panama'), true)

@Test
fun testNotPalindrome():
    assertEqual(isPalindrome('race a car'), false)

@Test
fun testEmpty():
    assertEqual(isPalindrome(''), true)

@Test
fun testPunctuationOnly():
    assertEqual(isPalindrome('.,'), true)
