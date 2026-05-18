# LeetCode 0888 — Fair Candy Swap
#
# Alice gives x and gets y so totals equalise: x - y = (sumA - sumB) / 2.
# Sort B, then for each x in A binary-search for y = x - diff.

fun fairCandySwap(aliceSizes, bobSizes):
    sa = aliceSizes.sum()
    sb = bobSizes.sum()
    diff = (sa - sb) // 2
    bSorted = bobSizes.sort()
    i = 0
    while i < aliceSizes.count():
        target = aliceSizes[i] - diff
        # Binary search target in bSorted
        lo = 0
        hi = bSorted.count() - 1
        while lo <= hi:
            mid = (lo + hi) // 2
            if bSorted[mid] == target: return [aliceSizes[i], target]
            elif bSorted[mid] < target: lo = mid + 1
            else: hi = mid - 1
        i += 1
    return [-1, -1]

@Test
fun testCanonical():
    assertEqual(fairCandySwap([1, 1], [2, 2]), [1, 2])

@Test
fun testSecondCase():
    assertEqual(fairCandySwap([1, 2], [2, 3]), [1, 2])

@Test
fun testWithLargerSet():
    assertEqual(fairCandySwap([1, 2, 5], [2, 4]), [5, 4])
