# LeetCode 1018 — Binary Prefix Divisible By 5
#
# For each prefix of the binary array, is the number it represents divisible
# by 5? Keep a running value mod 5 — only the residue matters, so this fits
# comfortably in an int.

fun prefixesDivBy5(nums):
    out = []
    cur = 0
    for b in nums:
        cur = (cur * 2 + b) % 5
        out = concat(out, [cur == 0])
    return out

@Test
fun testCanonical():
    assertEqual(prefixesDivBy5([0, 1, 1]), [true, false, false])

@Test
fun testAllOnes():
    assertEqual(prefixesDivBy5([1, 1, 1]), [false, false, false])

@Test
fun testFiveBit():
    # 5 = 0b101 prefixes: 1, 10=2, 101=5
    assertEqual(prefixesDivBy5([1, 0, 1]), [false, false, true])
