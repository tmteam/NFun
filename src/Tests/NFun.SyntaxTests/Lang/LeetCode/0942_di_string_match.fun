# LeetCode 0942 — DI String Match
#
# Build any permutation of 0..n that matches the I/D pattern. Greedy: on 'I'
# take the smallest unused, on 'D' take the largest unused; one extra at the
# end to balance.

fun diStringMatch(s):
    n = s.count()
    lo = 0
    hi = n
    out = []
    for c in s:
        if c == /'I':
            out = concat(out, [lo])
            lo += 1
        else:
            out = concat(out, [hi])
            hi -= 1
    return concat(out, [lo])

@Test
fun testIDID():
    assertEqual(diStringMatch('IDID'), [0, 4, 1, 3, 2])

@Test
fun testIII():
    assertEqual(diStringMatch('III'), [0, 1, 2, 3])

@Test
fun testDDI():
    assertEqual(diStringMatch('DDI'), [3, 2, 0, 1])
