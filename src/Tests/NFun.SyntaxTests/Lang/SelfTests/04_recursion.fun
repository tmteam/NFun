# 04_recursion.fun — function calls, composition, early return with @Test

fun double(x):
    return x * 2

fun triple(x):
    return x * 3

@Test(5, 10)
@Test(0, 0)
@Test(-3, -6)
fun testDouble(x, expected):
    assertEqual(double(x), expected)

@Test(5, 15)
@Test(0, 0)
@Test(-2, -6)
fun testTriple(x, expected):
    assertEqual(triple(x), expected)

fun compose(x):
    a = double(x)
    b = triple(x)
    return a + b

@Test(2, 10)
@Test(5, 25)
fun testCompose(x, expected):
    assertEqual(compose(x), expected)

fun earlyReturn(x):
    if x > 100:
        return 100
    if x < 0:
        return 0
    return x

@Test(50, 50)
@Test(200, 100)
@Test(-5, 0)
fun testEarlyReturn(x, expected):
    assertEqual(earlyReturn(x), expected)

fun multiReturn(a, b):
    if a == 0:
        return b
    if b == 0:
        return a
    return a + b

@Test(0, 5, 5)
@Test(3, 0, 3)
@Test(3, 5, 8)
fun testMultiReturn(a, b, expected):
    assertEqual(multiReturn(a, b), expected)
