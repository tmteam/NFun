# 08_early_return.fun — early return patterns with @Test

fun findFirst(threshold):
    if threshold > 100:
        return 100
    if threshold > 50:
        return 50
    if threshold > 10:
        return 10
    return 0

@Test(200, 100)
@Test(75, 50)
@Test(30, 10)
@Test(5, 0)
fun testFindFirst(input, expected):
    assertEqual(findFirst(input), expected)

fun validate(x):
    if x < 0:
        return -1
    if x > 100:
        return -1
    return x

@Test(50, 50)
@Test(-5, -1)
@Test(200, -1)
@Test(0, 0)
@Test(100, 100)
fun testValidate(x, expected):
    assertEqual(validate(x), expected)

fun nestedEarlyReturn(a, b):
    if a <= 0:
        return 0
    if b <= 0:
        return 0
    sum = a + b
    if sum > 100:
        return 100
    return sum

@Test(0, 5, 0)
@Test(5, 0, 0)
@Test(10, 20, 30)
@Test(60, 60, 100)
fun testNestedEarlyReturn(a, b, expected):
    assertEqual(nestedEarlyReturn(a, b), expected)
