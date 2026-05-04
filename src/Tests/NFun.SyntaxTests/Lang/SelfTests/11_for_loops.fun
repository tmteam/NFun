# 11_for_loops.fun — for loop tests with @Test

fun findInSmall(target):
    for x in [1,2,3,4,5]:
        if x == target:
            return x
    return -1

@Test(3, 3)
@Test(6, -1)
@Test(1, 1)
fun testFindInSmall(target, expected):
    assertEqual(findInSmall(target), expected)

fun containsInSmall(target):
    for x in [1,2,3]:
        if x == target:
            return true
    return false

@Test(2, true)
@Test(5, false)
fun testContainsInSmall(target, expected):
    assertEqual(containsInSmall(target), expected)

fun firstNonSkipped(skip):
    for x in [1,2,3,4,5]:
        if x == skip:
            continue
        return x
    return -1

@Test(1, 2)
@Test(3, 1)
@Test(6, 1)
fun testFirstNonSkipped(skip, expected):
    assertEqual(firstNonSkipped(skip), expected)

fun breakAtLimit(limit):
    for x in [10,20,30,40,50]:
        if x > limit:
            return x
    return -1

@Test(25, 30)
@Test(5, 10)
@Test(100, -1)
fun testBreakAtLimit(limit, expected):
    assertEqual(breakAtLimit(limit), expected)
