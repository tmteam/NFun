# LeetCode 0118 — Pascal's Triangle
#
# Return the first numRows rows. Row r = previous row offset-summed with
# itself plus a leading and trailing 1.

fun nextRow(prev):
    n = prev.count()
    out = [1]
    i = 1
    while i < n:
        out = concat(out, [prev[i - 1] + prev[i]])
        i += 1
    return concat(out, [1])

fun generate(numRows):
    if numRows == 0: return []
    rows = [[1]]
    i = 1
    while i < numRows:
        rows = concat(rows, [nextRow(rows[i - 1])])
        i += 1
    return rows

@Test
fun testFive():
    expected = [[1], [1, 1], [1, 2, 1], [1, 3, 3, 1], [1, 4, 6, 4, 1]]
    assertEqual(generate(5), expected)

@Test
fun testOne():
    assertEqual(generate(1), [[1]])

@Test
fun testZero():
    assertEqual(generate(0), [])
