# LeetCode 0367 — Valid Perfect Square
#
# Return true iff num is a perfect square. Binary search avoids floating
# point — works for the full 32-bit positive range.

fun isPerfectSquare(num):
    if num < 1: return false
    lo = 1
    hi = num
    while lo <= hi:
        mid = (lo + hi) // 2
        # Compare via `num // mid` to dodge Int32 overflow on mid² for large mid.
        q = num // mid
        if q == mid and num % mid == 0: return true
        elif q > mid: lo = mid + 1
        else: hi = mid - 1
    return false

@Test(1, true)
@Test(4, true)
@Test(9, true)
@Test(16, true)
@Test(14, false)
@Test(2, false)
@Test(0, false)
@Test(808201, true)
fun testIsPerfectSquare(num, expected):
    assertEqual(isPerfectSquare(num), expected)
