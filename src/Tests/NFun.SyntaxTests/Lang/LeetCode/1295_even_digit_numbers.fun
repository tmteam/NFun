# LeetCode 1295 — Find Numbers with Even Number of Digits

fun digitCount(n):
    if n == 0: return 1
    c = 0
    x = if n < 0: -n else: n
    while x > 0:
        x = x // 10
        c += 1
    return c

fun findNumbers(nums):
    count = 0
    for x in nums:
        if digitCount(x) % 2 == 0: count += 1
    return count

@Test
fun testCanonical():
    assertEqual(findNumbers([12, 345, 2, 6, 7896]), 2)

@Test
fun testNoneEven():
    assertEqual(findNumbers([555, 901, 482, 1771]), 1)
