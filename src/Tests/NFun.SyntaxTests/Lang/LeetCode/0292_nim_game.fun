# LeetCode 0292 — Nim Game
#
# Two players take 1, 2 or 3 stones; the one who takes the last stone wins.
# With optimal play, the first player loses exactly when n is divisible by 4.

fun canWinNim(n):
    return n % 4 != 0

@Test(1, true)
@Test(2, true)
@Test(3, true)
@Test(4, false)
@Test(5, true)
@Test(8, false)
@Test(100, false)
fun testCanWinNim(n, expected):
    assertEqual(canWinNim(n), expected)
