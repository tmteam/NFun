# 05_local_variables.fun — local variables and scope with @Test

fun quadratic(a, b, c, x):
    ax2 = a * x * x
    bx = b * x
    return ax2 + bx + c

@Test(1, 0, 0, 3, 9)
@Test(1, 2, 1, 0, 1)
@Test(1, -2, 1, 1, 0)
fun testQuadratic(a, b, c, x, expected):
    assertEqual(quadratic(a, b, c, x), expected)

fun hypotenuse(a, b):
    a2 = a * a
    b2 = b * b
    return a2 + b2

@Test(3, 4, 25)
@Test(5, 12, 169)
fun testHypotenuse(a, b, expected):
    assertEqual(hypotenuse(a, b), expected)

fun multiStep(x):
    step1 = x + 10
    step2 = step1 * 2
    step3 = step2 - 5
    return step3

@Test(0, 15)
@Test(5, 25)
fun testMultiStep(x, expected):
    assertEqual(multiStep(x), expected)

fun swap(a, b):
    first = b
    second = a
    return first * 100 + second

@Test(1, 2, 201)
@Test(9, 3, 309)
fun testSwap(a, b, expected):
    assertEqual(swap(a, b), expected)

fun doubleAndAdd(x, y):
    doubled = x * 2
    added = doubled + y
    return added

@Test(5, 3, 13)
@Test(0, 0, 0)
fun testDoubleAndAdd(x, y, expected):
    assertEqual(doubleAndAdd(x, y), expected)
