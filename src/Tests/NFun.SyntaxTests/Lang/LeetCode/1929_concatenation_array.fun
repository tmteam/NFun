# LeetCode 1929 — Concatenation of Array

fun getConcatenation(nums):
    return concat(nums, nums)

@Test
fun testCanonical():
    assertEqual(getConcatenation([1, 2, 1]), [1, 2, 1, 1, 2, 1])

@Test
fun testEmpty():
    assertEqual(getConcatenation([]), [])

@Test
fun testSingle():
    assertEqual(getConcatenation([42]), [42, 42])
