# LeetCode 1304 — Find N Unique Integers Sum up to Zero
#
# Output ±1, ±2, …, then a 0 if n is odd.

fun sumZero(n):
    out = []
    half = n // 2
    i = 1
    while i <= half:
        out = concat(out, [i])
        out = concat(out, [-i])
        i += 1
    if n % 2 == 1: out = concat(out, [0])
    return out

@Test
fun testEven():
    res = sumZero(4)
    assertEqual(res.count(), 4)
    assertEqual(res.sum(), 0)

@Test
fun testOdd():
    res = sumZero(5)
    assertEqual(res.count(), 5)
    assertEqual(res.sum(), 0)

@Test
fun testOne():
    assertEqual(sumZero(1), [0])
