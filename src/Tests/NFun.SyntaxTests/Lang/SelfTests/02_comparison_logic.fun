# 02_comparison_logic.fun — comparisons and boolean logic with @Test

@Test
fun testBasicComparisons():
    assert(1 < 2)
    assert(2 > 1)
    assert(3 >= 3)
    assert(3 <= 3)
    assert(5 == 5)
    assert(5 != 4)

@Test
fun testBooleanLogic():
    assert(not false)
    assert(true and true)
    assert(true or false)
    assert(not (true and false))

@Test
fun testCompoundConditions():
    assert(1 < 2 and 3 < 4)
    assert(1 > 2 or 3 < 4)

fun isPositive(x):
    return x > 0

@Test(1, true)
@Test(-1, false)
@Test(0, false)
fun testIsPositive(x, expected):
    assertEqual(isPositive(x), expected)

fun inRange(x, lo, hi):
    return x >= lo and x <= hi

@Test(5, 1, 10, true)
@Test(0, 1, 10, false)
@Test(1, 1, 10, true)
@Test(10, 1, 10, true)
fun testInRange(x, lo, hi, expected):
    assertEqual(inRange(x, lo, hi), expected)
