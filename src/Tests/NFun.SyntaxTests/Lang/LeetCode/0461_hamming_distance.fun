# LeetCode 0461 — Hamming Distance
#
# Number of positions where the bits of x and y differ. XOR pinpoints them,
# popcount counts.

fun hammingDistance(x, y):
    z = x ^ y
    count = 0
    while z != 0:
        z = z & (z - 1)
        count += 1
    return count

@Test(1, 4, 2)
@Test(3, 1, 1)
@Test(0, 0, 0)
@Test(255, 0, 8)
@Test(93, 73, 2)
fun testHammingDistance(x, y, expected):
    assertEqual(hammingDistance(x, y), expected)
