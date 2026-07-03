# LeetCode 1394 — Find Lucky Integer in an Array
#
# Largest x whose frequency in arr equals x; -1 if none.

fun findLucky(arr):
    s = arr.sort().reverse()
    i = 0
    while i < s.count():
        j = i
        while j < s.count() and s[j] == s[i]:
            j += 1
        if j - i == s[i]: return s[i]
        i = j
    return -1

@Test
fun testCanonical():
    assertEqual(findLucky([2, 2, 3, 4]), 2)

@Test
fun testLargerWin():
    assertEqual(findLucky([1, 2, 2, 3, 3, 3]), 3)

@Test
fun testNoneLucky():
    assertEqual(findLucky([2, 2, 2, 3, 3]), -1)
