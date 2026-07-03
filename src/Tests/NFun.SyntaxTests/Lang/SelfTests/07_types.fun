# 07_types.fun — type inference and assertType with @Test

# assertType calls at top level (types resolve differently inside generic functions)
assertType(42, 'int')
assertType(3.14, 'real')
assertType(true, 'bool')
assertType('hello', 'text')
assertType([1, 2, 3], 'list<int>')

fun identity(x):
    return x

assertType(identity(42), 'int')
assertType(identity(true), 'bool')

fun makePositive(x):
    if x >= 0:
        return x
    return -x

assertType(makePositive(5), 'int')

fun alwaysTrue():
    return true

assertType(alwaysTrue(), 'bool')

fun pi():
    return 3.14

assertType(pi(), 'real')

@Test
fun testIdentity():
    assertEqual(identity(42), 42)
    assertEqual(identity(true), true)

@Test
fun testMakePositive():
    assertEqual(makePositive(5), 5)
    assertEqual(makePositive(-5), 5)

@Test
fun testAlwaysTrue():
    assertEqual(alwaysTrue(), true)

@Test
fun testPi():
    assertEqual(pi(), 3.14)
