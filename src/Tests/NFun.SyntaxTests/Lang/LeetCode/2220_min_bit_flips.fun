# LeetCode 2220 — Minimum Bit Flips to Convert Number
#
# popcount(start XOR goal).

fun minBitFlips(start, goal):
    x = start ^ goal
    count = 0
    while x != 0:
        x = x & (x - 1)
        count += 1
    return count

@Test(10, 7, 3)
@Test(3, 4, 3)
@Test(0, 0, 0)
@Test(1, 1, 0)
fun testMinBitFlips(s, g, expected):
    assertEqual(minBitFlips(s, g), expected)
