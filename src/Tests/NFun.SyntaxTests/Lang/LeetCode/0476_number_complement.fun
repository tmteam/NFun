# LeetCode 0476 — Number Complement
#
# Flip the bits in num's binary representation (only the bits that are
# present — leading zeros are not flipped). Build a mask of all 1s the
# same width as num, then XOR.

fun findComplement(num):
    mask = 0
    x = num
    while x > 0:
        mask = (mask << 1) | 1
        x = x >> 1
    return num ^ mask

@Test(5, 2)
@Test(1, 0)
@Test(7, 0)
@Test(0, 0)
@Test(10, 5)
fun testFindComplement(num, expected):
    assertEqual(findComplement(num), expected)
