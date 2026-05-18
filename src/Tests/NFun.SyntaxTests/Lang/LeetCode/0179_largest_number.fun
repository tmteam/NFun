# LeetCode 0179 — Largest Number
#
# Arrange the numbers' decimal representations so the concatenation is
# largest. Comparator: `a+b > b+a` (string concat).

fun toStr(n):
    return '{n}'

fun aFirst(a, b):
    # true iff a should precede b — i.e. concat(a, b) > concat(b, a)
    ab = concat(toStr(a), toStr(b))
    ba = concat(toStr(b), toStr(a))
    i = 0
    while i < ab.count():
        if ab[i] > ba[i]: return true
        if ab[i] < ba[i]: return false
        i += 1
    return false

fun insertSorted(out, x):
    n = out.count()
    i = 0
    while i < n and aFirst(out[i], x):
        i += 1
    return concat(concat(out.take(i), [x]), out.skip(i))

fun largestNumber(nums):
    sorted = []
    for x in nums:
        sorted = insertSorted(sorted, x)
    out = ''
    for x in sorted:
        out = concat(out, toStr(x))
    # All zeros → "0", not "0000"
    if out.count() > 0 and out[0] == /'0': return '0'
    return out

@Test
fun testCanonical():
    assertEqual(largestNumber([10, 2]), '210')

@Test
fun testFiveNumbers():
    assertEqual(largestNumber([3, 30, 34, 5, 9]), '9534330')

@Test
fun testSingleton():
    assertEqual(largestNumber([1]), '1')

@Test
fun testAllZeros():
    assertEqual(largestNumber([0, 0]), '0')
