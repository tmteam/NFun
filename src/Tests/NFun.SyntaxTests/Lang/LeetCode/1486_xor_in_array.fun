# LeetCode 1486 — XOR Operation in an Array
#
# Build [start, start+2, start+4, ...] (length n) and XOR them all.

fun xorOperation(n, start):
    result = 0
    i = 0
    while i < n:
        result = result ^ (start + 2 * i)
        i += 1
    return result

@Test(5, 0, 8)
@Test(4, 3, 8)
@Test(1, 7, 7)
@Test(10, 5, 2)
fun testXorOperation(n, start, expected):
    assertEqual(xorOperation(n, start), expected)
