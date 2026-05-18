# LeetCode 0009 — Palindrome Number
#
# Given an integer x, return true if x is a palindrome and false otherwise.
# Negative numbers are not palindromes (the leading minus sign has no mirror).

fun isPalindromeNum(x):
    if x < 0: return false
    n = x
    reversed = 0
    while n > 0:
        reversed = reversed * 10 + n % 10
        n = n // 10
    return reversed == x

@Test(121, true)
@Test(-121, false)
@Test(10, false)
@Test(0, true)
@Test(12321, true)
@Test(123, false)
fun testPalindromeNum(x, expected):
    assertEqual(isPalindromeNum(x), expected)
