# LeetCode 1013 — Partition Array Into Three Parts With Equal Sum
#
# Total must be divisible by 3; then sweep and count parts hitting total/3.

fun canThreePartsEqualSum(arr):
    total = arr.sum()
    if total % 3 != 0: return false
    target = total // 3
    running = 0
    parts = 0
    for x in arr:
        running += x
        if running == target:
            parts += 1
            running = 0
    return parts >= 3

@Test
fun testCanonical():
    assertEqual(canThreePartsEqualSum([0, 2, 1, -6, 6, -7, 9, 1, 2, 0, 1]), true)

@Test
fun testNotDivisible():
    assertEqual(canThreePartsEqualSum([0, 2, 1, -6, 6, 7, 9, -1, 2, 0, 1]), false)

@Test
fun testRanges():
    assertEqual(canThreePartsEqualSum([3, 3, 6, 5, -2, 2, 5, 1, -9, 4]), true)
