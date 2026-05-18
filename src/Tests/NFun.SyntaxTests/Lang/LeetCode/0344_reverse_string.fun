# LeetCode 0344 — Reverse String
#
# Leetcode's canonical version reverses in place; without mutable arrays we
# build a new value. NFun strings expose `reverse()` directly so the function
# is one line — included to exercise the text path.

fun reverseString(s):
    return s.reverse()

@Test('hello', 'olleh')
@Test('A', 'A')
@Test('', '')
@Test('racecar', 'racecar')
@Test('ab', 'ba')
fun testReverseString(s, expected):
    assertEqual(reverseString(s), expected)
