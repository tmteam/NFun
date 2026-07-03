# 03_if_elif_else.fun — conditionals with @Test

fun abs(x):
    if x >= 0:
        return x
    else:
        return -x

@Test(5, 5)
@Test(-5, 5)
@Test(0, 0)
fun testAbs(input, expected):
    assertEqual(abs(input), expected)

fun sign(x):
    if x > 0:
        return 1
    elif x < 0:
        return -1
    else:
        return 0

@Test(42, 1)
@Test(-7, -1)
@Test(0, 0)
fun testSign(input, expected):
    assertEqual(sign(input), expected)

fun clamp(x, lo, hi):
    if x < lo:
        return lo
    elif x > hi:
        return hi
    else:
        return x

@Test(5, 0, 10, 5)
@Test(-5, 0, 10, 0)
@Test(15, 0, 10, 10)
@Test(0, 0, 10, 0)
fun testClamp(x, lo, hi, expected):
    assertEqual(clamp(x, lo, hi), expected)
