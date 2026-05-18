# LeetCode 0941 — Valid Mountain Array
#
# Increases strictly to a peak, then decreases strictly. Walk up to the peak
# from the left and the right; both walks must meet at the same interior
# index.

fun validMountainArray(arr):
    n = arr.count()
    if n < 3: return false
    i = 0
    while i + 1 < n and arr[i] < arr[i + 1]:
        i += 1
    if i == 0 or i == n - 1: return false
    while i + 1 < n and arr[i] > arr[i + 1]:
        i += 1
    return i == n - 1

@Test
fun testCanonical():
    assertEqual(validMountainArray([0, 3, 2, 1]), true)

@Test
fun testMonotone():
    assertEqual(validMountainArray([3, 5, 5]), false)

@Test
fun testTwoElements():
    assertEqual(validMountainArray([2, 1]), false)

@Test
fun testPlateau():
    assertEqual(validMountainArray([1, 2, 2, 1]), false)
