# LeetCode 1470 — Shuffle the Array
#
# Interleave x_i and y_i from nums = [x1,..xn, y1,..yn].

fun shuffle(nums, n):
    out = []
    i = 0
    while i < n:
        out = concat(out, [nums[i], nums[i + n]])
        i += 1
    return out

@Test
fun testCanonical():
    assertEqual(shuffle([2, 5, 1, 3, 4, 7], 3), [2, 3, 5, 4, 1, 7])

@Test
fun testAllZero():
    assertEqual(shuffle([1, 1, 2, 2], 2), [1, 2, 1, 2])
