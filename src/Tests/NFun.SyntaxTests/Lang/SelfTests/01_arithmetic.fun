# 01_arithmetic.fun — basic arithmetic with @Test functions

fun add(a, b):
    return a + b

@Test(1, 2, 3)
@Test(0, 0, 0)
@Test(10, 20, 30)
@Test(-1, 1, 0)
fun testAdd(a, b, expected):
    assertEqual(add(a, b), expected)

@Test(5, 25)
@Test(0, 0)
@Test(-3, 9)
fun testSquare(x, expected):
    assertEqual(x * x, expected)

@Test
fun testArithmeticOperators():
    assertEqual(2 + 3, 5)
    assertEqual(10 - 3, 7)
    assertEqual(4 * 5, 20)
    assertEqual(10 // 3, 3)
    assertEqual(10 % 3, 1)

@Test
fun testTypes():
    assertType(2 + 3, 'int')
    assertType(2.0 + 3.0, 'real')
