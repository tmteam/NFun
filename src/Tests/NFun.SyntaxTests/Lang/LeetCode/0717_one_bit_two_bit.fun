# LeetCode 0717 — 1-bit and 2-bit Characters
#
# Bits decode as: 0 → single 1-bit char, 1x → a 2-bit char. Does the array
# end with a 1-bit char (i.e. the final 0 sits alone)?
# Walk left-to-right, advance by 1 on 0 and by 2 on 1. End at the last index
# iff the last char is the 1-bit form.

fun isOneBitCharacter(bits):
    i = 0
    n = bits.count()
    while i < n - 1:
        if bits[i] == 1: i += 2
        else: i += 1
    return i == n - 1

@Test
fun testTrue():
    assertEqual(isOneBitCharacter([1, 0, 0]), true)

@Test
fun testFalse():
    assertEqual(isOneBitCharacter([1, 1, 1, 0]), false)

@Test
fun testSingleZero():
    assertEqual(isOneBitCharacter([0]), true)
