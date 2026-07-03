# LeetCode 0830 — Positions of Large Groups
#
# Return [start, end] for every run of ≥3 equal characters.

fun largeGroupPositions(s):
    out = []
    n = s.count()
    i = 0
    while i < n:
        j = i
        while j < n and s[j] == s[i]:
            j += 1
        if j - i >= 3:
            out = concat(out, [[i, j - 1]])
        i = j
    return out

@Test
fun testCanonical():
    assertEqual(largeGroupPositions('abbxxxxzzy'), [[3, 6]])

@Test
fun testNoGroups():
    assertEqual(largeGroupPositions('abc'), [])

@Test
fun testTwoGroups():
    assertEqual(largeGroupPositions('aaabbbccc'), [[0, 2], [3, 5], [6, 8]])
