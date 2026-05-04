# 10_stress.fun — complex scenarios with @Test

# GCD via Euclidean algorithm
fun gcd(a, b):
    if b == 0:
        return a
    return gcd(b, a % b)

@Test(12, 8, 4)
@Test(100, 75, 25)
@Test(7, 13, 1)
@Test(48, 18, 6)
fun testGcd(a, b, expected):
    assertEqual(gcd(a, b), expected)

# Chain of computations
fun process(x):
    step1 = x * 2 + 1
    step2 = step1 * 3 - 2
    step3 = step2 // 4
    result = step3 + 10
    return result

# process(0): 0*2+1=1, 1*3-2=1, 1//4=0, 0+10=10
# process(5): 5*2+1=11, 11*3-2=31, 31//4=7, 7+10=17
# process(10): 10*2+1=21, 21*3-2=61, 61//4=15, 15+10=25
@Test(0, 10)
@Test(5, 17)
@Test(10, 25)
fun testProcess(x, expected):
    assertEqual(process(x), expected)

# Multiple recursive functions
fun ackermann(m, n):
    if m == 0:
        return n + 1
    if n == 0:
        return ackermann(m - 1, 1)
    return ackermann(m - 1, ackermann(m, n - 1))

@Test(0, 0, 1)
@Test(1, 1, 3)
@Test(2, 2, 7)
@Test(3, 2, 29)
fun testAckermann(m, n, expected):
    assertEqual(ackermann(m, n), expected)

# Deep nesting
fun deepNest(x):
    if x > 50:
        if x > 75:
            if x > 90:
                return 4
            return 3
        return 2
    if x > 25:
        return 1
    return 0

@Test(95, 4)
@Test(80, 3)
@Test(60, 2)
@Test(30, 1)
@Test(10, 0)
fun testDeepNest(x, expected):
    assertEqual(deepNest(x), expected)
