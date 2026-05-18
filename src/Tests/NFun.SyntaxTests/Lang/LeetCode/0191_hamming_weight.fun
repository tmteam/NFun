# LeetCode 0191 — Number of 1 Bits (Hamming Weight)
#
# Count bits set to 1 in the binary representation of an unsigned integer.
# `n & (n - 1)` clears the lowest set bit — loop while bits remain.

fun hammingWeight(n):
    count = 0
    x = n
    while x != 0:
        x = x & (x - 1)
        count += 1
    return count

@Test(0, 0)
@Test(1, 1)
@Test(7, 3)
@Test(11, 3)
@Test(128, 1)
@Test(255, 8)
fun testHammingWeight(n, expected):
    assertEqual(hammingWeight(n), expected)
