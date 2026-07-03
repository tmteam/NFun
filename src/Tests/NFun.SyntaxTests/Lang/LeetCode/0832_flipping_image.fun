# LeetCode 0832 — Flipping an Image
#
# Reverse each row then invert each bit (0↔1). With immutable arrays both
# operations are simple maps.

fun invertBit(b):
    if b == 0: return 1
    return 0

fun flipAndInvertImage(image):
    return image.map(rule it.reverse().map(rule invertBit(it)))

@Test
fun testCanonical():
    assertEqual(flipAndInvertImage([[1, 1, 0], [1, 0, 1], [0, 0, 0]]),
                [[1, 0, 0], [0, 1, 0], [1, 1, 1]])

@Test
fun testSingle():
    assertEqual(flipAndInvertImage([[1]]), [[0]])
