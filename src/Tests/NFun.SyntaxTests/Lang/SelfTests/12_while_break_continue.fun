# 12_while_break_continue.fun — while/break/continue with @Test

fun whileBreakImmediate(dummy):
    while true:
        break
    return 42

@Test(0, 42)
fun testWhileBreakImmediate(dummy, expected):
    assertEqual(whileBreakImmediate(dummy), expected)

fun whileFalse(dummy):
    while false:
        return 0
    return 99

@Test(0, 99)
fun testWhileFalse(dummy, expected):
    assertEqual(whileFalse(dummy), expected)

fun whileReturnImmediate(dummy):
    while true:
        return 77
    return 0

@Test(0, 77)
fun testWhileReturnImmediate(dummy, expected):
    assertEqual(whileReturnImmediate(dummy), expected)

fun forContinueSkipFirst(skip):
    for x in [1,2,3]:
        if x == skip:
            continue
        return x
    return -1

@Test(1, 2)
@Test(3, 1)
fun testForContinueSkipFirst(skip, expected):
    assertEqual(forContinueSkipFirst(skip), expected)
