# LeetCode 0190 — Reverse Bits
#
# Reverse the bits of a 32-bit unsigned integer. (NFun's Int32 is signed,
# but bitwise ops behave the same as unsigned at the bit level.)

fun reverseBits(n):
    result = 0
    x = n
    i = 0
    while i < 32:
        result = (result << 1) | (x & 1)
        x = x >> 1
        i += 1
    return result

@Test
fun testCanonical():
    assertEqual(reverseBits(43261596), 964176192)

@Test
fun testAllOnes():
    assertEqual(reverseBits(-1), -1)

@Test
fun testZero():
    assertEqual(reverseBits(0), 0)
