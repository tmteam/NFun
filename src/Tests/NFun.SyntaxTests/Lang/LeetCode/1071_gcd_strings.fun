# LeetCode 1071 — Greatest Common Divisor of Strings
#
# x divides y if y is repeated x. GCD of two strings = longest x that divides both.

fun gcdInt(a:int, b:int):
    while b != 0:
        t = b
        b = a % b
        a = t
    return a

fun repeats(part:text, n:int):
    out = ''
    i = 0
    while i < n:
        out = concat(out, part)
        i += 1
    return out

fun gcdOfStrings(s1:text, s2:text):
    n1 = s1.count()
    n2 = s2.count()
    g = gcdInt(n1, n2)
    candidate = s1.take(g)
    if repeats(candidate, n1 // g) == s1 and repeats(candidate, n2 // g) == s2:
        return candidate
    return ''

@Test
fun testCanonical():
    assertEqual(gcdOfStrings('ABCABC', 'ABC'), 'ABC')

@Test
fun testRepeated():
    assertEqual(gcdOfStrings('ABABAB', 'ABAB'), 'AB')

@Test
fun testNoCommon():
    assertEqual(gcdOfStrings('LEET', 'CODE'), '')
