# LeetCode 1342 — Number of Steps to Reduce a Number to Zero

fun numberOfSteps(num):
    steps = 0
    n = num
    while n > 0:
        if n % 2 == 0: n = n // 2
        else: n -= 1
        steps += 1
    return steps

@Test(14, 6)
@Test(8, 4)
@Test(123, 12)
@Test(0, 0)
@Test(1, 1)
fun testNumberOfSteps(num, expected):
    assertEqual(numberOfSteps(num), expected)
