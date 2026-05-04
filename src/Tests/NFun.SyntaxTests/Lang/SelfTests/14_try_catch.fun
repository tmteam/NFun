# 14_try_catch.fun — try/catch/anyway with @Test

fun safeDivide(a, b):
    return try:
        a / b
    catch:
        0

@Test(10, 2, 5)
@Test(100, 10, 10)
fun testSafeDivide(a, b, expected):
    assertEqual(safeDivide(a, b), expected)

fun tryCatchNoError():
    return try:
        42
    catch:
        0

@Test(42)
fun testTryCatchNoError(expected):
    assertEqual(tryCatchNoError(), expected)

fun tryCatchWithError():
    return try:
        oops('fail')
    catch:
        99

@Test(99)
fun testTryCatchWithError(expected):
    assertEqual(tryCatchWithError(), expected)

fun nestedTry():
    return try:
        try:
            oops('inner')
        catch:
            10
    catch:
        99

@Test(10)
fun testNestedTry(expected):
    assertEqual(nestedTry(), expected)
