# LeetCode 0264 — Ugly Number II
#
# n-th positive number whose only prime factors are 2, 3 and 5.
# DP via three pointers: every ugly number is min(2x, 3y, 5z) for previously
# generated values; advance the pointer(s) that produced the min.

fun nthUglyNumber(n):
    arr = [1]
    i2 = 0
    i3 = 0
    i5 = 0
    while arr.count() < n:
        nxt2 = arr[i2] * 2
        nxt3 = arr[i3] * 3
        nxt5 = arr[i5] * 5
        nextVal = min(min(nxt2, nxt3), nxt5)
        arr = concat(arr, [nextVal])
        if nextVal == nxt2: i2 += 1
        if nextVal == nxt3: i3 += 1
        if nextVal == nxt5: i5 += 1
    return arr[n - 1]

@Test(1, 1)
@Test(10, 12)
@Test(11, 15)
@Test(15, 24)
@Test(50, 243)
fun testNthUglyNumber(n, expected):
    assertEqual(nthUglyNumber(n), expected)
