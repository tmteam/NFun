# LeetCode 0633 — Sum of Square Numbers
#
# Given a non-negative integer c, decide whether there exist non-negative
# integers a, b such that a² + b² = c. Two pointers from 0..sqrt(c).

fun judgeSquareSum(c):
    lo = 0
    hi = 1
    while hi * hi <= c:
        hi += 1
    while lo <= hi:
        s = lo * lo + hi * hi
        if s == c: return true
        elif s < c: lo += 1
        else: hi -= 1
    return false

@Test(0, true)
@Test(1, true)
@Test(2, true)
@Test(4, true)
@Test(5, true)
@Test(3, false)
@Test(7, false)
@Test(25, true)
@Test(100, true)
fun testJudgeSquareSum(c, expected):
    assertEqual(judgeSquareSum(c), expected)
