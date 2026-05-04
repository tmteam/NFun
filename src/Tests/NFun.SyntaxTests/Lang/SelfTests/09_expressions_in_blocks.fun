# 09_expressions_in_blocks.fun — NFun expressions inside function blocks with @Test

fun maxOfThree(a, b, c):
    return max(a, max(b, c))

@Test(1, 2, 3, 3)
@Test(3, 1, 2, 3)
fun testMaxOfThree(a, b, c, expected):
    assertEqual(maxOfThree(a, b, c), expected)

fun absValue(x):
    return abs(x)

@Test(-42, 42)
@Test(42, 42)
fun testAbsValue(x, expected):
    assertEqual(absValue(x), expected)

fun conditional(x):
    result = if (x > 0) x else -x
    return result

@Test(5, 5)
@Test(-5, 5)
fun testConditional(x, expected):
    assertEqual(conditional(x), expected)

fun sumThree(a, b, c):
    total = a + b + c
    return total

@Test(10, 20, 30, 60)
fun testSumThree(a, b, c, expected):
    assertEqual(sumThree(a, b, c), expected)

fun ternaryChain(x):
    result = if (x > 100) 100 else if (x < 0) 0 else x
    return result

@Test(50, 50)
@Test(-10, 0)
@Test(200, 100)
fun testTernaryChain(x, expected):
    assertEqual(ternaryChain(x), expected)
