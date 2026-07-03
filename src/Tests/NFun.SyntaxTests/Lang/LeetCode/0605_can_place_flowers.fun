# LeetCode 0605 — Can Place Flowers
#
# Greedy: scan left to right, plant whenever the current spot and both
# neighbours are 0.

fun canPlaceFlowers(flowerbed, n):
    bed = flowerbed
    placed = 0
    i = 0
    while i < bed.count():
        if bed[i] == 0:
            leftFree = i == 0 or bed[i - 1] == 0
            rightFree = i == bed.count() - 1 or bed[i + 1] == 0
            if leftFree and rightFree:
                bed = bed.setAt(i, 1)
                placed += 1
                if placed >= n: return true
        i += 1
    return placed >= n

@Test
fun testCanonicalYes():
    assertEqual(canPlaceFlowers([1, 0, 0, 0, 1], 1), true)

@Test
fun testCanonicalNo():
    assertEqual(canPlaceFlowers([1, 0, 0, 0, 1], 2), false)

@Test
fun testZero():
    assertEqual(canPlaceFlowers([1, 0, 0, 0, 0, 1], 0), true)

@Test
fun testEmptyish():
    assertEqual(canPlaceFlowers([0], 1), true)
