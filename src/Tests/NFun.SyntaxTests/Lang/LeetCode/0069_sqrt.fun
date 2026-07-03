# LeetCode 0069 — Sqrt(x)
#
# Given a non-negative integer x, return the integer part of sqrt(x).
# Binary search on the answer — O(log x), avoids floating point.

fun mySqrt(x):
    if x < 2: return x
    lo = 1
    hi = x // 2
    result = 0
    while lo <= hi:
        mid = (lo + hi) // 2
        # `mid <= x / mid` is `mid² <= x` rewritten to avoid Int32 overflow
        # when mid is large.
        if mid <= x // mid:
            result = mid
            lo = mid + 1
        else:
            hi = mid - 1
    return result

@Test(0, 0)
@Test(1, 1)
@Test(4, 2)
@Test(8, 2)
@Test(16, 4)
@Test(2147395599, 46339)
fun testMySqrt(x, expected):
    assertEqual(mySqrt(x), expected)
