# 06_multiple_functions.fun — function composition and calling with @Test

fun double(x):
    return x * 2

fun triple(x):
    return x * 3

fun add(a, b):
    return a + b

@Test
fun testBasicFunctions():
    assertEqual(double(5), 10)
    assertEqual(triple(5), 15)
    assertEqual(add(double(3), triple(4)), 18)

fun isEven(n):
    return n % 2 == 0

fun isOdd(n):
    return not isEven(n)

@Test(0, true)
@Test(4, true)
@Test(1, false)
@Test(7, false)
fun testIsEven(n, expected):
    assertEqual(isEven(n), expected)

@Test(1, true)
@Test(7, true)
@Test(0, false)
@Test(4, false)
fun testIsOdd(n, expected):
    assertEqual(isOdd(n), expected)

fun compose(x):
    a = double(x)
    b = triple(x)
    return add(a, b)

@Test(2, 10)
@Test(5, 25)
fun testCompose(x, expected):
    assertEqual(compose(x), expected)

fun max3(a, b, c):
    m = max(a, b)
    return max(m, c)

@Test(1, 2, 3, 3)
@Test(3, 1, 2, 3)
@Test(2, 3, 1, 3)
fun testMax3(a, b, c, expected):
    assertEqual(max3(a, b, c), expected)

fun min3(a, b, c):
    m = min(a, b)
    return min(m, c)

@Test(1, 2, 3, 1)
@Test(3, 1, 2, 1)
fun testMin3(a, b, c, expected):
    assertEqual(min3(a, b, c), expected)
