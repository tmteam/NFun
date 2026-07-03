# 13_when.fun — when pattern matching with @Test

fun classify(x):
    return when x:
        1: 'one'
        2: 'two'
        3: 'three'
        else: 'other'

@Test(1, 'one')
@Test(2, 'two')
@Test(3, 'three')
@Test(99, 'other')
fun testClassify(input, expected):
    assertEqual(classify(input), expected)

fun sign(x):
    return when:
        x > 0: 'positive'
        x < 0: 'negative'
        else: 'zero'

@Test(5, 'positive')
@Test(-3, 'negative')
@Test(0, 'zero')
fun testSign(input, expected):
    assertEqual(sign(input), expected)

fun grade(score):
    return when:
        score >= 90: 'A'
        score >= 80: 'B'
        score >= 70: 'C'
        else: 'F'

@Test(95, 'A')
@Test(85, 'B')
@Test(75, 'C')
@Test(50, 'F')
fun testGrade(score, expected):
    assertEqual(grade(score), expected)

fun dayType(d):
    return when d:
        0: 'weekend'
        6: 'weekend'
        else: 'weekday'

@Test(0, 'weekend')
@Test(6, 'weekend')
@Test(3, 'weekday')
fun testDayType(d, expected):
    assertEqual(dayType(d), expected)
