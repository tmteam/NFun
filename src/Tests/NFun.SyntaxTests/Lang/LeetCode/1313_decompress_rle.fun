# LeetCode 1313 — Decompress Run-Length Encoded List
#
# Pairs (freq, value) — expand to `freq` copies of `value`.

fun decompressRLElist(nums):
    out = []
    i = 0
    while i < nums.count():
        freq = nums[i]
        value = nums[i + 1]
        j = 0
        while j < freq:
            out = concat(out, [value])
            j += 1
        i += 2
    return out

@Test
fun testCanonical():
    assertEqual(decompressRLElist([1, 2, 3, 4]), [2, 4, 4, 4])

@Test
fun testZeroFreq():
    assertEqual(decompressRLElist([0, 5, 2, 7]), [7, 7])
